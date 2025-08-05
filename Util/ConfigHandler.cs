using BepInEx.Configuration;
using LethalFauna.Util.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LethalFauna.Util
{
    internal abstract class ConfigHandler<T> where T : ConfigHandler<T>
    {
        // Other config vars will go here in other classes

        public ConfigHandler(ConfigFile config)
        {
            foreach (FieldInfo field in typeof(T).GetFields())
            {
                if (field.IsDefined(typeof(ConfigAttribute), false)) {

                    Type type = field.FieldType;

                    if (type == typeof(bool))
                    {
                        field.SetValue(this, config.Bind(
                                "General",
                                field.Name,
                                (bool)field.GetValue(this),
                                ((ConfigAttribute)field.GetCustomAttribute(typeof(ConfigAttribute), false)).Description
                            ).Value
                        );
                    }
                    //else if () Todo: Add more types.
                }
            }
        }
    }
}
