using System;
using Dissonance.VAD;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal interface IPreprocessingPipeline
        : IDisposable, IMicrophoneSubscriber
    {
        /// <summary>
        /// Get the format of audio being output from the pipeline
        /// </summary>
        WaveFormat OutputFormat { get; }

        /// <summary>
        /// Get the amplitude of audio at the end of the pipeline
        /// </summary>
        float Amplitude { get; }

        /// <summary>
        /// Perform any startup work required by the pipeline before audio arrives
        /// </summary>
        void Start();

        /// <summary>
        /// Get the size of input frames
        /// </summary>
        int InputFrameSize { get; }

        /// <summary>
        /// Get the size of input frames
        /// </summary>
        int OutputFrameSize { get; }

        void Subscribe(IMicrophoneSubscriber listener);

        bool Unsubscribe(IMicrophoneSubscriber listener);

        void Subscribe(IVoiceActivationListener listener);

        bool Unsubscribe(IVoiceActivationListener listener);
    }
}
