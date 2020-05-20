using Bricker.Configuration;
using Common.Audio;
using Common.Standard.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bricker.Audio
{
    public enum Sounds
    {
        Music1 = 0,
        Test1 = 1
    }

    public class SoundManager : IDisposable
    {
        private readonly AudioManager _manager;

        public SoundManager(IGameConfig config)
        {
            _manager = new AudioManager(new string[]
            {
                Path.Combine(config.ApplicationFolder, "Music1.mp3"),
                Path.Combine(config.ApplicationFolder, "Test1.mp3")
            });
        }

        public void PlaySound(Sounds sound)
        {
            _manager.PlaySound((int)sound);
        }

        public void PlayLoop(Sounds sound)
        {
            _manager.PlayLoop((int)sound);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
