﻿using System;
using System.Collections.Generic;
using System.Threading;
using Dissonance.Config;
using Dissonance.Threading;
using Dissonance.VAD;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal abstract class BasePreprocessingPipeline
        : IPreprocessingPipeline
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(BasePreprocessingPipeline).Name);

        private ArvCalculator _arv = new ArvCalculator();
        public float Amplitude
        {
            get { return _arv.ARV; }
        }

        private int _droppedSamples;

        private readonly BufferedSampleProvider _resamplerInput;
        private readonly Resampler _resampler;
        private readonly SampleToFrameProvider _resampledOutput;
        private readonly float[] _intermediateFrame;

        private AudioFileWriter _diagnosticOutputRecorder;

        private readonly int _inputFrameSize;
        public int InputFrameSize
        {
            get { return _inputFrameSize; }
        }

        private readonly int _outputFrameSize;
        public int OutputFrameSize
        {
            get { return _outputFrameSize; }
        }

        private readonly WaveFormat _outputFormat;
        [NotNull] public WaveFormat OutputFormat { get { return _outputFormat; } }

        private bool _resetApplied;
        private int _resetRequested = 1;

        private volatile bool _runThread;
        private readonly DThread _thread;
        private readonly AutoResetEvent _threadEvent;

        private readonly ReadonlyLockedValue<List<IMicrophoneSubscriber>> _micSubscriptions = new ReadonlyLockedValue<List<IMicrophoneSubscriber>>(new List<IMicrophoneSubscriber>());
        private int _micSubscriptionCount;

        private readonly ReadonlyLockedValue<List<IVoiceActivationListener>> _vadSubscriptions = new ReadonlyLockedValue<List<IVoiceActivationListener>>(new List<IVoiceActivationListener>());
        private int _vadSubscriptionCount;

        protected abstract bool VadIsSpeechDetected { get; }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputFormat">format of audio supplied to `Send`</param>
        /// <param name="inputFrameSize">Size of frames which will should be provided from `GetFrameBuffer`</param>
        /// <param name="intermediateFrameSize">Size of frames which should be passed into `PreprocessAudioFrame`</param>
        /// <param name="intermediateSampleRate">Sample rate which should be passed into `PreprocessAudioFrame`</param>
        /// <param name="outputFrameSize">Size of frames which will be provided sent to `SendSamplesToSubscribers`</param>
        /// <param name="outputSampleRate"></param>
        protected BasePreprocessingPipeline([NotNull] WaveFormat inputFormat, int inputFrameSize, int intermediateFrameSize, int intermediateSampleRate, int outputFrameSize, int outputSampleRate)
        {
            if (inputFormat == null) throw new ArgumentNullException("inputFormat");
            if (inputFrameSize < 0) throw new ArgumentOutOfRangeException("inputFrameSize", "Input frame size cannot be less than zero");
            if (intermediateFrameSize < 0) throw new ArgumentOutOfRangeException("intermediateFrameSize", "Intermediate frame size cannot be less than zero");
            if (intermediateSampleRate < 0) throw new ArgumentOutOfRangeException("intermediateSampleRate", "Intermediate sample rate cannot be less than zero");
            if (outputFrameSize < 0) throw new ArgumentOutOfRangeException("outputFrameSize", "Output frame size cannot be less than zero");
            if (outputSampleRate < 0) throw new ArgumentOutOfRangeException("outputSampleRate", "Output sample rate cannot be less than zero");

            _inputFrameSize = inputFrameSize;
            _outputFrameSize = outputFrameSize;
            _outputFormat = new WaveFormat(1, outputSampleRate);

            //Create resampler to resample input to intermediate rate
            _resamplerInput = new BufferedSampleProvider(inputFormat, InputFrameSize * 16);
            _resampler = new Resampler(_resamplerInput, 48000);
            _resampledOutput = new SampleToFrameProvider(_resampler, (uint)OutputFrameSize);
            _intermediateFrame = new float[intermediateFrameSize];

            _threadEvent = new AutoResetEvent(false);
            _thread = new DThread(ThreadEntry);
        }

        public virtual void Dispose()
        {
            _runThread = false;
            _threadEvent.Set();
            if (_thread.IsStarted)
                _thread.Join();

            //ncrunch: no coverage start
            if (_diagnosticOutputRecorder != null)
            {
                _diagnosticOutputRecorder.Dispose();
                _diagnosticOutputRecorder = null;
            }
            //ncrunch: no coverage end

            using (var subscriptions = _micSubscriptions.Lock())
                while (subscriptions.Value.Count > 0)
                    Unsubscribe(subscriptions.Value[0]);

            using (var subscriptions = _vadSubscriptions.Lock())
                while (subscriptions.Value.Count > 0)
                    Unsubscribe(subscriptions.Value[0]);

            Log.Info("Disposed pipeline");
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _resetRequested, 1);
        }

        protected virtual void ApplyReset()
        {
            Log.Debug("Resetting preprocessing pipeline");

            _resamplerInput.Reset();
            _resampler.Reset();
            _resampledOutput.Reset();

            _arv.Reset();
            _droppedSamples = 0;

            SendResetToSubscribers();

            _resetApplied = true;
        }

        void IMicrophoneSubscriber.ReceiveMicrophoneData(ArraySegment<float> data, [NotNull] WaveFormat format)
        {
            if (data.Array == null) throw new ArgumentNullException("data");
            if (!format.Equals(_resamplerInput.WaveFormat)) throw new ArgumentException("Incorrect format supplied to preprocessor", "format");

            //Write data into input buffer
            var written = _resamplerInput.Write(data);
            if (written < data.Count)
            {
                //We didn't write everything, so try to write out as much as possible (fill buffer) and then count the rest of the samples as lost
                var written2 = _resamplerInput.Write(new ArraySegment<float>(data.Array, data.Offset + written, Math.Min(data.Count - written, _resamplerInput.Capacity - _resamplerInput.Capacity)));

                //Increase the count of lost samples, so we can inject the appropriate amount of silence to compensate later
                var totalWritten = written + written2;
                Interlocked.Add(ref _droppedSamples, data.Count - totalWritten);
            }

            //Wake up the processing thread
            _threadEvent.Set();
        }

        public void Start()
        {
            _runThread = true;
            _thread.Start();
        }

        private void ThreadEntry()
        {
            try
            {
                ApplyReset();

                while (_runThread)
                {
                    //Wait for an event(s) to arrive which we need to process.
                    //Max wait time is the size of the smallest frame size so it should wake up with no work to do
                    // a lot of the time (ensuring minimal processing latency when there is work to do).
                    if (_resamplerInput.Count == 0)
                        _threadEvent.WaitOne(10);

                    //Apply a pipeline reset if one has been requested, but one has not yet been applied
                    if (Interlocked.Exchange(ref _resetRequested, 0) == 1 && !_resetApplied)
                        ApplyReset();

                    //We're about to process some audio, meaning the pipeline is no longer in a reset state
                    _resetApplied = false;

                    //Reset the external dropped samples counter, we'll inject the appropriate number of samples to compensate
                    var missedSamples = Interlocked.Exchange(ref _droppedSamples, 0);

                    //Pull input data through the resampler and push it through the preprocessor
                    var preSpeech = VadIsSpeechDetected;
                    while (_resampledOutput.Read(new ArraySegment<float>(_intermediateFrame, 0, _intermediateFrame.Length)))
                    {
                        _arv.Update(new ArraySegment<float>(_intermediateFrame));
                        PreprocessAudioFrame(_intermediateFrame);
                    }
                    var postSpeech = VadIsSpeechDetected;

                    //Update the VAD subscribers if necessary
                    if (preSpeech ^ postSpeech)
                    {
                        if (preSpeech)
                            SendStoppedTalking();
                        else
                            SendStartedTalking();
                    }

                    //Preprocess silent frames to make up the missed samples
                    if (missedSamples > 0)
                    {
                        Log.Warn("Lost {0} samples in the preprocess (buffer full), injecting silence to compensate", missedSamples);

                        //Inject silent frames directly into the preprocessor
                        Array.Clear(_intermediateFrame, 0, _intermediateFrame.Length);
                        while (missedSamples >= _intermediateFrame.Length)
                        {
                            PreprocessAudioFrame(_intermediateFrame);
                            missedSamples -= _intermediateFrame.Length;
                        }

                        //Add the remaining samples back onto the missed counter (in case we didn't lose exactly one frame)
                        if (missedSamples > 0)
                            Interlocked.Add(ref _droppedSamples, missedSamples);
                    }
                }
            }
            //ncrunch: no coverage start (Justification: this should never happen, it's just a log in case of sanity failure
            catch (Exception e)
            {
                Log.Error(Log.PossibleBugMessage("Unhandled exception killed audio preprocessor thread: " + e, "02EB75C0-1E12-4109-BFD2-64645C14BD5F"));
            }
            //ncrunch: no coverage end
        }

        /// <summary>
        /// Process a frame of audio. Audio passed to this method arrives in frames of intermediate size/rate (passed into the constructor).
        /// Frames of output size/rate must be passed to SendSamplesToSubscribers.
        /// </summary>
        /// <param name="frame"></param>
        protected abstract void PreprocessAudioFrame([NotNull] float[] frame);

        #region mic subscriptions
        protected void SendSamplesToSubscribers([NotNull] float[] buffer)
        {
            //Write processed audio to file (for diagnostic purposes)
            //ncrunch: no coverage start (Justification: don't want to have to write to disk in a test)
            if (DebugSettings.Instance.EnableRecordingDiagnostics && DebugSettings.Instance.RecordPreprocessorOutput)
            {
                if (_diagnosticOutputRecorder == null)
                {
                    var filename = string.Format("Dissonance_Diagnostics/PreprocessorOutputAudio_{0}", DateTime.UtcNow.ToFileTime());
                    _diagnosticOutputRecorder = new AudioFileWriter(filename, OutputFormat);
                }

                _diagnosticOutputRecorder.WriteSamples(new ArraySegment<float>(buffer));
            }
            //ncrunch: no coverage end

            //Send the audio to subscribers
            using (var unlocker = _micSubscriptions.Lock())
            {
                var subs = unlocker.Value;
                for (var i = 0; i < subs.Count; i++)
                {
                    try
                    {
                        subs[i].ReceiveMicrophoneData(new ArraySegment<float>(buffer), OutputFormat);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Microphone subscriber '{0}' threw: {1}", subs[i].GetType().Name, ex);
                    }
                }
            }
        }

        private void SendResetToSubscribers()
        {
            using (var unlocker = _micSubscriptions.Lock())
            {
                var subs = unlocker.Value;
                for (var i = 0; i < subs.Count; i++)
                {
                    try
                    {
                        subs[i].Reset();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Microphone subscriber '{0}' Reset threw: {1}", subs[i].GetType().Name, ex);
                    }
                }
            }
        }

        public virtual void Subscribe([NotNull] IMicrophoneSubscriber listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");

            using (var subscriptions = _micSubscriptions.Lock())
            {
                subscriptions.Value.Add(listener);
                Interlocked.Increment(ref _micSubscriptionCount);
            }
        }

        public virtual bool Unsubscribe([NotNull] IMicrophoneSubscriber listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");

            using (var subscriptions = _micSubscriptions.Lock())
            {
                var removed = subscriptions.Value.Remove(listener);
                if (removed)
                    Interlocked.Decrement(ref _micSubscriptionCount);
                return removed;
            }
        }
        #endregion

        #region vad subscriptions
        private void SendStoppedTalking()
        {
            using (var unlocker = _vadSubscriptions.Lock())
            {
                var subs = unlocker.Value;
                for (var i = 0; i < subs.Count; i++)
                    SendStoppedTalking(subs[i]);
            }
        }

        private static void SendStoppedTalking([NotNull] IVoiceActivationListener listener)
        {
            try
            {
                listener.VoiceActivationStop();
            }
            catch (Exception ex)
            {
                Log.Error("VAD subscriber '{0}' threw: {1}", listener.GetType().Name, ex);
            }
        }

        private void SendStartedTalking()
        {
            using (var unlocker = _vadSubscriptions.Lock())
            {
                var subs = unlocker.Value;
                for (var i = 0; i < subs.Count; i++)
                    SendStartedTalking(subs[i]);
            }
        }

        private static void SendStartedTalking([NotNull] IVoiceActivationListener listener)
        {
            try
            {
                listener.VoiceActivationStart();
            }
            catch (Exception ex)
            {
                Log.Error("VAD subscriber '{0}' threw: {1}", listener.GetType().Name, ex);
            }
        }

        public virtual void Subscribe([NotNull] IVoiceActivationListener listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");

            using (var subs = _vadSubscriptions.Lock())
            {
                subs.Value.Add(listener);
                Interlocked.Increment(ref _vadSubscriptionCount);

                if (VadIsSpeechDetected)
                    SendStartedTalking(listener);
            }
        }

        public virtual bool Unsubscribe([NotNull] IVoiceActivationListener listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");

            using (var subs = _vadSubscriptions.Lock())
            {
                var removed = subs.Value.Remove(listener);
                if (removed)
                {
                    Interlocked.Decrement(ref _vadSubscriptionCount);

                    if (VadIsSpeechDetected)
                        SendStoppedTalking(listener);
                }
                return removed;
            }
        }
        #endregion
    }
}
