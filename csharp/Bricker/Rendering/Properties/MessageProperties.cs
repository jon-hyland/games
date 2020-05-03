namespace Bricker.Rendering.Properties
{
    /// <summary>
    /// Represents buttons displayed under message.
    /// </summary>
    public enum MessageButtons
    {
        None = 0,
        OK = 1,
        CancelOK = 2,
        NoYes = 3
    }

    /// <summary>
    /// Stores message properties for rendering.
    /// </summary>
    public class MessageProperties
    {
        //public
        public string[] Text { get; }
        public double Size { get; }
        public MessageButtons Buttons { get; }
        public int ButtonIndex { get; private set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MessageProperties(string[] text, double size, MessageButtons buttons, int buttonIndex)
        {
            Text = text;
            Size = size;
            Buttons = buttons;
            ButtonIndex = buttonIndex;
        }

        /// <summary>
        /// Increments button index.
        /// </summary>
        public void IncrementIndex()
        {
            ButtonIndex++;
            if (ButtonIndex > 1)
                ButtonIndex = 0;
        }

        /// <summary>
        /// Decrements button index.
        /// </summary>
        public void DecrementsIndex()
        {
            ButtonIndex--;
            if (ButtonIndex < 0)
                ButtonIndex = 1;
        }

    }
}
