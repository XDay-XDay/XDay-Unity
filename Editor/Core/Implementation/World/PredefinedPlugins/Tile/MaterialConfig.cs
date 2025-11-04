/*
 * Copyright (c) 2024-2025 XDay
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using XDay.UtilityAPI;
using UnityEditor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal class MaterialConfig : WorldObject
    {
        public string Name { set => m_Name = value; get => m_Name; }
        public string ShaderGUID => m_ShaderGUID;
        public bool ShowInInspector { set => m_Show = value; get => m_Show; }
        public List<FloatParam> Floats => m_Floats;
        public List<TextureParam> Textures => m_Textures;
        public List<Vector4Param> Vector4s => m_Vector4s;
        public List<ColorParam> Colors => m_Colors;
        public override string TypeName => "MaterialConfig";

        public MaterialConfig()
        {
        }

        public MaterialConfig(int id, int objectIndex, string shaderGUID, string name)
            : base(id, objectIndex)
        {
            m_ShaderGUID = shaderGUID;
            m_Name = name;
        }

        public MaterialConfig Clone()
        {
            var clone = new MaterialConfig(World.AllocateObjectID(), 0, m_ShaderGUID, m_Name + "_Clone");
            clone.Combine(this);
            clone.m_Show = m_Show;
            return clone;
        }

        public void Combine(MaterialConfig other)
        {
            CombineFloats(other);
            CombineTextures(other);
            CombineVectors(other);
            CombineColors(other);
        }

        public void ReplaceName(string oldName, string newName)
        {
            m_Name = m_Name.Replace(oldName, newName);
        }

        public void ReplaceTexture(Texture oldTexture, Texture newTexture, Vector4 uvTransform)
        {
            foreach (var tex in m_Textures)
            {
                var oldGUID = EditorHelper.GetObjectGUID(oldTexture);
                var newGUID = EditorHelper.GetObjectGUID(newTexture);
                if (oldGUID == tex.TextureGUID)
                {
                    tex.TextureGUID = newGUID;
                    tex.UVTransform = uvTransform;
                }
            }
        }

        public void AddTexture(string name, Texture texture, Vector4 uvTransform)
        {
            m_Textures.Add(new TextureParam
            {
                Name = name,
                TextureGUID = EditorHelper.GetObjectGUID(texture),
                UVTransform = uvTransform,
            });
        }

        public void AddVector(string name, Vector4 value)
        {
            m_Vector4s.Add(new Vector4Param
            {
                Name = name,
                Value = value,
            });
        }

        public void AddColor(string name, Color value)
        {
            m_Colors.Add(new ColorParam
            {
                Name = name,
                Value = value,
            });
        }

        public void AddFloat(string name, float value)
        {
            m_Floats.Add(new FloatParam
            {
                Name = name,
                Value = value,
            });
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            var version = deserializer.ReadInt32("MaterialConfig.Version");

            base.EditorDeserialize(deserializer, label);

            m_ShaderGUID = deserializer.ReadString("Shader");
            m_Show = deserializer.ReadBoolean("Show");
            m_Name = deserializer.ReadString("Name");
            m_Floats = deserializer.ReadList($"Floats", (index) => {
                var param = new FloatParam();
                deserializer.ReadStructure($"Float {index}", () =>
                {
                    var hasValue = deserializer.ReadBoolean("Has Value");
                    var value = deserializer.ReadSingle("Value");
                    if (hasValue)
                    {
                        param.Value = value;
                    }
                    param.Name = deserializer.ReadString("Name");
                });
                return param;
            });
            m_Vector4s = deserializer.ReadList($"Vector4 Parameters", (index) => {
                var param = new Vector4Param();
                deserializer.ReadStructure($"Vector4 {index}", () =>
                {
                    var hasValue = deserializer.ReadBoolean("Has Value");
                    var value = deserializer.ReadVector4("Value");
                    if (hasValue)
                    {
                        param.Value = value;
                    }
                    param.Name = deserializer.ReadString("Name");
                });
                return param;
            });
            if (version >= 2)
            {
                m_Colors = deserializer.ReadList($"Color Parameters", (index) => {
                    var param = new ColorParam();
                    deserializer.ReadStructure($"Color {index}", () =>
                    {
                        var hasValue = deserializer.ReadBoolean("Has Value");
                        var value = deserializer.ReadColor("Value");
                        if (hasValue)
                        {
                            param.Value = value;
                        }
                        param.Name = deserializer.ReadString("Name");
                    });
                    return param;
                });
            }
            m_Textures = deserializer.ReadList($"Textures", (index) => {
                var param = new TextureParam();
                deserializer.ReadStructure($"Texture {index}", () =>
                {
                    var hasValue = deserializer.ReadBoolean("Has UV Transform");
                    var uvTransform = deserializer.ReadVector4("UV Transform");
                    if (hasValue)
                    {
                        param.UVTransform = uvTransform;
                    }
                    param.TextureGUID = deserializer.ReadString("Texture");
                    param.Name = deserializer.ReadString("Name");
                });
                return param;
            });
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "MaterialConfig.Version");

            base.EditorSerialize(writer, label, converter);

            writer.WriteString(m_ShaderGUID, "Shader GUID");
            writer.WriteBoolean(m_Show, "Show");
            writer.WriteString(m_Name, "Name");
            writer.WriteList(m_Floats, $"Floats", (param, index) => {
                writer.WriteStructure($"Float {index}", () =>
                {
                    writer.WriteBoolean(param.Value.HasValue, "Has Value");
                    writer.WriteSingle(param.Value.GetValueOrDefault(), "Value");
                    writer.WriteString(param.Name, "Name");
                });
            });
            writer.WriteList(m_Vector4s, $"Vector4s", (param, index) => {
                writer.WriteStructure($"Vector4 {index}", () =>
                {
                    writer.WriteBoolean(param.Value.HasValue, "Has Value");
                    writer.WriteVector4(param.Value.GetValueOrDefault(), "Value");
                    writer.WriteString(param.Name, "Name");
                });
            });
            writer.WriteList(m_Colors, $"Colors", (param, index) => {
                writer.WriteStructure($"Color {index}", () =>
                {
                    writer.WriteBoolean(param.Value.HasValue, "Has Value");
                    writer.WriteColor(param.Value.GetValueOrDefault(), "Value");
                    writer.WriteString(param.Name, "Name");
                });
            });
            writer.WriteList(m_Textures, $"Textures", (param, index) => {
                writer.WriteStructure($"Texture {index}", () =>
                {
                    writer.WriteBoolean(param.UVTransform.HasValue, "Has UV Transform");
                    writer.WriteVector4(param.UVTransform.GetValueOrDefault(), "UV Transform");
                    writer.WriteString(param.TextureGUID, "Texture");
                    writer.WriteString(param.Name, "Name");
                });
            });
        }   

        private void CombineFloats(MaterialConfig other)
        {
            var deletedFloats = m_Floats.Where(a => !other.Floats.Any(b => b.Name == a.Name)).ToArray();
            var addedFloats = other.Floats.Where(b => !m_Floats.Any(a => a.Name == b.Name)).ToArray();
            foreach (var setting in deletedFloats)
            {
                for (var i = m_Floats.Count - 1; i >= 0; --i)
                {
                    if (m_Floats[i].Name == setting.Name)
                    {
                        m_Floats.RemoveAt(i);
                    }
                }
            }
            foreach (var setting in addedFloats)
            {
                AddFloat(setting.Name, setting.Value.Value);
            }
        }

        private void CombineVectors(MaterialConfig other)
        {
            var deletedVectors = m_Vector4s.Where(a => !other.Vector4s.Any(b => b.Name == a.Name)).ToArray();
            var addedVectors = other.Vector4s.Where(b => !m_Vector4s.Any(a => a.Name == b.Name)).ToArray();
            foreach (var setting in deletedVectors)
            {
                for (var i = m_Vector4s.Count - 1; i >= 0; --i)
                {
                    if (m_Vector4s[i].Name == setting.Name)
                    {
                        m_Vector4s.RemoveAt(i);
                    }
                }
            }
            foreach (var setting in addedVectors)
            {
                AddVector(setting.Name, setting.Value.Value);
            }
        }

        private void CombineColors(MaterialConfig other)
        {
            var deletedVectors = m_Colors.Where(a => !other.Colors.Any(b => b.Name == a.Name)).ToArray();
            var addedVectors = other.Colors.Where(b => !m_Colors.Any(a => a.Name == b.Name)).ToArray();
            foreach (var setting in deletedVectors)
            {
                for (var i = m_Colors.Count - 1; i >= 0; --i)
                {
                    if (m_Colors[i].Name == setting.Name)
                    {
                        m_Colors.RemoveAt(i);
                    }
                }
            }
            foreach (var setting in addedVectors)
            {
                AddColor(setting.Name, setting.Value.Value);
            }
        }

        private void CombineTextures(MaterialConfig other)
        {
            var deletedTextures = m_Textures.Where(a => !other.Textures.Any(b => b.Name == a.Name)).ToArray();
            var addedTextures = other.Textures.Where(b => !m_Textures.Any(a => a.Name == b.Name)).ToArray();
            foreach (var setting in deletedTextures)
            {
                for (var i = m_Textures.Count - 1; i >= 0; --i)
                {
                    if (m_Textures[i].Name == setting.Name)
                    {
                        m_Textures.RemoveAt(i);
                    }
                }
            }
            foreach (var setting in addedTextures)
            {
                Texture texture = null;
                Vector4 uvTransform = new Vector4(1, 1, 0, 0);
                if (!string.IsNullOrEmpty(setting.TextureGUID))
                {
                    texture = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(setting.TextureGUID));
                }
                
                AddTexture(setting.Name, texture, setting.UVTransform.Value);
            }
        }

        protected override void OnInit()
        {
        }

        protected override void OnUninit()
        {
        }

        public class ParamBase
        {
            public string Name { get; set; }
        }

        public class Vector4Param : ParamBase
        {
            public Vector4? Value { get; set; }
        }

        public class ColorParam : ParamBase
        {
            public Color? Value { get; set; }
        }

        public class FloatParam : ParamBase
        {
            public float? Value { get; set; }
        }

        public class TextureParam : ParamBase
        {
            public Vector4? UVTransform { get; set; }
            public string TextureGUID { get; set; }
        }

        private bool m_Show = true;
        private List<FloatParam> m_Floats = new();
        private List<Vector4Param> m_Vector4s = new();
        private List<ColorParam> m_Colors = new();
        private List<TextureParam> m_Textures = new();
        private string m_Name;
        private string m_ShaderGUID;
        private const int m_Version = 2;
    }
}


//XDay