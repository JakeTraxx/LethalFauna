using System;

namespace LethalFauna.Util.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class ConfigAttribute : Attribute
    {
        public string Description;

        public ConfigAttribute(string description)
        {
            Description = description;
        }
    }
}
