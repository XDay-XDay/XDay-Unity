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

namespace XDay.WorldAPI.Editor
{
    internal class WorldBakerWindow : EditorWindow
    {
        [MenuItem("XDay/地图/烘培")]
        private static void Open()
        {
            if(WorldEditor.WorldManager == null ||
                WorldEditor.WorldManager.FirstWorld == null)
            {
                EditorUtility.DisplayDialog("出错了", "在地图编辑器中烘培!", "确定");
            }

            var window = GetWindow<WorldBakerWindow>("地图烘培");
            EditorHelper.MakeWindowCenterAndSetSize(window);
            window.Show();
        }

        private void OnEnable()
        {
            if (WorldEditor.WorldManager == null)
            {
                return;
            }

            var world = WorldEditor.WorldManager.FirstWorld as World;
            if (world == null)
            {
                return;
            }

            Load();

            GetAllBakeablesInfo(world);

            m_LODSettings = new List<WorldBakerLODSetting>();
            var lodSetting = new WorldBakerLODSetting(1000, 1000, 2, null);
            m_LODSettings.Add(lodSetting);
        }

        private void OnGUI()
        {
            if (WorldEditor.WorldManager == null)
            {
                return;
            }

            var world = WorldEditor.WorldManager.FirstWorld as World;
            if (world == null)
            {
                return;
            }

            EditorGUIUtility.labelWidth = 300;

            m_EnableDepthTexture = EditorGUILayout.ToggleLeft("Use Camera Depth Texture", m_EnableDepthTexture);

            DrawBakeables();

            m_DefaultShader = EditorGUILayout.ObjectField("Baking Shader", m_DefaultShader, typeof(Shader), false, null) as Shader;
            if (m_DefaultShader == null)
            {
                m_DefaultShader = Shader.Find("XDay/TextureTransparent");
            }
            m_TexturePropertyName = EditorGUILayout.TextField("Baking Shader Texture Property Name", m_TexturePropertyName);
            m_TextureSize = EditorGUILayout.IntField("Texture Size", m_TextureSize);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.IntField("Baking LOD Count ", m_LODSettings.Count);
            GUI.enabled = true;

            if (GUILayout.Button("+", GUILayout.MaxWidth(20)))
            {
                AddLODSetting();
            }
            EditorGUILayout.EndHorizontal();

            m_RemoveIndex = -1;
            EditorGUILayout.Foldout(true, "Baking LODs");
            EditorHelper.IndentLayout(() =>
            {
                for (int i = 0; i < m_LODSettings.Count; ++i)
                {
                    int idx = i;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"LOD {i}");
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
                    {
                        m_RemoveIndex = idx;
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();

                    EditorHelper.IndentLayout(() =>
                    {
                        m_LODSettings[idx].BakingHeight = EditorGUILayout.FloatField("Baking Height", m_LODSettings[idx].BakingHeight);
                        m_LODSettings[idx].LODHeight = EditorGUILayout.FloatField("LOD Switch Height", m_LODSettings[idx].LODHeight);
                        m_LODSettings[idx].Resolution = EditorGUILayout.IntField("Resolution", m_LODSettings[idx].Resolution);
                        m_LODSettings[idx].Material = EditorGUILayout.ObjectField("Material", m_LODSettings[idx].Material, typeof(Material), false, null) as Material;
                    });
                }
            });

            if (m_RemoveIndex >= 0)
            {
                RemoveLODSetting(m_RemoveIndex);
            }

            if (GUILayout.Button("Bake"))
            {
                Bake(world);
            }

            EditorGUIUtility.labelWidth = 0;
        }

        private bool Validate()
        {
            if (string.IsNullOrEmpty(m_TexturePropertyName))
            {
                EditorUtility.DisplayDialog("Error", "Texture Property Name Is Empty", "OK");
                return false;
            }

            if (m_DefaultShader == null)
            {
                EditorUtility.DisplayDialog("Error", "Baking shader is null", "OK");
                return false;
            }

            if (!m_DefaultShader.ContainsTextureProperty(m_TexturePropertyName))
            {
                EditorUtility.DisplayDialog("Error", $"Shader Has No Texture Property Called {m_TexturePropertyName}!", "OK");
                return false;
            }

            if (!Helper.IsPOT(m_TextureSize) || m_TextureSize <= 0)
            {
                EditorUtility.DisplayDialog("Error", "Baking texture size must be power of 2", "OK");
                return false;
            }

            if (m_LODSettings.Count == 0)
            {
                return false;
            }

            if (m_EnabledBakeables.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No bakeable is enabled!", "OK");
                return false;
            }

            for (int i = 0; i < m_LODSettings.Count; ++i)
            {
                if (m_LODSettings[i].Resolution <= 0)
                {
                    EditorUtility.DisplayDialog("Error", "Resolution must be greater than 0", "OK");
                    return false;
                }

                if (i != m_LODSettings.Count - 1)
                {
                    if (m_LODSettings[i].Resolution < m_LODSettings[i + 1].Resolution)
                    {
                        EditorUtility.DisplayDialog("Error", $"Invalid resolution in lod {i}", "OK");
                        return false;
                    }
                }
            }

            return true;
        }

