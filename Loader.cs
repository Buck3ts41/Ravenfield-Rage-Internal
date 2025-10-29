using UnityEngine;

namespace TestUnityInternal
{
    public class Loader
    {
        public static void Init()
        {
            Loader.Load = new GameObject();
            Loader.Load.AddComponent<Main>();
            UnityEngine.Object.DontDestroyOnLoad(Loader.Load);
        }

        public static void Unload()
        {
            _Unload();
        }

        public static void _Unload()
        {
            GameObject.Destroy(Load);
        }

        private static GameObject Load;


    }
}
