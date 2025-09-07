using Cysharp.Threading.Tasks;
using System.IO;
using UnityEngine;

namespace XDay.AssetAPI
{
    public static class Asset
    {
        static Asset()
        {
            m_AssetLoader = new();
        }

        public static bool Exists(string path)
        {
            return m_AssetLoader.Exists(path);
        }

        public static T Load<T>(string path) where T : Object
        {
            return m_AssetLoader.Load<T>(path);
        }

        public static byte[] LoadBytes(string path)
        {
            var asset = m_AssetLoader.Load<TextAsset>(path);
            if (asset != null)
            {
                return asset.bytes;
            }
            Debug.Assert(false, $"load bytes {path} failed");
            return null;
        }

        public static GameObject LoadGameObject(string path)
        {
            var prefab = m_AssetLoader.Load<GameObject>(path);
            if (prefab != null)
            {
                var obj = Object.Instantiate(prefab);
                obj.name = prefab.name;
                return obj;
            }
            return null;
        }

        public static string LoadText(string path)
        {
            var asset = m_AssetLoader.Load<TextAsset>(path);
            if (asset != null)
            {
                return asset.text;
            }
            Debug.Assert(false, $"load text {path} failed");
            return null;
        }

        public static Stream LoadTextStream(string path)
        {
            return new MemoryStream(LoadBytes(path));
        }

        public static async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            return await m_AssetLoader.LoadAsync<T>(path);
        }

        public static async UniTask<GameObject> LoadGameObjectAsync(string path)
        {
            return await m_AssetLoader.LoadGameObjectAsync(path);
        }

        public static void LoadGameObjectAsync(string path, System.Action<GameObject> onLoaded)
        {
            m_AssetLoader.LoadGameObjectAsync(path, onLoaded);
        }

        public static bool UnloadAsset(string path)
        {
            return m_AssetLoader.UnloadAsset(path);
        }

        private static DefaultAssetLoader m_AssetLoader;
    }
}
