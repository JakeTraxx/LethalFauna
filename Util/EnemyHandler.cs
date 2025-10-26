namespace LethalFauna.Util
{
    // The constructor will be called during mod loading to allow asset and config loading.
    internal abstract class EnemyHandler<T> where T : EnemyHandler<T>
    {
        public static T Instance { get; private set; }

        public EnemyHandler()
        {
            Instance = (T)this;
        }
    }
}
