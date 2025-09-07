#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace XDay.GUIAPI
{
    public static class AtlasTool
    {
        [MenuItem("XDay/Other/创建2048空贴图")]
        public static void Create2048()
        {
            CreateEmpty(2048);
        }

        [MenuItem("XDay/Other/创建1024空贴图")]
        public static void Create1024()
        {
            CreateEmpty(1024);
        }

        [MenuItem("XDay/Other/将Image替换成UIImage")]
        public static void ReplaceImage()
        {
#if false
            var guids = AssetDatabase.FindAssets("t:prefab");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                ReplaceImage(prefab);
            }
#else
            var selection = Selection.activeGameObject;
            ReplaceImage(selection);
#endif
        }

        [MenuItem("XDay/Other/将图集Sprite的MeshType设置为FullRect")]
        public static void SetSpriteFullRect()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                var guids = AssetDatabase.FindAssets("t:sprite");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.IndexOf("Assets/Game/Res/UI") >= 0)
                    {
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (sprite != null)
                        {
                            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                            var settings = new TextureImporterSettings();
                            importer.ReadTextureSettings(settings);
                            settings.spriteMeshType = SpriteMeshType.FullRect;
                            importer.SetTextureSettings(settings);
                            importer.SaveAndReimport();
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TextureSizeBatchProcessor] 批量处理出错: {e.Message}");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        public static void CreateEmpty(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            var pixels = new Color32[resolution * resolution];
            for (var i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = Color.clear;
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            var bytes = texture.EncodeToTGA();
            File.WriteAllBytes($"Assets/empty_{resolution}.tga", bytes);
            AssetDatabase.Refresh();
        }

        public static void ReplaceImage(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            var clone = UnityEngine.Object.Instantiate(gameObject);

            var rootTransform = gameObject.GetComponent<RectTransform>();
            var scale = rootTransform.localScale;
            var anchorMin = rootTransform.anchorMin;
            var anchorMax = rootTransform.anchorMax;
            var offsetMin = rootTransform.offsetMin;
            var offsetMax = rootTransform.offsetMax;
            var pivot = rootTransform.pivot;

            var images = clone.GetComponentsInChildren<Image>(true);
            if (images.Length > 0)
            {
                Debug.Log($"替换{gameObject.name}的{images.Length}个Image组件为UIImage");
            }

            foreach (var image in images)
            {
                var json = JsonUtility.ToJson(image);
                var obj = image.gameObject;
                UnityEngine.Object.DestroyImmediate(image);
                var uniImage = obj.AddComponent<UIImage>();
                JsonUtility.FromJsonOverwrite(json, uniImage);
            }

            var type = PrefabUtility.GetPrefabAssetType(gameObject);
            if (type == PrefabAssetType.Regular ||
                type == PrefabAssetType.Variant)
            {
                var path = AssetDatabase.GetAssetPath(gameObject);
                PrefabUtility.SaveAsPrefabAsset(clone, path);

                AssetDatabase.Refresh();

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var t = prefab.transform as RectTransform;
                t.anchorMin = anchorMin;
                t.anchorMax = anchorMax;
                t.offsetMin = offsetMin;
                t.offsetMax = offsetMax;
                t.pivot = pivot;
                t.localScale = scale;
                PrefabUtility.SavePrefabAsset(prefab);
            }

            Object.DestroyImmediate(clone);
        }
    }
}

#endif