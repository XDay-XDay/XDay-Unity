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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileSystem
    {
        private void DrawMaterialConfigs()
        {
            var changed = false;
            EditorGUILayout.BeginHorizontal();
            {
                m_ShowMaterialConfig = EditorGUILayout.Foldout(m_ShowMaterialConfig, "材质参数");
                EditorGUILayout.Space();
                if (GUILayout.Button("替换名称", GUILayout.MaxWidth(80)))
                {
                    ReplaceName();
                }
                if (GUILayout.Button("应用所有", GUILayout.MaxWidth(80)))
                {
                    ApplyAllMaterialConfigs();
                }
                if (GUILayout.Button(new GUIContent("新增", "新增材质参数"), GUILayout.MaxWidth(40)))
                {
                    changed = AddMaterialConfig();
                }
            }
            EditorGUILayout.EndHorizontal();

            DrawSearch();

            if (m_ShowMaterialConfig)
            {
                QueryTileShaders();

                EditorHelper.IndentLayout(() =>
                {
                    changed |= DrawTileShaders();

                    QueryActiveShaderMaterialConfigs(changed);

                    for (var i = 0; i < m_TempList.Count; ++i)
                    {
                        if (DrawMaterialConfig(m_TempList, i))
                        {
                            break;
                        }
                    }
                });
                m_MaterialConfigs.Clear();
                m_MaterialConfigs.AddRange(m_TempList);
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
                SetActiveMaterialConfig(config.ID);
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
            var idx = 0;
            foreach (var kv in m_TileShaders)
            {
                m_ShaderNames[idx] = kv.Key.name;
                ++idx;
            }
            var shaderIndex = EditorGUILayout.Popup("地块Shader", m_ActiveShaderIndex, m_ShaderNames);
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
                    UndoSystem.SetAspect(tile, TileDefine.TILE_MATERIAL_ID_NAME, IAspect.FromInt32(config.ID), "Set Tile Material ID", ID, UndoActionJoinMode.None);
                    ApplyVectorParameters(config, renderer, tile);
                    ApplyColorParameters(config, renderer, tile);
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

        private bool DrawMaterialConfig(List<MaterialConfig> configs, int index)
        {
            var config = configs[index];

            if(!string.IsNullOrEmpty(m_SearchText) && config.Name.IndexOf(m_SearchText) < 0)
            {
                return false;
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUIUtility.labelWidth = 5;
            if (EditorGUILayout.ToggleLeft("", config.ID == m_ActiveMaterialConfigID, GUILayout.MaxWidth(25)))
            {
                SetActiveMaterialConfig(config.ID);
            }
            EditorGUIUtility.labelWidth = 0;

            DrawConfigInfo(config);

            EditorGUILayout.Space();

            DrawChangeOrder(configs, index);

            DrawApplyMaterial(config);

            DrawRefreshMaterialButton(config);

            DrawCopyMaterial(configs, config, index);

            var configDeleted = DrawDeleteMaterialConfigButton(configs, index);

            EditorGUILayout.EndHorizontal();

            if (config.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    DrawMaterialFloatParameters(config);
                    DrawMaterialVectorParameters(config);
                    DrawMaterialColorParameters(config);
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
                            !ContainsShader(shader))
                        {
                            m_TileShaders.Add(new(shader, renderer.sharedMaterial));
                        }
                    }
                }
            }
        }

        private void QueryActiveShaderMaterialConfigs(bool changed)
        {
            var shaderGUID = EditorHelper.GetObjectGUID(GetActiveShader(out _));

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
                SetActiveMaterialConfig(m_TempList[0].ID);
            }
        }

        private bool DrawDeleteMaterialConfigButton(List<MaterialConfig> configs, int index)
        {
            if (GUILayout.Button(new GUIContent("删除", ""), GUILayout.MaxWidth(40)))
            {
                if (EditorUtility.DisplayDialog("警告", "确定删除?删除后不可恢复", "确定", "取消"))
                {
                    configs[index].Uninit();
                    configs.RemoveAt(index);
                    SceneView.RepaintAll();
                    return true;
                }
            }
            return false;
        }

        private void DrawRefreshMaterialButton(MaterialConfig config)
        {
            if (GUILayout.Button(new GUIContent("刷新", ""), GUILayout.MaxWidth(40)))
            {
                var newConfig = CreateMaterialConfig(0);
                config.Combine(newConfig);
            }
        }

        private void DrawChangeOrder(List<MaterialConfig> configs, int index)
        {
            if (index != configs.Count - 1)
            {
                if (GUILayout.Button(new GUIContent("->", "向下移动"), GUILayout.MaxWidth(20)))
                {
                    (configs[index], configs[index+1]) = (configs[index + 1], configs[index]);
                }
            }

            if (index != 0)
            {
                if (GUILayout.Button(new GUIContent("<-", "向上移动"), GUILayout.MaxWidth(20)))
                {
                    (configs[index], configs[index - 1]) = (configs[index - 1], configs[index]);
                }
            }
        }

        private void DrawApplyMaterial(MaterialConfig config)
        {
            if (GUILayout.Button(new GUIContent("应用", "应用到地块"), GUILayout.MaxWidth(40)))
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

        private void ApplyAllMaterialConfigs()
        {
            UndoSystem.NextGroupAndJoin();

            foreach (var config in m_MaterialConfigs)
            {
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

        private void DrawCopyMaterial(List<MaterialConfig> configs, MaterialConfig config, int index)
        {
            if (GUILayout.Button(new GUIContent("复制", "复制材质设置"), GUILayout.MaxWidth(40)))
            {
                var newConfig = config.Clone();
                newConfig.Init(World);
                configs.Insert(index + 1, newConfig);
                SetActiveMaterialConfig(newConfig.ID);
            }
        }

        private void DrawMaterialTextureParameters(MaterialConfig config)
        {
            for (var i = 0; i < config.Textures.Count; ++i)
            {
                var tex = config.Textures[i];
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("替换", "替换所有材质中相同的texture"), GUILayout.MaxWidth(40)))
                {
                    var oldTexture = EditorHelper.LoadAssetByGUID<Texture>(tex.TextureGUID);
                    ShowReplaceTextureWindow(tex.Name, oldTexture, tex.UVTransform.Value);
                }
                tex.TextureGUID = AssetDatabase.AssetPathToGUID(EditorHelper.ObjectField<Texture>($"{tex.Name}", AssetDatabase.GUIDToAssetPath(tex.TextureGUID)));
                EditorGUILayout.EndHorizontal();
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
		
	    private void ApplyColorParameters(MaterialConfig config, MeshRenderer renderer, TileObject tile)
        {
            foreach (var param in config.Colors)
            {
                if (renderer.sharedMaterial != null)
                {
                    if (param.Value.HasValue)
                    {
                        UndoSystem.SetAspect(tile, $"{TileDefine.SHADER_PROPERTY_ASPECT_NAME}@{TileDefine.SHADER_COLOR_PROPERTY_NAME}@{param.Name}", IAspect.FromColor(param.Value.GetValueOrDefault()), "Set Shader Color Property", ID, UndoActionJoinMode.None);
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
		
        private void DrawMaterialColorParameters(MaterialConfig config)
        {
            for (var i = 0; i < config.Colors.Count; ++i)
            {
                var parameter = config.Colors[i];
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.ColorField(parameter.Name, parameter.Value.GetValueOrDefault());
                if (EditorGUI.EndChangeCheck())
                {
                    parameter.Value = newValue;
                }
                if (i == config.Colors.Count - 1)
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

        private Shader GetActiveShader(out Material material)
        {
            material = null;
            if (m_ActiveShaderIndex >= 0 && m_ActiveShaderIndex < m_TileShaders.Count)
            {
                material = m_TileShaders[m_ActiveShaderIndex].Value;
                return m_TileShaders[m_ActiveShaderIndex].Key;
            }

            return null;
        }

        private void EditMaterial()
        {
            var evt = Event.current;
            var pos = Helper.GUIRayCastWithXZPlane(evt.mousePosition, World.CameraManipulator.Camera);

            if (evt.alt == false &&
                evt.button == 0 &&
                (evt.type == EventType.MouseDown || 
                evt.type == EventType.MouseDrag))
            {
                if (MaterialConfig != null)
                {
                    GetTileCoordinatesInRange(pos, out var minX, out var minY, out var maxX, out var maxY);
                    var coord = UnrotatedPositionToCoordinate(pos.x, pos.z);
                    if (m_LastPaintMaterialCoord != coord)
                    {
                        m_LastPaintMaterialCoord = coord;

                        if (evt.type == EventType.MouseDown)
                        {
                            UndoSystem.NextGroupAndJoin();
                        }

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
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Select one material!", "OK");
                    evt.Use();
                }
            }

            if (evt.button == 0 && evt.type == EventType.MouseUp)
            {
                m_LastPaintMaterialCoord = new Vector2Int(-1, -1);
            }

            HandleUtility.AddDefaultControl(0);
        }

        private void DrawTileMaterialIDs()
        {
            if (m_TextStyle == null)
            {
                m_TextStyle = new GUIStyle(GUI.skin.label);
                m_TextStyle.normal.textColor = Color.black;
            }
            for (var i = 0; i < m_YTileCount; ++i)
            {
                for (var j = 0; j < m_XTileCount; ++j)
                {
                    var tile = GetTile(j, i);
                    if (tile != null)
                    {
                        var position = CoordinateToRotatedCenterPosition(j, i);
                        var config = GetMaterialConfig(tile.MaterialConfigID);
                        if (config != null)
                        {
                            Handles.Label(position, config.Name, m_TextStyle);
                        }
                    }
                }
            }
        }

        private void DrawTileCoords()
        {
            if (m_TextStyle == null)
            {
                m_TextStyle = new GUIStyle(GUI.skin.label);
                m_TextStyle.normal.textColor = Color.black;
            }
            for (var i = 0; i < m_YTileCount; ++i)
            {
                for (var j = 0; j < m_XTileCount; ++j)
                {
                    var tile = GetTile(j, i);
                    if (tile != null)
                    {
                        var position = CoordinateToRotatedCenterPosition(j, i);
                        Handles.Label(position, $"{j}-{i}", m_TextStyle);
                    }
                }
            }
        }

        private MaterialConfig GetMaterialConfig(int id)
        {
            foreach (var config in m_MaterialConfigs)
            {
                if (id == config.ID)
                {
                    return config;
                }
            }
            return null;
        }

        private MaterialConfig CreateMaterialConfig(int id)
        {
            var activeShader = GetActiveShader(out var material);
            if (activeShader == null)
            {
                EditorUtility.DisplayDialog("出错了", "未设置Shader", "确定");
                return null;
            }
            
            var tempMaterial = new Material(activeShader);

            var config = new MaterialConfig(id, m_MaterialConfigs.Count, EditorHelper.GetObjectGUID(activeShader), "材质参数");
            config.Init(World);

            foreach (var name in tempMaterial.GetPropertyNames(MaterialPropertyType.Float))
            {
                config.AddFloat(name, material.GetFloat(name));
            }

            var texturePropertyNames = tempMaterial.GetTexturePropertyNames();
            foreach (var name in tempMaterial.GetPropertyNames(MaterialPropertyType.Vector))
            {
                var isValidName = IsUserDefinedVectorParameterName(name, texturePropertyNames);
                if (isValidName)
                {
                    if (IsColorProperty(tempMaterial, name))
                    {
                        config.AddColor(name, material.GetVector(name));
                    }
                    else
                    {
                        config.AddVector(name, material.GetVector(name));
                    }
                }
            }

            foreach (var name in texturePropertyNames)
            {
                if (name != TexturePainter.MaskName)
                {
                    var offset = material.GetTextureOffset(name);
                    var scale = material.GetTextureScale(name);
                    config.AddTexture(name, material.GetTexture(name), new Vector4(scale.x, scale.y, offset.x, offset.y));
                }
            }

            Object.DestroyImmediate(tempMaterial);

            return config;
        }

        private bool IsColorProperty(Material material, string propertyName)
        {
            if (material == null || string.IsNullOrEmpty(propertyName))
                return false;

            int propertyIndex = material.shader.FindPropertyIndex(propertyName);
            if (propertyIndex == -1)
                return false;

            Shader shader = material.shader;
            ShaderUtil.ShaderPropertyType type = ShaderUtil.GetPropertyType(shader, propertyIndex);

            if (type == ShaderUtil.ShaderPropertyType.Color)
                return true;

            if (type == ShaderUtil.ShaderPropertyType.Vector)
                return false;

            return false;
        }

        private void DrawConfigInfo(MaterialConfig config)
        {
            if (m_Style == null)
            {
                m_Style = new GUIStyle(EditorStyles.foldout);
            }
            m_Style.fixedWidth = 1;

            EditorGUIUtility.fieldWidth = 18;
            config.ShowInInspector = EditorGUILayout.Foldout(config.ShowInInspector, "", m_Style);
            EditorGUIUtility.fieldWidth = 0;

            EditorGUIUtility.labelWidth = 1;
            config.Name = EditorGUILayout.TextField("", config.Name);
            EditorGUIUtility.labelWidth = 0;
        }

        private bool ContainsShader(Shader shader)
        {
            foreach (var kv in m_TileShaders)
            {
                if (kv.Key == shader)
                {
                    return true;
                }
            }
            return false;
        }

        //替换所有material config的相同texture
        private void ShowReplaceTextureWindow(string name, Texture oldTexture, Vector4 oldUVTransform)
        {
            var parameters = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.TextureParameter("旧", "", name, oldTexture, oldUVTransform),
                new ParameterWindow.TextureParameter("新", "", name, null, oldUVTransform)
            };
            ParameterWindow.Open("替换贴图", parameters, (p) =>
            {
                var ok = ParameterWindow.GetTexture(p[0], out var oldTex, out var oldUV);
                ok &= ParameterWindow.GetTexture(p[1], out var newTex, out var newUV);
                if (ok)
                {
                    foreach (var config in m_MaterialConfigs)
                    {
                        config.ReplaceTexture(oldTexture, newTex, newUV);
                    }
                }
                return ok;
            });
        }

        private void ReplaceName()
        {
            var parameters = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("旧", "", ""),
                new ParameterWindow.StringParameter("新", "", "")
            };
            ParameterWindow.Open("替换名称", parameters, (p) =>
            {
                var ok = ParameterWindow.GetString(p[0], out var oldName);
                ok &= ParameterWindow.GetString(p[1], out var newName);
                if (ok)
                {
                    foreach (var config in m_MaterialConfigs) 
                    {
                        config.ReplaceName(oldName, newName);
                    }
                }
                return ok;
            });
        }

        private void DrawSearch()
        {
            m_SearchText = EditorGUILayout.TextField("搜索", m_SearchText, EditorStyles.toolbarSearchField);
        }

        internal void SetSplatMaskAfter()
        {
            for (var i = 0; i < m_YTileCount; ++i)
            {
                for (var j = 0; j < m_XTileCount; ++j)
                {
                    var gameObject = m_Renderer.GetTileGameObject(j, i);
                    var renderer = gameObject.GetComponentInChildren<MeshRenderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        renderer.sharedMaterial.SetTexture("_SplatMask_After", renderer.sharedMaterial.GetTexture("_SplatMask"));
                        EditorUtility.SetDirty(renderer.sharedMaterial);
                    }
                }
            }

            AssetDatabase.SaveAssets();
        }

        private GUIStyle m_Style;
        private GUIStyle m_TextStyle;
        private int m_ActiveShaderIndex = -1;
        private string[] m_ShaderNames;
        private string m_SearchText;
        private List<KeyValuePair<Shader, Material>> m_TileShaders = new();
        private List<MaterialConfig> m_TempList = new();
        private Vector2Int m_LastPaintMaterialCoord = new Vector2Int(-1, -1);
    }
}


//XDay