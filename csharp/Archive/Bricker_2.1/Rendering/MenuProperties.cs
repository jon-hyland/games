using Bricker.Game;

namespace Bricker.Rendering
{
    /// <summary>
    /// Stores menu properties for rendering.
    /// </summary>
    public class MenuProperties
    {
        public MenuSelection Selection { get; set; }
        public bool InGame { get; set; }

        public MenuProperties(MenuSelection selection, bool inGame)
        {
            Selection = selection;
            InGame = inGame;
        }
    }
}
