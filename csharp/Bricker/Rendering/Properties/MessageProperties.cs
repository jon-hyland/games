using System.Linq;

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
        public TextLine[] Lines { get; }
        public MessageButtons Buttons { get; }
        public int ButtonIndex { get; private set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MessageProperties(string line, MessageButtons buttons = MessageButtons.OK, double size = 24)
        {
            Lines = new TextLine[] { new TextLine(line, size) };
            Buttons = buttons;
            ButtonIndex = buttons >= MessageButtons.CancelOK ? 1 : 0;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MessageProperties(string[] lines, MessageButtons buttons = MessageButtons.OK, double size = 24)
        {
            Lines = lines.Select(l => new TextLine(l, size)).ToArray();
            Buttons = buttons;
            ButtonIndex = buttons >= MessageButtons.CancelOK ? 1 : 0;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MessageProperties(TextLine line, MessageButtons buttons = MessageButtons.OK)
        {
            Lines = new TextLine[] { line };
            Buttons = buttons;
            ButtonIndex = buttons >= MessageButtons.CancelOK ? 1 : 0;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MessageProperties(TextLine[] lines, MessageButtons buttons = MessageButtons.OK)
        {
            Lines = lines;
            Buttons = buttons;
            ButtonIndex = buttons >= MessageButtons.CancelOK ? 1 : 0;
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
