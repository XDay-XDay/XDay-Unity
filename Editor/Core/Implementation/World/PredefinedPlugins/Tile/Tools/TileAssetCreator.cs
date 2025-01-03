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
using System.IO;
using System.Text;
using System.Collections.Generic;
using XDay.UtilityAPI.Editor;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile.Editor
{
    internal class TileCreateInfo
    {
        public string OutputPath;
        public int MaskTextureChannelCount = 4;
        public Material Material;
        public float TileSize;
        public int XTileCount;
        public int YTileCount;
        public string TileName = "Tile";
        public int MaskResolution = 512;
        public bool CreateRoot = false;
        public string MaskName = "_Mask";
    }

    internal class TileAssetCreator
    {
        public bool Create(TileCreateInfo createInfo)
        {
            if (!Validate(createInfo))
            {
                return false;
            }

            Helper.CreateDirectory(createInfo.OutputPath);

            var n = createInfo.XTileCount * createInfo.YTileCount;
            for (var y = 0; y < createInfo.YTileCount; ++y)
            {
                for (var x = 0; x < createInfo.XTileCount; ++x)
                {
                    var idx = y * createInfo.XTileCount + x;
                    EditorUtility.DisplayProgressBar("Creating Tile", $"{idx}/{n}", idx / (float)n);
                    CreateTile(createInfo, x, y);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            m_Mesh = null;

            return true;
        }

        [MenuItem("XDay/World/Tile/Create Tiles")]
        private static void ShowWindow()
        {
            var parameters = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("Name", "", "Ground"),
                new ParameterWindow.ObjectParameter("Material", "", null, typeof(Material), false),
                new ParameterWindow.StringParameter("Output Path", "", "Assets/GroundTile"),
                new ParameterWindow.StringParameter("Mask Name", "", "_Mask"),
                new ParameterWindow.IntParameter("Mask Resolution", "", 512),
                new ParameterWindow.BoolParameter("Create Root", "", true),
                new ParameterWindow.EnumParameter("Mask Format", "", TextureFormat.RGBA),
                new ParameterWindow.FloatParameter("Tile Size", "", 100),
                new ParameterWindow.IntParameter("X Tile Count", "", 2),
                new ParameterWindow.IntParameter("Y Tile Count", "", 2),
            };
            ParameterWindow.Open("Create Tile", parameters, (p) =>
            {
                var ok = ParameterWindow.GetString(p[0], out var name);
                ok &= ParameterWindow.GetObject<Material>(p[1], out var material);
                ok &= ParameterWindow.GetString(p[2], out var outputPath);
                ok &= ParameterWindow.GetString(p[3], out var maskName);
                ok &= ParameterWindow.GetInt(p[4], out var maskResolution);
                ok &= ParameterWindow.GetBool(p[5], out var createRoot);
                ok &= ParameterWindow.GetEnum<TextureFormat>(p[6], out var maskFormat);
                ok &= ParameterWindow.GetFloat(p[7], out var tileSize);
                ok &= ParameterWindow.GetInt(p[8], out var xTileCount);
                ok &= ParameterWindow.GetInt(p[9], out var yTileCount);
                if (ok)
                {
                    var param = new TileCreateInfo
                    {
                        TileName = name,
                        Material = material,
                        OutputPath = $"{outputPath}/{name}",
                        MaskName = maskName,
                        MaskResolution = maskResolution,
                        CreateRoot = createRoot,
                        MaskTextureChannelCount = maskFormat == TextureFormat.RGB ? 3 : 4,
                        TileSize = tileSize,
                        XTileCount = xTileCount,
                        YTileCount = yTileCount,
                    };
                    return new TileAssetCreator().Create(param);
                }
                return false;
            });
        }

        private Mesh CreateMesh(string prefix, float tileSize)
        {
            if (m_Mesh != null)
            {
                return m_Mesh;
            }

            m_Mesh = new Mesh
            {
                vertices = new Vector3[4]
                {
                    new(0, 0, 0),
                    new(0, 0, tileSize),
                    new(tileSize, 0, tileSize),
                    new(tileSize, 0, 0),
                },
                uv = new Vector2[4]
                {
                    new(0, 0),
                    new(0, 1),
                    new(1, 1),
                    new(1, 0),
                },
                triangles = new int[6] { 0, 1, 2, 0, 2, 3 }
            };
            m_Mesh.RecalculateBounds();
            m_Mesh.UploadMeshData(true);
            AssetDatabase.CreateAsset(m_Mesh, $"{prefix}.asset");
            return m_Mesh;
        }

        private void CreateTile(TileCreateInfo createInfo, int x, int y)
        {
            var tileGameObject = new GameObject(createInfo.TileName)
            {
                tag = TileDefine.TILE_ROOT_TAG
            };

            var root = tileGameObject;
            if (createInfo.CreateRoot)
            {
                root = new GameObject();
                tileGameObject.transform.SetParent(root.transform);
            }

            var prefix = $"{createInfo.OutputPath}/{createInfo.TileName}_{x}_{y}_lod0";

            var renderer = tileGameObject.AddComponent<MeshRenderer>();
            var material = Object.Instantiate(createInfo.Material);
            AssetDatabase.CreateAsset(material, $"{prefix}.mat");
            renderer.sharedMaterial = material;
            if (createInfo.MaskResolution > 0)
            {
                var maskTexture = CreateMaskTexture(prefix, createInfo.MaskResolution, createInfo.MaskTextureChannelCount);
                renderer.sharedMaterial.SetTexture(createInfo.MaskName, maskTexture);
            }

            var filter = tileGameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateMesh(prefix, createInfo.TileSize);

            PrefabUtility.SaveAsPrefabAsset(root, $"{prefix}.prefab");
            Helper.DestroyUnityObject(root);
        }
        
        private bool Validate(TileCreateInfo parameter)
        {
            var errorMsg = new StringBuilder();            

            if (parameter.MaskResolution < 0)
            {
                errorMsg.AppendLine("Invalid mask texture tesolution");
            }
            if (parameter.XTileCount == 0 ||
                parameter.YTileCount == 0)
            {
                errorMsg.AppendLine("Invalid tile count");
            }
            if (string.IsNullOrEmpty(parameter.TileName))
            {
                errorMsg.AppendLine("Invalid tile name");
            }
            if (string.IsNullOrEmpty(parameter.OutputPath))
            {
                errorMsg.AppendLine("Invalid asset output path");
            }
            if (parameter.TileSize <= 0)
            {
                errorMsg.AppendLine("Invalid tile size");
            }
            if (parameter.Material == null)
            {
                errorMsg.AppendLine("Invalid material");
            }
            if (parameter.MaskResolution > 0)
            {
                if (string.IsNullOrEmpty(parameter.MaskName))
                {
                    errorMsg.AppendLine("Invalid mask texture shader property name");
                }
                if (parameter.MaskTextureChannelCount != 3 && 
                    parameter.MaskTextureChannelCount != 4)
                {
                    errorMsg.AppendLine("Invalid mask texture channel");
                }
            }

            if (errorMsg.Length != 0)
            {
                EditorUtility.DisplayDialog("Error", errorMsg.ToString(), "OK");
                return false;
            }

            return true;
        }

        private Texture2D CreateMaskTexture(string prefix, int resolution, int channelCount)
        {
            var pixels = new Color32[resolution * resolution];
            for (var i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 0, 0, 0);
            }

            var texturePath = $"{prefix}.tga";
            var texture = new Texture2D(resolution, resolution, channelCount == 3 ? UnityEngine.TextureFormat.RGB24 : UnityEngine.TextureFormat.RGBA32, true);

            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(texturePath, texture.EncodeToTGA());

            AssetDatabase.ImportAsset(texturePath);
            Object.DestroyImmediate(texture);

            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Debug.Assert(texture != null, $"invalid mask texture {texturePath}");

            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            return texture;
        }

        private Mesh m_Mesh;

        private enum TextureFormat
        {
            RGB,
            RGBA,
        }
    }
}


//XDay