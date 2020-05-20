using Common.Audio;
using Common.Standard.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bricker.Audio
{
    public static class Sounds
    {
        public static Sound Music1 { get; private set; }
        public static Sound Test1 { get; private set; }

        public static void Initialize(IGameConfig config)
        {
            Music1 = new Sound(1, Path.Combine(config.ApplicationFolder, "Music1.mp3"));
            Test1 = new Sound(2, Path.Combine(config.ApplicationFolder, "Test1.mp3"));
        }

        public static List<Sound> GetSounds()
        {
            return typeof(Sounds)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Select(p => p.GetValue(null, null))
                .OfType<Sound>()
                .ToList();
        }
    }

    public class SoundManager : IDisposable
    {
        private readonly AudioManager _manager;

        public SoundManager(IGameConfig config)
        {
            Sounds.Initialize(config);
            _manager = new AudioManager(Sounds.GetSounds());
        }

        public void PlaySound(Sound sound)
        {
            _manager.PlaySound(sound.ID);
        }

        public void PlayLoop(Sound sound)
        {
            _manager.PlayLoop(sound.ID);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
