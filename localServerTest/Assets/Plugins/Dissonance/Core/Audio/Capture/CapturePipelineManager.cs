using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Networking;
using Dissonance.VAD;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal class CapturePipelineManager
        : IAmplitudeProvider
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(CapturePipelineManager).Name);

        private readonly CodecSettings _codecSettings;
        private readonly RoomChannels _roomChannels;
        private readonly PlayerChannels _playerChannels;
        private readonly PacketLossMonitor _receivingPacketLossMonitor;
        [CanBeNull] private ICommsNetwork _network;

        private IMicrophoneCapture _microphone;
        private IPreprocessingPipeline _preprocessor;
        private EncoderPipeline _encoder;

        private bool _encoderSubscribed;

        private FrameSkipDetector _skipDetector = new FrameSkipDetector(
            maxFrameTime: TimeSpan.FromMilliseconds(150),
            minimumBreakerDuration: TimeSpan.FromMilliseconds(350),
            maxBreakerDuration: TimeSpan.FromMilliseconds(10000),
            breakerResetPerSecond: TimeSpan.FromMilliseconds(250)
        );

        private readonly List<IVoiceActivationListener> _activationListeners = new List<IVoiceActivationListener>();

        [CanBeNull] public IMicrophoneCapture Microphone
        {
            get { return _microphone; }
        }

        private string _micName;
        public string MicrophoneName
        {
            get { return _micName; }
            set
            {
                //Early exit if the value isn't actually changing
                if (_micName == value)
                    return;

                if (_microphone != null && _microphone.IsRecording)
                    Log.Info("Changing microphone device from '{0}' to '{1}'", _micName, value);

                //Save the mic name and force a reset, this will pick up the new name as part of the reset
                _micName = value;
                RestartTransmissionPipeline();
            }
        }

        public float PacketLoss
        {
            get { return _receivingPacketLossMonitor.PacketLoss; }
        }

        public float Amplitude
        {
            get { return _preprocessor == null ? 0 : _preprocessor.Amplitude; }
        }
        #endregion

        #region constructor
        public CapturePipelineManager([NotNull] CodecSettings codecSettings, [NotNull] RoomChannels roomChannels, [NotNull] PlayerChannels playerChannels, [NotNull] ReadOnlyCollection<VoicePlayerState> players)
        {
            if (codecSettings == null) throw new ArgumentNullException("codecSettings");
            if (roomChannels == null) throw new ArgumentNullException("roomChannels");
            if (playerChannels == null) throw new ArgumentNullException("playerChannels");
            if (players == null) throw new ArgumentNullException("players");

            _codecSettings = codecSettings;
            _roomChannels = roomChannels;
            _playerChannels = playerChannels;
            _receivingPacketLossMonitor = new PacketLossMonitor(players);
        }
        #endregion

        public void Start([NotNull] ICommsNetwork network, [NotNull] IMicrophoneCapture microphone)
        {
            if (network == null) throw new ArgumentNullException("network");
            if (microphone == null) throw new ArgumentNullException("microphone");

            _microphone = microphone;
            _network = network;

            Net_ModeChanged(network.Mode);
            network.ModeChanged += Net_ModeChanged;
        }

        public void Destroy()
        {
            Log.Debug("Destroying");

            if (!ReferenceEquals(_network, null))
                _network.ModeChanged -= Net_ModeChanged;

            StopTransmissionPipeline();
        }

        private void Net_ModeChanged(NetworkMode mode)
        {
            if (mode.IsClientEnabled())
                RestartTransmissionPipeline();
            else
                StopTransmissionPipeline();
        }

        public void Update(bool muted, float deltaTime)
        {
			_receivingPacketLossMonitor.Update();

            //Update microphone and reset it either if there is a frame skip or the microphone requests a reset
            var skipped = _skipDetector.IsFrameSkip(deltaTime);
            var request = _microphone.IsRecording && _microphone.UpdateSubscribers();
            if (skipped || request)
            {
                if (skipped)
                    Log.Warn("Detected a frame skip, forcing capture pipeline reset");

                RestartTransmissionPipeline();
            }

            if (_encoder != null)
            {
                //If the encoder is finally stopped and still subscribed then unsubscribe and reset it. This puts it into a state ready to be used again.
                if (_encoder.Stopped && _encoderSubscribed)
                {
                    Log.Debug("Unsubscribing encoder from preprocessor");

                    _preprocessor.Unsubscribe(_encoder);
                    _encoder.Reset();
                    _encoderSubscribed = false;
                }

                //Check if the encoder state matches the desired state
                var shouldSub = !(_encoder.Stopping && !_encoder.Stopped) && !muted && (_roomChannels.Count + _playerChannels.Count) > 0;
                if (shouldSub != _encoderSubscribed)
                {
                    if (shouldSub)
                    {
                        Log.Debug("Subscribing encoder to preprocessor");

                        _encoder.Reset();
                        _preprocessor.Subscribe(_encoder);
                        _encoderSubscribed = true;
                    }
                    else
                    {
                        //Inform the encoder that it should stop encoding (after sending one final packet)
                        if (!_encoder.Stopping)
                        {
                            _encoder.Stop();
                            Log.Debug("Stopping encoder");
                        }
                        else
                        {
                            Log.Debug("Waiting for encoder to send last packet");
                        }
                    }
                }
                else
                {
                    //Log.Debug("Should Sub - Stopping:{0} Muted:{1}", _encoder.Stopping, muted);
                }

                //Propogate measured *incoming* packet loss to encoder as expected *outgoing* packet loss
                if (_encoder != null)
                    _encoder.TransmissionPacketLoss = _receivingPacketLossMonitor.PacketLoss;
            }
        }

        /// <summary>
        /// Immediately stop the entire transmission system
        /// </summary>
        private void StopTransmissionPipeline()
        {
            //Stop microphone
            if (_microphone != null && _microphone.IsRecording)
                _microphone.StopCapture();

            //Dispose preprocessor and encoder
            if (_preprocessor != null)
            {
                if (_microphone != null)
                    _microphone.Unsubscribe(_preprocessor);
                if (_encoder != null)
                    _preprocessor.Unsubscribe(_encoder);

                _preprocessor.Dispose();
                _preprocessor = null;
            }

            if (_encoder != null)
            {
                _encoder.Dispose();
                _encoder = null;
            }

            _encoderSubscribed = false;
        }

        /// <summary>
        /// (Re)Start the transmission pipeline, getting to a start where we *can* send voice (but aren't yet)
        /// </summary>
        private void RestartTransmissionPipeline()
        {
            StopTransmissionPipeline();

            //No point starting a transmission pipeline if the network is not a client
            if (_network == null || !_network.Mode.IsClientEnabled())
                return;

            //Create new mic capture system
            var format = _microphone.StartCapture(_micName);

            //If we created a mic (can be null if e.g. there is no mic)
            if (format != null)
            {
                //Close and re-open all channels, propogating this restart to the receiving end
                _roomChannels.Refresh();
                _playerChannels.Refresh();

                //Create preprocessor and subscribe it to microphone (webrtc preprocessor always wants audio to drive VAD+AEC)
                _preprocessor = CreatePreprocessor(format);
                _preprocessor.Start();
                _microphone.Subscribe(_preprocessor);

                //Sub VAD listeners to preprocessor
                for (var i = 0; i < _activationListeners.Count; i++)
                    _preprocessor.Subscribe(_activationListeners[i]);

                //Create encoder (not yet subscribed to receive audio data, we'll do that later)
                Log.AssertAndThrowPossibleBug(_network != null, "5F33336B-15B5-4A85-9B54-54352C74768E", "Network object is unexpectedly null");
                _encoder = new EncoderPipeline(_preprocessor.OutputFormat, _codecSettings.CreateEncoder(), _network);
            }
            else
                Log.Warn("Failed to start microphone capture; local voice transmission will be disabled.");
        }

        // ncrunch: no coverage start
        // Justification: we don't want to load the webrtc preprocessing DLL into tests so we're faking a preprocessor in a derived test class)
        [NotNull] protected virtual IPreprocessingPipeline CreatePreprocessor([NotNull] WaveFormat format)
        {
            return new WebRtcPreprocessingPipeline(format);
        }
        //ncrunch: no coverage end

        #region VAD subscribers
        public void Subscribe([NotNull] IVoiceActivationListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener", "Cannot subscribe with a null listener");

            _activationListeners.Add(listener);

            if (_preprocessor != null)
                _preprocessor.Subscribe(listener);
        }

        public void Unsubscribe([NotNull] IVoiceActivationListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener", "Cannot unsubscribe with a null listener");

            _activationListeners.Remove(listener);

            if (_preprocessor != null)
                _preprocessor.Unsubscribe(listener);
        }
        #endregion

#if UNITY_EDITOR || NCRUNCH
        // We obviously can't run a realtime audio pipeline while the editor is paused. Encoding happens on...
        // ...another thread but we have to get microphone data on the main thread and that's not going...
        // ...to happen. Stop the pipeline until the editor is unpaused and then resume the pipeline.

        //ncrunch: no coverage start
        public void Pause()
        {
            StopTransmissionPipeline();
        }

        public void Resume()
        {
            RestartTransmissionPipeline();
        }
        //ncrunch: no coverage end
#endif
    }
}
