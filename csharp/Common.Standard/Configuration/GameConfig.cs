namespace Common.Standard.Configuration
{
    /// <summary>
    /// Static config accessor available everywhere.  Contains settings 
    /// generic to any game on this framework.  Used by library classes
    /// rather than game classes.
    /// </summary>
    public static class GameConfig
    {
        //private
        private static IGameConfig _config = null;

        //public
        public static IGameConfig Instance => _config;

        /// <summary>
        /// Initialize static class.
        /// </summary>
        public static void Initialize(IGameConfig config)
        {
            _config = config;
        }
    }
}
