using Bricker.Game;

namespace Bricker.Rendering
{
    /// <summary>
    /// Stores menu properties for rendering.
    /// </summary>
    public class MenuProperties
    {
        //pbublic
        public MenuSelection Selection { get; private set; }
        public bool InGame { get; set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MenuProperties(MenuSelection selection, bool inGame)
        {
            Selection = selection;
            InGame = inGame;
        }

        /// <summary>
        /// Increments selection.
        /// </summary>
        public void IncrementSelection()
        {
            lock (this)
            {
                Selection++;
                if (InGame)
                {
                    if (Selection > MenuSelection.Quit)
                        Selection = MenuSelection.Resume;
                }
                else
                {
                    if (Selection > MenuSelection.Quit)
                        Selection = MenuSelection.New;
                }
            }
        }

        /// <summary>
        /// Decrements selection.
        /// </summary>
        public void DecrementSelection()
        {
            lock (this)
            {
                Selection--;
                if (InGame)
                {
                    if (Selection < MenuSelection.Resume)
                        Selection = MenuSelection.Quit;
                }
                else
                {
                    if (Selection < MenuSelection.New)
                        Selection = MenuSelection.Quit;
                }
            }
        }

    }
}
