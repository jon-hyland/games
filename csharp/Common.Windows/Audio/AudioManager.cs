using System;
using System.Collections.Generic;

namespace Common.Audio
{
    /// <summary>
    /// Wraps audio engine, preloads sounds, plays by index, allows loops.
    /// </summary>
    public class AudioManager : IDisposable
    {
        //private
        private readonly AudioEngine _engine;
        private readonly Dictionary<int, CachedSound> _sounds;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public AudioManager(IEnumerable<SoundSample> sounds, int sampleRate = 48000, int channels = 2)
        {
            _engine = new AudioEngine(sampleRate, channels);
            _sounds = new Dictionary<int, CachedSound>();
            foreach (SoundSample s in sounds)
                if (!_sounds.ContainsKey(s.ID))
                    _sounds.Add(s.ID, new CachedSound(s.File, sampleRate, channels, s.Volume));
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public AudioManager(IList<string> files, int sampleRate = 48000, int channels = 2)
        {
            _engine = new AudioEngine(sampleRate, channels);
            _sounds = new Dictionary<int, CachedSound>();
            for (int i = 0; i < files.Count; i++)
                _sounds.Add(i, new CachedSound(files[i], sampleRate, channels));
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            _engine.Dispose();
        }

        /// <summary>
        /// Plays specified sound.
        /// </summary>
        public void PlaySound(int id)
        {
            _engine.PlaySound(_sounds[id]);
        }

        /// <summary>
        /// Plays specified sound on a loop.
        /// </summary>
        public void PlayLoop(int id, bool stopOtherLoops = true, long position = 0)
        {
            _engine.PlayLoop(_sounds[id], stopOtherLoops, position);
        }

        /// <summary>
        /// Stops specified sound, if it's looping.  Returns current position (if possible), or 0.
        /// </summary>
        public long StopLoop(int id)
        {
            return _engine.StopLoop(_sounds[id]);
        }

        /// <summary>
        /// Stops all loops.
        /// </summary>
        public void StopAllLoops()
        {
            _engine.StopAllLoops();
        }

        /// <summary>
        /// Returns current position in specified looping sound, or 0 if not looping sound.
        /// </summary>
        public long GetLoopPosition(int id)
        {
            return _engine.GetLoopPosition(_sounds[id]);
        }

    }

    /// <summary>
    /// Represents a sound that can be played.
    /// </summary>
    public class SoundSample
    {
        public int ID { get; }
        public string File { get; }
        public float Volume { get; }
        public bool IsMusic { get; }

        public SoundSample(int id, string file, float volume = 1.0f, bool isMusic = false)
        {
            ID = id;
            File = file;
            Volume = volume;
            IsMusic = isMusic;
        }
    }



}
