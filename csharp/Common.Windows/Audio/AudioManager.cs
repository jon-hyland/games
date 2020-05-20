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
        private readonly Dictionary<int, Sound> _sounds;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public AudioManager(IEnumerable<Sound> sounds, int sampleRate = 44100, int channels = 2)
        {
            _engine = new AudioEngine(sampleRate, channels);
            _sounds = new Dictionary<int, Sound>();
            foreach (Sound s in sounds)
                if (!_sounds.ContainsKey(s.ID))
                    _sounds.Add(s.ID, s);
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public AudioManager(IList<string> files, int sampleRate = 44100, int channels = 2)
        {
            _engine = new AudioEngine(sampleRate, channels);
            _sounds = new Dictionary<int, Sound>();
            for (int i = 0; i < files.Count; i++)
                _sounds.Add(i, new Sound(i, files[i]));
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
            _engine.PlaySound(_sounds[id].Cache);
        }

        /// <summary>
        /// Plays specified sound on a loop.
        /// </summary>
        public void PlayLoop(int id, bool stopAllLoops = true)
        {
            if (stopAllLoops)
                _engine.StopAllLoops();
            _engine.PlayLoop(_sounds[id].Cache);
        }

        /// <summary>
        /// Stops specified loop.
        /// </summary>
        public void StopLoop(int id)
        {
            _engine.StopLoop(_sounds[id].Cache);
        }

        /// <summary>
        /// Stops all loops.
        /// </summary>
        public void StopAllLoops()
        {
            _engine.StopAllLoops();
        }
    }

    /// <summary>
    /// Represents a sound that can be played.
    /// </summary>
    public class Sound
    {
        public int ID { get; }
        public string File { get; }
        public CachedSound Cache { get; }

        public Sound(int id, string file)
        {
            ID = id;
            File = file;
            Cache = new CachedSound(file);
        }
    }



}
