using System;

namespace Common.Standard.Settings
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SettingPropsAttribute : Attribute
    {
        public string Key { get; }

        public SettingPropsAttribute(string key)
        {
            Key = key;
        }
    }
}
