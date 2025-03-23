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

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using XDay.WorldAPI.Editor;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileSystem
    {
        private void DrawMaterialConfigs()
        {
            var changed = false;
            EditorGUILayout.BeginHorizontal();
            {
                m_ShowMaterialConfig = EditorGUILayout.Foldout(m_ShowMaterialConfig, "Material Config");
                EditorGUILayout.Space();
                if (GUILayout.Button(new GUIContent("Add", "Add Parameters"), GUILayout.MaxWidth(30)))
                {
                    changed = AddMaterialConfig();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (m_ShowMaterialConfig)
            {
                QueryTileShaders();

                EditorHelper.IndentLayout(() =>
                {
                    changed |= DrawTileShaders();

                    QueryActiveShaderMaterialConfigs(changed);

                    for (var i = 0; i < m_TempList.Count; ++i)
                    {
                        if (DrawMaterialConfig(m_TempList[i], i))
                        {
                            break;
                        }
                    }
                });
            }
        }

        private bool IsUserDefinedVectorParameterName(string name, string[] texturePropertyNames)
        {
            foreach (var texturePropName in texturePropertyNames)
            {
                if (name.StartsWith(texturePropName))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AddMaterialConfig()
        {
            var config = CreateMaterialConfig(World.AllocateObjectID());
            if (config == null)
            {
                return false;
            }

            if (m_ActiveMaterialConfigID == 0)
            {
                m_ActiveMaterialConfigID = config.ID;
            }
            m_MaterialConfigs.Add(config);
            return true;
        }

        private bool DrawTileShaders()
        {
            if (m_ShaderNames == null ||
                m_ShaderNames.Length != m_TileShaders.Count)
            {
                m_ShaderNames = new string[m_TileShaders.Count];
            }

            if (m_ShaderNames.Length > 0)
            {
                m_ActiveShaderIndex = Mathf.Clamp(m_ActiveShaderIndex, 0, m_ShaderNames.Length - 1);
            }
            else
            {
                m_ActiveShaderIndex = -1;
            }

            EditorGUI.BeginChangeCheck();
            for (var i = 0; i < m_ShaderNames.Length; ++i)
            {
                m_ShaderNames[i] = m_TileShaders[i].name;
            }
            var shaderIndex = EditorGUILayout.Popup("Tile Shaders", m_ActiveShaderIndex, m_ShaderNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_ActiveShaderIndex = shaderIndex;
                return true;
            }
            return false;
        }

        private void ApplyMaterialConfigToTile(MaterialConfig config, TileObject tile, int x, int y)
        {
            var renderer = m_Renderer.GetTileMeshRenderer(x, y);
            if (renderer != null)
            {
                if (EditorHelper.GetObjectGUID(renderer.sharedMaterial.shader) == config.ShaderGUID)
                {
                    tile.MaterialConfigID = config.ID;
                    ApplyVectorParameters(config, renderer, tile);
                    ApplyTextureParameters(config, renderer, tile);
                    ApplyFloatParameters(config, renderer, tile);
                    EditorUtility.SetDirty(renderer.sharedMaterial);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Config shader is not same as tile shader!", "OK");
                }
            }
            else
            {
                Debug.LogError($"renderer tagged by {TileDefine.TILE_ROOT_TAG} not found!");
            }
        }

        private bool DrawMaterialConfig(MaterialConfig config, int index)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUIUtility.labelWidth = 5;
            if (EditorGUILayout.ToggleLeft("", config.ID == m_ActiveMaterialConfigID, GUILayout.MaxWidth(25)))
            {
                m_ActiveMaterialConfigID = config.ID;
            }
            EditorGUIUtility.labelWidth = 0;

            DrawConfigInfo(config);

            EditorGUILayout.Space();

            DrawApplyMaterial(config);

            DrawRefreshMaterialButton(config);

            var configDeleted = DrawDeleteMaterialConfigButton(index);

            EditorGUILayout.EndHorizontal();

            if (config.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    DrawMaterialFloatParameters(config);
                    DrawMaterialVectorParameters(config);
                    DrawMaterialTextureParameters(config);
                });
            }

            return configDeleted;
        }

        private void QueryTileShaders()
        {
            m_TileShaders.Clear();
            for (var y = 0; y < m_YTileCount; ++y)
            {
                for (var x = 0; x < m_XTileCount; ++x)
                {
                    var renderer = m_Renderer.GetTileMeshRenderer(x, y);
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        var shader = renderer.sharedMaterial.shader;
                        if (shader != null && 
                            !m_TileShaders.Contains(shader))
                        {
                            m_TileShaders.Add(shader);
                        }
                    }
                }
            }
        }

        private void QueryActiveShaderMaterialConfigs(bool changed)
        {
            var shaderGUID = EditorHelper.GetObjectGUID(GetActiveShader());

            m_TempList.Clear();
            foreach (var config in m_MaterialConfigs)
            {
                if (config.ShaderGUID == shaderGUID)
                {
                    m_TempList.Add(config);
                }
            }

            if (m_TempList.Count > 0 &&
                changed)
            {
                m_ActiveMaterialConfigID = m_TempList[0].ID;
            }
        }

        private bool DrawDeleteMaterialConfigButton(int index)
        {
            if (GUILayout.Button(new GUIContent("Delete", "")))
            {
                m_MaterialConfigs.RemoveAt(index);
                return true;
            }
            return false;
        }

        private void DrawRefreshMaterialButton(MaterialConfig config)
        {
            if (GUILayout.Button(new GUIContent("Refresh", "")))
            {
                var newConfig = CreateMaterialConfig(0);
                config.Combine(newConfig);
            }
        }

        private void DrawApplyMaterial(MaterialConfig config)
        {
            if (GUILayout.Button(new GUIContent("Apply To Tiles", "")))
            {
                UndoSystem.NextGroupAndJoin();

                for (var y = 0; y < m_YTileCount; ++y)
                {
                    for (var x = 0; x < m_XTileCount; ++x)
                    {
                        var tile = m_Tiles[y * m_XTileCount + x];
                        if (tile != null && tile.MaterialConfigID == config.ID)
                        {
                            ApplyMaterialConfigToTile(config, tile, x, y);
                        }
                    }
                }
            }
        }

        private void DrawMaterialTextureParameters(MaterialConfig config)
        {
            for (var i = 0; i < config.Textures.Count; ++i)
            {
                var tex = config.Textures[i];
                tex.TextureGUID = AssetDatabase.AssetPathToGUID(EditorHelper.ObjectField<Texture>($"{tex.Name}", AssetDatabase.GUIDToAssetPath(tex.TextureGUID)));
                tex.UVTransform = EditorGUILayout.Vector4Field("", tex.UVTransform.HasValue ? tex.UVTransform.GetValueOrDefault() : new Vector4(1, 1, 0, 0));
                if (i == config.Textures.Count - 1)
                {
                    EditorHelper.HorizontalLine();
                }
            }
        }

        private void ApplyTextureParameters(MaterialConfig config, MeshRenderer renderer, TileObject tile)
        {
            foreach (var param in config.Textures)
            {
                if (renderer.sharedMaterial != null)
                {
                    UndoSystem.SetAspect(tile, $"{TileDefine.SHADER_PROPERTY_ASPECT_NAME}@{TileDefine.SHADER_TEXTURE_PROPERTY_NAME}@{param.Name}", IAspect.FromString(param.TextureGUID), "Set Shader Texture Property", ID, UndoActionJoinMode.None);
                    if (param.UVTransform.HasValue)
                    {
                        UndoSystem.SetAspect(tile, $"{TileDefine.SHADER_PROPERTY_ASPECT_NAME}@{TileDefine.SHADER_VECTOR4_PROPERTY_NAME}@{param.Name}_ST", IAspect.FromVector4(param.UVTransform.GetValueOrDefault()), "Set Shader Vector4 Property", ID, UndoActionJoinMode.None);
                    }
                }
            }
        }

        private void ApplyVectorParameters(MaterialConfig config, MeshRenderer renderer, TileObject tile)
        {
            foreach (var param in config.Vector4s)
            {
                if (renderer.sharedMaterial != null)
                {
                    if (param.Value.HasValue)
                    {
                        UndoSystem.SetAspect(tile, $"{TileDefine.SHADER_PROPERTY_ASPECT_NAME}@{TileDefine.SHADER_VECTOR4_PROPERTY_NAME}@{param.Name}", IAspect.FromVector4(param.Value.GetValueOrDefault()), "Set Shader Vector4 Property", ID, UndoActionJoinMode.None);
                    }
                }
            }
        }

        private void DrawMaterialVectorParameters(MaterialConfig config)
        {
            for (var i = 0; i < config.Vector4s.Count; ++i)
            {
                var parameter = config.Vector4s[i];
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.Vector4Field(parameter.Name, parameter.Value.GetValueOrDefault());
                if (EditorGUI.EndChangeCheck())
                {
                    parameter.Value = newValue;
                }
                if (i == config.Vector4s.Count - 1)
                {
                    EditorHelper.HorizontalLine();
                }
            }
        }

        private void ApplyFloatParameters(MaterialConfig config, MeshRenderer renderer, TileObject tile)
        {
            foreach (var parameter in config.Floats)
            {
                if (renderer.sharedMaterial != null)
                {
                    if (parameter.Value.HasValue)
                    {
                        UndoSystem.SetAspect(tile, $"{TileDefine.SHADER_PROPERTY_ASPECT_NAME}@{TileDefine.SHADER_SINGLE_PROPERTY_NAME}@{parameter.Name}", IAspect.FromSingle(parameter.Value.GetValueOrDefault()), "Set Shader Vector4 Property", ID, UndoActionJoinMode.None);
                    }
                }
            }
        }

        private void DrawMaterialFloatParameters(MaterialConfig config)
        {
            for (var i = 0; i < config.Floats.Count; ++i)
            {
                var param = config.Floats[i];
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.FloatField(param.Name, param.Value.GetValueOrDefault());
                if (EditorGUI.EndChangeCheck())
                {
                    param.Value = newValue;
                }
                if (i == config.Floats.Count - 1)
                {
                    EditorHelper.HorizontalLine();
                }
            }
        }

        private Shader GetActiveShader()
        {
            if (m_ActiveShaderIndex >= 0 && m_ActiveShaderIndex < m_TileShaders.Count)
            {
                return m_TileShaders[m_ActiveShaderIndex];
            }

            return null;
        }

        private void EditMaterial()
        {
            var evt = Event.current;
            var pos = Helper.GUIRayCastWithXZPlane(evt.mousePosition, World.CameraManipulator.Camera);

            if (evt.alt == false &&
                evt.button == 0 &&
                evt.type == EventType.MouseDown)
            {
                if (MaterialConfig != null)
                {
                    GetTileCoordinatesInRange(pos, out var minX, out var minY, out var maxX, out var maxY);

                    UndoSystem.NextGroupAndJoin();

                    for (var y = minY; y <= maxY; ++y)
                    {
                        for (var x = minX; x <= maxX; ++x)
                        {
                            if (x >= 0 && x < m_XTileCount && y >= 0 && y < m_YTileCount)
                            {
                                var tile = GetTile(x, y);
                                if (tile != null)
                                {
                                    ApplyMaterialConfigToTile(MaterialConfig, tile, x, y);
                                }
                            }
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Select one material!", "OK");
                    evt.Use();
                }
            }

            HandleUtility.AddDefaultControl(0);
        }

        private MaterialConfig CreateMaterialConfig(int id)
        {
            var activeShader = GetActiveShader();
            if (activeShader == null)
            {
                EditorUtility.DisplayDialog("Error", "Active shader not set", "OK");
                return null;
            }
            
            var tempMaterial = new Material(activeShader);

            var config = new MaterialConfig(id, m_MaterialConfigs.Count, EditorHelper.GetObjectGUID(activeShader), "Material Config");
            config.Init(World);

            foreach (var name in tempMaterial.GetPropertyNames(MaterialPropertyType.Float))
            {
                config.AddFloat(name);
            }

            var texturePropertyNames = tempMaterial.GetTexturePropertyNames();
            foreach (var name in tempMaterial.GetPropertyNames(MaterialPropertyType.Vector))
            {
                var isValidName = IsUserDefinedVectorParameterName(name, texturePropertyNames);
                if (isValidName)
                {
                    config.AddVector(name);
                }
            }

            foreach (var name in texturePropertyNames)
            {
                if (name != TexturePainter.MaskName)
                {
                    config.AddTexture(name);
                }
            }

            Object.DestroyImmediate(tempMaterial);

            return config;
        }

        private void DrawConfigInfo(MaterialConfig config)
        {
            m_Style ??= new GUIStyle(EditorStyles.foldout);
            m_Style.fixedWidth = 1;

            EditorGUIUtility.fieldWidth = 18;
            config.ShowInInspector = EditorGUILayout.Foldout(config.ShowInInspector, "", m_Style);
            EditorGUIUtility.fieldWidth = 0;

            EditorGUIUtility.labelWidth = 1;
            config.Name = EditorGUILayout.TextField("", config.Name);
            EditorGUIUtility.labelWidth = 0;
        }

        private GUIStyle m_Style;
        private int m_ActiveShaderIndex = -1;
        private string[] m_ShaderNames;
        private List<Shader> m_TileShaders = new();
        private List<MaterialConfig> m_TempList = new();
    }
}


//XDay