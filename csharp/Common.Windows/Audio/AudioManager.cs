using System;
using System.Linq;

namespace Common.Audio
{
    /// <summary>
    /// Wraps audio engine, preloads sounds, plays by index, allows loops.
    /// </summary>
    public class AudioManager : IDisposable
    {
        //private
        private readonly AudioEngine _engine;
        private readonly CachedSound[] _sounds;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public AudioManager(string[] files, int sampleRate = 44100, int channels = 2)
        {
            _engine = new AudioEngine(sampleRate, channels);
            _sounds = files.Select(f => new CachedSound(f)).ToArray();
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
        public void PlaySound(int index)
        {
            _engine.PlaySound(_sounds[index]);
        }

        /// <summary>
        /// Plays specified sound on a loop.
        /// </summary>
        public void PlayLoop(int index, bool stopAllLoops = true)
        {
            if (stopAllLoops)
                _engine.StopAllLoops();
            _engine.PlayLoop(_sounds[index]);            
        }

        /// <summary>
        /// Stops specified loop.
        /// </summary>
        public void StopLoop(int index)
        {
            _engine.StopLoop(_sounds[index]);
        }

        /// <summary>
        /// Stops all loops.
        /// </summary>
        public void StopAllLoops()
        {
            _engine.StopAllLoops();
        }
    }



}
