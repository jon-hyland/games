using Common.Standard.Configuration;

namespace Common.Audio
{
    public static class AudioProps
    {
        public static bool Music { get; set; }
        public static bool SoundEffects { get; set; }

        public static void Initialize(IGameConfig config)
        {
            Music = config.Music;
            SoundEffects = config.SoundEffects;
        }
    }
}
