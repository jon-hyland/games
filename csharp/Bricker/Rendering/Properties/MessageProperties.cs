﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public DateTime TimeoutTime { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MessageProperties(string line, MessageButtons buttons = MessageButtons.OK, double size = 24, int timeoutSecs = -1)
        {
            List<string> lines1 = line.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
            List<string> lines2 = new List<string>();
            foreach (string l in lines1)
                lines2.AddRange(WrapText(l, 100));
            Lines = lines2.Select(l => new TextLine(l, size)).ToArray();
            Buttons = buttons;
            ButtonIndex = buttons >= MessageButtons.CancelOK ? 1 : 0;
            TimeoutTime = timeoutSecs > 0 ? DateTime.Now.AddSeconds(timeoutSecs) : DateTime.Now.AddHours(24);
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MessageProperties(string[] lines, MessageButtons buttons = MessageButtons.OK, double size = 24, int timeoutSecs = -1)
        {
            Lines = lines.Select(l => new TextLine(l, size)).ToArray();
            Buttons = buttons;
            ButtonIndex = buttons >= MessageButtons.CancelOK ? 1 : 0;
            TimeoutTime = timeoutSecs > 0 ? DateTime.Now.AddSeconds(timeoutSecs) : DateTime.Now.AddHours(24);
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MessageProperties(TextLine line, MessageButtons buttons = MessageButtons.OK, int timeoutSecs = -1)
        {
            Lines = new TextLine[] { line };
            Buttons = buttons;
            ButtonIndex = buttons >= MessageButtons.CancelOK ? 1 : 0;
            TimeoutTime = timeoutSecs > 0 ? DateTime.Now.AddSeconds(timeoutSecs) : DateTime.Now.AddHours(24);
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MessageProperties(TextLine[] lines, MessageButtons buttons = MessageButtons.OK, int timeoutSecs = -1)
        {
            Lines = lines;
            Buttons = buttons;
            ButtonIndex = buttons >= MessageButtons.CancelOK ? 1 : 0;
            TimeoutTime = timeoutSecs > 0 ? DateTime.Now.AddSeconds(timeoutSecs) : DateTime.Now.AddHours(24);
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

        private static List<string> WrapText(string input, int maxLineSize)
        {
            string[] split = input.Split(new char[] { ' ' }, StringSplitOptions.None);
            List<string> lines = new List<string>();
            StringBuilder sb = new StringBuilder();
            foreach (string word in split)
            {
                if (sb.Length == 0)
                {
                    sb.Append(word);
                }
                else
                {
                    if ((sb.Length + word.Length + 1) > maxLineSize)
                    {
                        lines.Add(sb.ToString());
                        sb.Clear();
                    }
                    sb.Append(" " + word);
                }
            }
            lines.Add(sb.ToString());
            return lines;
        }

    }
}
