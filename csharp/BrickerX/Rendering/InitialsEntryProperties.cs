using Bricker.Configuration;

namespace Bricker.Rendering
{
    /// <summary>
    /// Stores initials-entry properties for rendering.
    /// </summary>
    public class InitialsEntryProperties
    {
        //public
        public string Initials { get; private set; }
        public string[] Header { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public InitialsEntryProperties(string initials, string[] header)
        {
            Initials = initials;
            Header = header;
        }
        
        /// <summary>
        /// Sets user initials.
        /// </summary>
        public void SetInitials(string initials)
        {
            lock (this)
            {
                initials = Config.CleanInitials(initials);
                if (Initials != initials)
                    Initials = initials;
            }
        }

    }
}
