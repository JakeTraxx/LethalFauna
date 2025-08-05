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
    // When Instantiating versions of this class it will auto load configuration values into fields using the ConfigAttribute. The description is set by the ConfigAttribute.
    internal abstract class ConfigHandler<T> where T : ConfigHandler<T>
    {
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
                    else if (type == typeof(string))
                    {
                        field.SetValue(this, config.Bind(
                                "General",
                                field.Name,
                                (string)field.GetValue(this),
                                ((ConfigAttribute)field.GetCustomAttribute(typeof(ConfigAttribute), false)).Description
                            ).Value
                        );
                    }
                    else if (type == typeof(int))
                    {
                        field.SetValue(this, config.Bind(
                                "General",
                                field.Name,
                                (int)field.GetValue(this),
                                ((ConfigAttribute)field.GetCustomAttribute(typeof(ConfigAttribute), false)).Description
                            ).Value
                        );
                    }
                    else if (type == typeof(float))
                    {
                        field.SetValue(this, config.Bind(
                                "General",
                                field.Name,
                                (float)field.GetValue(this),
                                ((ConfigAttribute)field.GetCustomAttribute(typeof(ConfigAttribute), false)).Description
                            ).Value
                        );
                    }
                }
            }
        }
    }
}
