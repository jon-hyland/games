using System;

namespace Common.Audio
{
    public class SoundManager : IDisposable
    {
        //private
        private readonly AudioEngine _engine;
        private readonly CachedSound[] _sounds;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public SoundManager(int sampleRate = 44100, int channels = 2, string[] soundFiles)
        {
            _engine = new AudioEngine(sampleRate, channels);
        }

        public void Stop()
        {
            Dispose();
        }

        public void Dispose()
        {
            _backgroundLoop?.Dispose();
        }

        public void StartBackgroundLoop(string file, double volume = 1.0)
        {
            _backgroundLoop?.Dispose();
            _backgroundLoop = new SoundLoop(file, volume);
        }


    }

    //public class SoundEffect : IDisposable
    //{
    //    private readonly AudioFileReader _reader;
    //    private readonly WaveOutEvent _waveOut;

    //    public SoundEffect(string file, double volume = 1.0)
    //    {
    //        _reader = new AudioFileReader(file);
    //        _waveOut = new WaveOutEvent();
    //        _waveOut.Init(_reader);
    //        _waveOut.Volume = (float)volume;
    //    }

    //    public void Play()
    //    {
    //        _waveOut.Play();
    //    }

    //    public void Dispose()
    //    {
    //        _waveOut?.Dispose();
    //        _reader?.Dispose();
    //    }
    //}

    //public class SoundLoop : IDisposable
    //{
    //    private readonly AudioFileReader _reader;
    //    private readonly WaveOutEvent _waveOut;
    //    private bool _stop;

    //    public SoundLoop(string file, double volume = 1.0)
    //    {
    //        _reader = new AudioFileReader(file);
    //        _waveOut = new WaveOutEvent();
    //        _stop = false;
    //        _waveOut.PlaybackStopped += (s, e) =>
    //        {
    //            if (_stop)
    //                return;
    //            _reader.Position = 0;
    //            _waveOut.Play();
    //        };
    //        _waveOut.Init(_reader);
    //        _waveOut.Volume = (float)volume;
    //        _waveOut.Play();
    //    }

    //    public void Stop()
    //    {
    //        _stop = true;
    //        _waveOut?.Stop();
    //    }

    //    public void Dispose()
    //    {
    //        Stop();
    //        _waveOut?.Dispose();
    //        _reader?.Dispose();
    //    }
    //}


}
