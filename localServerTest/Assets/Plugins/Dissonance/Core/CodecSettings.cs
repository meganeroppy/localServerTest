using Dissonance.Audio.Codecs;
using Dissonance.Audio.Codecs.Identity;
using Dissonance.Audio.Codecs.Opus;
using Dissonance.Config;

namespace Dissonance
{
    internal class CodecSettings
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(CodecSettings).Name);

        private bool _started;

        private bool _settingsReady;
        private readonly object _settingsWriteLock = new object();

        private uint _decoderFrameSize;
        public uint FrameSize
        {
            get
            {
                Generate();
                return _decoderFrameSize;
            }
        }

        private int _decoderSampleRate;
        public int SampleRate
        {
            get
            {
                Generate();
                return _decoderSampleRate;
            }
        }

        private AudioQuality _encoderQuality;
        private FrameSize _encoderFrameSize;

        private Codec _codec = Codec.Opus;
        #endregion

        public void Start(Codec codec = Codec.Opus)
        {
            //Save encoder settings to ensure we use the same settings every time it is restarted
            _codec = codec;
            _encoderQuality = VoiceSettings.Instance.Quality;
            _encoderFrameSize = VoiceSettings.Instance.FrameSize;
            _started = true;
        }

        private void Generate()
        {
            if (!_started)
                throw Log.CreatePossibleBugException("Attempted to use codec settings before codec settings loaded", "9D4F1C1E-9C09-424A-86F7-B633E71EF100");

            if (!_settingsReady)
            {
                lock (_settingsWriteLock)
                {
                    if (!_settingsReady)
                    {
                        //Create and destroy an encoder to determine the decoder settings to use
                        var encoder = CreateEncoder(_encoderQuality, _encoderFrameSize);
                        _decoderFrameSize = (uint)encoder.FrameSize;
                        _decoderSampleRate = encoder.SampleRate;
                        encoder.Dispose();

                        _settingsReady = true;
                    }
                }
            }
        }

        [NotNull] private IVoiceEncoder CreateEncoder(AudioQuality quality, FrameSize frameSize)
        {
            switch (_codec)
            {
                case Codec.Identity:
                    return new IdentityEncoder(44100, 441);
                case Codec.Opus:
                    return new OpusEncoder(quality, frameSize);

                default:
                    throw Log.CreatePossibleBugException(string.Format("Unknown Codec {0}", _codec), "6232F4FA-6993-49F9-AA79-2DBCF982FD8C");
            }
        }

        [NotNull] public IVoiceEncoder CreateEncoder()
        {
            if (!_started)
                throw Log.CreatePossibleBugException("Attempted to use codec settings before codec settings loaded", "0BF71972-B96C-400B-B7D9-3E2AEE160470");

            return CreateEncoder(_encoderQuality, _encoderFrameSize);
        }
    }
}
