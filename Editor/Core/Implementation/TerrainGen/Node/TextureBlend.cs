

using UnityEngine;
using UnityEditor;
using XDay.SerializationAPI;
using XDay.UtilityAPI;

namespace XDay.Terrain.Editor
{
    class TextureBlend : OutputBase
    {
        public class Layer
        {
            public Texture2D texture = null;
            public Vector4 uvTransform = new Vector4(1, 1, 0, 0);
        };

        public class Setting : ITerrainModifierSetting
        {
            public Layer[] layers;
            public Material material;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteString(EditorHelper.GetObjectGUID(material), "Material File ID");
                writer.WriteArray(layers, "Layers", (layer, index) =>
                {
                    writer.WriteStructure($"Layer {index}", () =>
                {
                    writer.WriteVector4(layers[index].uvTransform, "UV Transform");
                    writer.WriteString(EditorHelper.GetObjectGUID(layers[index].texture), "Texture File ID");
                });
                });
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                string materialGUID = reader.ReadString("Material File ID");
                material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialGUID));
                layers = reader.ReadArray("Layers", (index) =>
            {
                var layer = new Layer();
                reader.ReadStructure($"Layer {index}", () =>
                        {
                            layer.uvTransform = reader.ReadVector4("UV Transform");
                            string textureGUID = reader.ReadString("Texture File ID");
                            layer.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(textureGUID));
                        });
                return layer;
            });
            }

            private const int m_Version = 1;
        };

        public TextureBlend(int id) : base(id)
        {
        }

        public TextureBlend()
        {
        }

        public override void Execute()
        {
            base.Execute();

            var setting = GetSetting() as Setting;

            var mtl = GetMaterial();
            if (mtl != null)
            {
                mtl.SetTexture("_Layer0", setting.layers[0].texture);
                mtl.SetTexture("_Layer1", setting.layers[1].texture);
                mtl.SetTexture("_Layer2", setting.layers[2].texture);
                mtl.SetTexture("_Layer3", setting.layers[3].texture);
                mtl.SetVector("_Layer0Tiling", setting.layers[0].uvTransform);
                mtl.SetVector("_Layer1Tiling", setting.layers[1].uvTransform);
                mtl.SetVector("_Layer2Tiling", setting.layers[2].uvTransform);
                mtl.SetVector("_Layer3Tiling", setting.layers[3].uvTransform);
            }
            else
            {
                Debug.LogError("TextureBlend: no material!");
            }
        }

        public override string GetName()
        {
            return "TextureBlend";
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());

            setting.material = EditorGUILayout.ObjectField("Material", GetMaterial(), typeof(Material), false) as Material;

            DrawLayers();
        }

        public override bool HasMaskData() { return true; }
        public override Material GetMaterial()
        {
            var setting = GetSetting() as Setting;
            return setting.material;
        }

        public override bool IsVisible() { return true; }
        public override bool CanDelete() { return true; }
        private void DrawLayers()
        {
            var setting = GetSetting() as Setting;

            for (var i = 0; i < setting.layers.Length; ++i)
            {
                DrawLayer(i, setting.layers[i]);
            }
        }

        private void DrawLayer(int index, Layer layer)
        {
            layer.texture = EditorGUILayout.ObjectField($"Layer {index}", layer.texture, typeof(Texture2D), false) as Texture2D;
            layer.uvTransform = EditorGUILayout.Vector4Field("UV Transform", layer.uvTransform);
        }
    };
}