﻿using Bricker.Game;
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
        public static SoundSample Clear1 { get; private set; }
        public static SoundSample Clear2 { get; private set; }
        public static SoundSample Click1 { get; private set; }
        public static SoundSample Click2 { get; private set; }
        public static SoundSample Click3 { get; private set; }
        public static SoundSample Error1 { get; private set; }
        public static SoundSample Explode1 { get; private set; }
        public static SoundSample Explode2 { get; private set; }
        public static SoundSample Explode3 { get; private set; }
        public static SoundSample Hit1 { get; private set; }
        public static SoundSample Hit2 { get; private set; }
        public static SoundSample LevelUp1 { get; private set; }
        public static SoundSample MenuBack1 { get; private set; }
        public static SoundSample MenuMove1 { get; private set; }
        public static SoundSample MenuMove2 { get; private set; }
        public static SoundSample MenuSelect1 { get; private set; }
        public static SoundSample MenuSelect2 { get; private set; }
        public static SoundSample Music1 { get; private set; }
        public static SoundSample Music2 { get; private set; }
        public static SoundSample Send1 { get; private set; }

        /// <summary>
        /// Initializes sound files.
        /// </summary>
        public static void Initialize(IGameConfig config)
        {
            int id = 0;
            Clear1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Clear1.mp3"));
            Clear2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Clear2.mp3"));
            Click1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Click1.mp3"));
            Click2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Click2.mp3"));
            Click3 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Click3.mp3"));
            Error1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Error1.mp3"));
            Explode1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Explode1.mp3"));
            Explode2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Explode2.mp3"));
            Explode3 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Explode3.mp3"));
            Hit1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Hit1.mp3"));
            Hit2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Hit2.mp3"), 0.8f);
            LevelUp1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "LevelUp1.mp3"));
            MenuBack1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "MenuBack1.mp3"));
            MenuMove1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "MenuMove1.mp3"));
            MenuMove2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "MenuMove2.mp3"));
            MenuSelect1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "MenuSelect1.mp3"));
            MenuSelect2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "MenuSelect2.mp3"));
            Music1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Music1.mp3"), 0.1f, true);
            Music2 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Music2.mp3"), 0.2f, true);
            Send1 = new SoundSample(++id, Path.Combine(config.AudioSampleFolder, "Send1.mp3"));
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
        private static MusicMode _musicMode;

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
            if ((sound.IsMusic) && (!GameConfig.Instance.Music))
                return;
            if ((!sound.IsMusic) && (!GameConfig.Instance.SoundEffects))
                return;

            _manager?.PlaySound(sound.ID);
        }

        /// <summary>
        /// Plays the specified sound sample, looping until stop.
        /// Stops any existing loop.
        /// </summary>
        public static void Loop(SoundSample sound, long position = 0)
        {
            if (sound == Sound.Music1)
                _musicMode = MusicMode.Music1;
            else if (sound == Sound.Music2)
                _musicMode = MusicMode.Music2;
            else
                _musicMode = MusicMode.None;

            if ((sound.IsMusic) && (!GameConfig.Instance.Music))
                return;
            if ((!sound.IsMusic) && (!GameConfig.Instance.SoundEffects))
                return;

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
        /// Resets sound after audio enabled or disabled.
        /// </summary>
        public static void Reset()
        {
            if (GameConfig.Instance.Music)
            {
                if (_musicMode == MusicMode.Music1)
                    Loop(Sound.Music1, Main.MusicPosition);
                else if (_musicMode == MusicMode.Music2)
                    Loop(Sound.Music2);
                else
                    StopLoops();
            }
            else
            {
                StopLoops();
            }
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public static void Dispose()
        {
            _manager?.Dispose();
        }

        /// <summary>
        /// Represents the music that should be playing (if it were enabled).
        /// </summary>
        private enum MusicMode
        {
            None,
            Music1,
            Music2
        }
    }
}
