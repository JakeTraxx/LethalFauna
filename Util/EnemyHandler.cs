using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalFauna.Util
{
    internal abstract class EnemyHandler<T> where T : EnemyHandler<T>
    {
        public static T Instance { get; private set; }

        public EnemyHandler()
        {
            Instance = (T)this;
        }
    }
}