        private void Bake(World world)
        {
            Save();

            Bounds bounds = GetEnabledBakeables();

            bool isValid = Validate();
            if (isValid)
            {
                SimpleStopwatch watch = new();
                watch.Begin();

                var worldBaker = new WorldBaker();
                var bakeSetting = new WorldBakerSetting
                {
                    BakingBounds = Helper.ToRect(bounds),
                    BakingTextureSize = m_TextureSize,
                    Camera = null,
                    DefaultBakingShader = m_DefaultShader,
                    LODs = m_LODSettings.ToArray(),
                    OutputFolder = $"{world.Setup.GameFolder}/{WorldDefine.CONSTANT_FOLDER_NAME}/Baked",
                    TexturePropertyName = m_TexturePropertyName,
                    EnableDepthTexture = m_EnableDepthTexture,
                };
                worldBaker.Bake(m_EnabledBakeables, bakeSetting);

                watch.Stop();
                Debug.Log($"Baking cost {watch.ElapsedSeconds} seconds");

                Close();
            }
        }

        private void GetAllBakeablesInfo(World world)
        {
            m_Bakeables = new List<BakeableInfo>();
            for (int i = 0; i < world.PluginCount; ++i)
            {
                var plugin = world.GetPlugin(i);
                if (plugin is IWorldBakeable bakeable)
                {
                    var info = new BakeableInfo(plugin.Name, bakeable.EnableBake, bakeable);
                    m_Bakeables.Add(info);
                }
            }
        }

        private Bounds GetEnabledBakeables()
        {
            Bounds bounds = new();
            m_EnabledBakeables = new List<IWorldBakeable>();

            foreach (var info in m_Bakeables)
            {
                if (info.Enabled)
                {
                    m_EnabledBakeables.Add(info.Bakeable);
                    bounds.Encapsulate(info.Bakeable.Bounds);
                }
            }

            return bounds;
        }

        private void AddLODSetting()
        {
            float cameraHeight = 1000;
            int resolution = 1;
            if (m_LODSettings.Count > 0)
            {
                cameraHeight = m_LODSettings[m_LODSettings.Count - 1].LODHeight + 500;
            }
            var lodSetting = new WorldBakerLODSetting(cameraHeight, cameraHeight, resolution, null);
            m_LODSettings.Add(lodSetting);
        }

        private void RemoveLODSetting(int index)
        {
            m_LODSettings.RemoveAt(index);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(TextureSizeName, m_TextureSize);
            PlayerPrefs.SetInt(EnableDepthTextureName, m_EnableDepthTexture ? 1 : 0);
            PlayerPrefs.SetString(DefaultShaderName, AssetDatabase.GetAssetPath(m_DefaultShader));
            PlayerPrefs.SetString(TexturePropertyNameName, m_TexturePropertyName);

            PlayerPrefs.Save();
        }

        private void Load()
        {
            m_TextureSize = PlayerPrefs.GetInt(TextureSizeName, 1024);
            m_EnableDepthTexture = PlayerPrefs.GetInt(EnableDepthTextureName, 0) != 0;
            m_DefaultShader = AssetDatabase.LoadAssetAtPath<Shader>(PlayerPrefs.GetString(DefaultShaderName));
            m_TexturePropertyName = PlayerPrefs.GetString(TexturePropertyNameName, "_MainTex");
        }

        private void DrawBakeables()
        {
            for (int i = 0; i < m_Bakeables.Count; ++i)
            {
                m_Bakeables[i].Enabled = EditorGUILayout.ToggleLeft(m_Bakeables[i].Name, m_Bakeables[i].Enabled);
            }
        }

        private int m_TextureSize = 1024;
        private int m_RemoveIndex = -1;
        private bool m_EnableDepthTexture = false;
        private Shader m_DefaultShader;
        private string m_TexturePropertyName = "_MainTex";
        private List<BakeableInfo> m_Bakeables;
        private List<WorldBakerLODSetting> m_LODSettings;
        private List<IWorldBakeable> m_EnabledBakeables;
        private const string TextureSizeName = "WorldBaker.TextureSize";
        private const string EnableDepthTextureName = "WorldBaker.EnableDepthTexture";
        private const string DefaultShaderName = "WorldBaker.DefaultShader";
        private const string TexturePropertyNameName = "WorldBaker.TexturePropertyName";

        private class BakeableInfo
        {
            public BakeableInfo(string name, bool enabled, IWorldBakeable bakeable)
            {
                Name = name;
                Bakeable = bakeable;
                Enabled = enabled;
            }

            public string Name;
            public bool Enabled;
            public IWorldBakeable Bakeable;
        }
    }
}
