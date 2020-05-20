using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bricker.Rendering.Properties
{
    public class SettingsProperties
    {
        public SettingsMenuItem[] Items { get; }
        public 

    }

    public class SettingsMenuItem
    {
        public string OnCaption { get; }
        public string OffCaption { get; }
        public bool Value { get; set; }
        public string Caption => Value ? OnCaption : OffCaption;

        public SettingsMenuItem(string onCaption, string offCaption, bool value)
        {
            OnCaption = onCaption;
            OffCaption = offCaption;
            Value = value;
        }
    }
}
