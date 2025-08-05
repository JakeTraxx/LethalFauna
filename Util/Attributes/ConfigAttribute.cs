using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
