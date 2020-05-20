using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Audio
{
    /// <summary>
    /// Opens constant output connection to audio device, continuously sends audio data (silence if nothing playing),
    /// allows mixing of multiple audio signals, preloading/caching of sounds, and looping background sound (for music).
    /// http://mark-dot-net.blogspot.com/2014/02/fire-and-forget-audio-playback-with.html
    /// </summary>
    public class AudioEngine : IDisposable
    {
        //private
        private readonly IWavePlayer _outputDevice;
        private readonly MixingSampleProvider _mixer;
        private readonly List<CachedSoundProvider> _loops;

        /// <summary>
        /// Class constructor,
        /// </summary>
        public AudioEngine(int sampleRate = 44100, int channels = 2)
        {
            _outputDevice = new WaveOutEvent();
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
            _loops = new List<CachedSoundProvider>();
            _mixer.ReadFully = true;
            _mixer.MixerInputEnded += Mixer_MixerInputEnded;
            _outputDevice.Init(_mixer);
            _outputDevice.Play();
        }

        /// <summary>
        /// Adds specified sample to the mix (plays the sound).
        /// </summary>
        private void AddMixerInput(ISampleProvider input)
        {
            _mixer.AddMixerInput(ConvertToRightChannelCount(input));
        }

        /// <summary>
        /// Checks number of channels is correct or throws error.  Tries to convert mono to stereo,
        /// add more conversions later.
        /// </summary>
        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == _mixer.WaveFormat.Channels)
                return input;
            if ((input.WaveFormat.Channels == 1) && (_mixer.WaveFormat.Channels == 2))
                return new MonoToStereoSampleProvider(input);
            throw new NotImplementedException("Channel count not yet implemented");
        }

        /// <summary>
        /// Fired when input has ended.  Allows restart of looped sounds.
        /// </summary>
        private void Mixer_MixerInputEnded(object sender, SampleProviderEventArgs e)
        {
            CachedSoundProvider provider = ((e.SampleProvider != null) && (e.SampleProvider is CachedSoundProvider)) ? (CachedSoundProvider)e.SampleProvider : null;
            if (provider == null)
                return;
            lock (_loops)
            {
                if (_loops.Contains(provider))
                {
                    provider.Reset();
                    AddMixerInput(provider);
                }
            }
        }

        /// <summary>
        /// Plays a sound file without preloading or caching.  Must be in correct format.
        /// </summary>
        public void PlaySound(string file)
        {
            AudioFileReader input = new AudioFileReader(file);
            AddMixerInput(new AutoDisposeFileReader(input));
        }

        /// <summary>
        /// Plays a cached sound.
        /// </summary>
        public void PlaySound(CachedSound sound)
        {
            AddMixerInput(new CachedSoundProvider(sound));
        }

        /// <summary>
        /// Plays a cached sound, looping forever until stop.  Allows only one instance of specified sound.
        /// </summary>
        public void PlayLoop(CachedSound sound)
        {
            CachedSoundProvider provider;
            lock (_loops)
            {
                StopLoop(sound);
                provider = new CachedSoundProvider(sound);
                _loops.Add(provider);
            }
            AddMixerInput(provider);
        }

        /// <summary>
        /// Stops the specified sound, if it's looping.
        /// </summary>
        public void StopLoop(CachedSound sound)
        {
            lock (_loops)
            {
                CachedSoundProvider provider = _loops
                    .Where(p => p.Sound == sound)
                    .FirstOrDefault();
                if (provider != null)
                {
                    provider.Stop();
                    _loops.Remove(provider);
                }
            }
        }

        /// <summary>
        /// Stops all looped sounds.
        /// </summary>
        public void StopAllLoops()
        {
            lock (_loops)
            {
                foreach (CachedSoundProvider provider in _loops)
                    provider.Stop();
                _loops.Clear();
            }
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            lock (_loops)
            {
                foreach (CachedSoundProvider p in _loops)
                    p.Stop();
            }
            _outputDevice?.Dispose();
        }

        /// <summary>
        /// Gets static instance of class, with defaults.
        /// </summary>
        public static readonly AudioEngine Instance = new AudioEngine(44100, 2);
    }

    /// <summary>
    /// Loads a sound file to be cached in memory and played on demand.
    /// </summary>
    public class CachedSound
    {
        //public
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CachedSound(string file)
        {
            using (AudioFileReader reader = new AudioFileReader(file))
            {
                //todo: add resampling here?
                WaveFormat = reader.WaveFormat;
                List<float> wholeFile = new List<float>((int)(reader.Length / 4));
                float[] buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                    wholeFile.AddRange(buffer.Take(samplesRead));
                AudioData = wholeFile.ToArray();
            }
        }
    }

    /// <summary>
    /// Wraps a cached sound, allows playback.
    /// </summary>
    public class CachedSoundProvider : ISampleProvider
    {
        //private
        private readonly CachedSound _sound;
        private long _position;
        private bool _stop;

        //public
        public CachedSound Sound => _sound;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CachedSoundProvider(CachedSound sound)
        {
            _sound = sound;
        }

        /// <summary>
        /// Reads data from the cached buffer, for playback.
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            if (_stop)
            {
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = 0f;
                return 0;
            }

            long availableSamples = _sound.AudioData.Length - _position;
            long samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(_sound.AudioData, _position, buffer, offset, samplesToCopy);
            _position += samplesToCopy;
            return (int)samplesToCopy;
        }

        /// <summary>
        /// Sets flag to return no more data (end playback).
        /// </summary>
        public void Stop()
        {
            _stop = true;
        }

        /// <summary>
        /// Resets provider to play again, from beginning.
        /// </summary>
        public void Reset()
        {
            _position = 0;
            _stop = false;
        }

        /// <summary>
        /// Returns wave format of cached sound.
        /// </summary>
        public WaveFormat WaveFormat { get { return _sound.WaveFormat; } }
    }

    /// <summary>
    /// Helper class that reads an audio file and automatically disposes of the reader.
    /// </summary>
    public class AutoDisposeFileReader : ISampleProvider
    {
        //private
        private readonly AudioFileReader _reader;
        private bool _isDisposed;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public AutoDisposeFileReader(AudioFileReader reader)
        {
            _reader = reader;
            WaveFormat = reader.WaveFormat;
        }

        /// <summary>
        /// Reads data from the file stream, disposes reader when done.
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            if (_isDisposed)
                return 0;
            int read = _reader.Read(buffer, offset, count);
            if (read == 0)
            {
                _reader.Dispose();
                _isDisposed = true;
            }
            return read;
        }

        /// <summary>
        /// Returns wave format of file.
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }
    }


}
