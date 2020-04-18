namespace Bricker.Rendering
{
    /// <summary>
    /// Stores score-entry properties for rendering.
    /// </summary>
    public class ScoreEntryProperties
    {
        public string Initials { get; set; }

        public ScoreEntryProperties(string initials)
        {
            Initials = initials;
        }
    }
}
