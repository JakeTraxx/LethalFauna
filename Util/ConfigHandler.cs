using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalFauna.Util
{
    internal abstract class ConfigHandler<T> where T : ConfigHandler<T>
    {
        // Other config vars will go here in other classes

        public ConfigHandler(ConfigFile config)
        {

        }
    }
}
