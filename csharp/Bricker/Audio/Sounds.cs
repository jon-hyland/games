using Common.Audio;
using Common.Standard.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bricker.Audio
{
    /// <summary>
    /// Defines sound samples for Bricker game.
    /// </summary>
    public static class Sound
    {
        //public
        public static SoundSample Music1 { get; private set; }
        public static SoundSample Music2 { get; private set; }
        public static SoundSample MenuMove1 { get; private set; }
        public static SoundSample MenuMove2 { get; private set; }
        public static SoundSample MenuBack1 { get; private set; }
        public static SoundSample MenuSelect1 { get; private set; }
        public static SoundSample MenuSelect2 { get; private set; }
        public static SoundSample Error1 { get; private set; }
        public static SoundSample Click1 { get; private set; }
        public static SoundSample Click2 { get; private set; }
        public static SoundSample Click3 { get; private set; }

        /// <summary>
        /// Initializes sound files.
        /// </summary>
        public static void Initialize(IGameConfig config)
        {
            int id = 0;
            Music1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Music1.mp3"), 0.1f);
            Music2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Music2.mp3"), 0.2f);
            MenuMove1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "MenuMove1.mp3"));
            MenuMove2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "MenuMove2.mp3"));
            MenuBack1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "MenuBack1.mp3"));
            MenuSelect1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "MenuSelect1.mp3"));
            MenuSelect2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "MenuSelect2.mp3"));
            Error1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Error1.mp3"));
            Click1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Click1.mp3"));
            Click2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Click2.mp3"));
            Click3 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Click3.mp3"));
        }

        /// <summary>
        /// Gets all sounds as a list.
        /// </summary>
        public static List<SoundSample> GetSounds()
        {
            return typeof(Sound)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Select(p => p.GetValue(null, null))
                .OfType<SoundSample>()
                .ToList();
        }
    }

    /// <summary>
    /// Sound manager for Bricker game.
    /// </summary>
    public static class Sounds
    {
        //private
        private static AudioManager _manager;

        /// <summary>
        /// Initilizes sound, loads samples.  Can be done on background thread.
        /// </summary>
        public static void Initialize(IGameConfig config)
        {
            Sound.Initialize(config);
            _manager = new AudioManager(Sound.GetSounds());
        }

        /// <summary>
        /// Plays the specified sound sample.
        /// </summary>
        public static void Play(SoundSample sound)
        {
            _manager?.PlaySound(sound.ID);
        }

        /// <summary>
        /// Plays the specified sound sample, looping until stop.
        /// Stops any existing loop.
        /// </summary>
        public static void Loop(SoundSample sound, long position = 0)
        {
            _manager?.PlayLoop(id: sound.ID, stopOtherLoops: true, position: position);
        }

        /// <summary>
        /// Stops specified sound, if it's looping.  Returns current position (if possible), or 0.
        /// </summary>
        public static long StopLoop(SoundSample sound)
        {
            return _manager?.StopLoop(sound.ID) ?? 0;
        }

        /// <summary>
        /// Stops any sound loops.
        /// </summary>
        public static void StopLoops()
        {
            _manager?.StopAllLoops();
        }

        /// <summary>
        /// Returns current position in specified looping sound, or 0 if not looping sound.
        /// </summary>
        public static long GetLoopPosition(SoundSample sound)
        {
            return _manager?.GetLoopPosition(sound.ID) ?? 0;
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public static void Dispose()
        {
            _manager?.Dispose();
        }
    }
}
