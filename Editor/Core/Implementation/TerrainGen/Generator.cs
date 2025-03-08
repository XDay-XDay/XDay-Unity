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


using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XDay.SerializationAPI;
using XDay.UtilityAPI;

namespace XDay.Terrain.Editor
{
    internal class TerrainGenerator
    {
        public TerrainGenerator()
        {
            RegisterCreators();
        }

        public TerrainGenerator(int placeholder)
        {
            RegisterCreators();

            mStartModifier = new Start(GetNextID());
            mStartModifier.Initialize(this);
            AddModifier(mStartModifier);

            mOutputModifier = new Output(GetNextID());
            mOutputModifier.Initialize(this);
            AddModifier(mOutputModifier);
        }

        public void OnDestroy()
        {
            for (var i = mModifiers.Count - 1; i >= 0; --i)
            {
                mModifiers[i].OnDestroy();
            }
        }

        public int GetNextID()
        {
            return --mNextID;
        }

        public void Generate(TerrainModifier toModifier)
        {
            List<TerrainModifier> ancestorModifiers = new();
            TerrainModifier outputModifier = FindOutputModifier();
            FindAncestorModifiers(toModifier, ancestorModifiers);

            if (ancestorModifiers.Count == 0)
            {
                return;
            }

            mStartModifier.Execute();

            foreach (var mod in ancestorModifiers)
            {
                mod.Execute();
            }

            outputModifier ??= mOutputModifier;
            var output = outputModifier as OutputBase;
            output.SetTempInput(toModifier);
            outputModifier.Execute();
            CreateView(toModifier.HasMaskData(), outputModifier);
            output.SetTempInput(null);
        }

        public void AddModifier(TerrainModifier modifier)
        {
            mModifiers.Add(modifier);

            modifier.Initialize(this);

            if (modifier is Start start)
            {
                mStartModifier = start;
            }

            if (modifier is Output output)
            {
                mOutputModifier = output;
            }
        }

        public TerrainModifier GetModifier(int id)
        {
            foreach (var modifier in mModifiers)
            {
                if (modifier.GetID() == id)
                {
                    return modifier;
                }
            }

            Debug.LogError("Modifier not found!");
            return null;
        }

        public List<TerrainModifier> GetModifiers() { return mModifiers; }
        public TerrainModifier GetStartModifier() { return mStartModifier; }
        public TerrainModifier CreateModifier(string typeName)
        {
            Debug.Assert(mModifierCreators.ContainsKey(typeName));
            return mModifierCreators[typeName]();
        }
        public float GetMaxHeight()
        {
            var size = mStartModifier.GetSize();
            return size.MaxHeight;
        }

        public bool GetShowMask() { return mShowMask; }
        public void ShowMask(bool show)
        {
            mShowMask = show;
            if (mPlaneGameObject != null)
            {
                mPlaneGameObject.SetActive(show);
            }
        }

        public Material GetDefaultOutputMaterial()
        {
            var setting = mStartModifier.GetSetting() as Start.Setting;
            return setting.defaultOutputMaterial;
        }

        private void CreateView(bool setMaskTextureToTerrain, TerrainModifier output)
        {
            Helper.DestroyUnityObject(mTerrainGameObject);
            mTerrainGameObject = null;

            Helper.DestroyUnityObject(mPlaneGameObject);
            mPlaneGameObject = null;

            if (mMaskTexture != null)
            {
                Helper.DestroyUnityObject(mMaskTexture);
                mMaskTexture = null;
            }

            var size = output.GetSize();

            CreateMask(size.Resolution, output.GetMaskData());
            CreateTerrain(size.Width, size.Height, size.Resolution, output.GetHeightData(), size.Position, setMaskTextureToTerrain, output as OutputBase);
            if (mShowMask)
            {
                CreatePlane(size.Width, size.Height, size.Position);
            }
        }

        private void CreateTerrain(float width, float height, int resolution, List<float> heights, Vector3 position, bool setMaskTextureToTerrain, OutputBase outputModifier)
        {
            if (resolution > 0 && heights.Count > 0)
            {
                Mesh mesh = new Mesh();

                float deltaX = width / (resolution - 1);
                float deltaY = height / (resolution - 1);

                List<Vector3> positions = new();
                List<Vector3> normals = new();
                List<Vector2> uvs = new();
                List<int> indices = new();
                for (var y = 0; y < resolution; ++y)
                {
                    for (var x = 0; x < resolution; ++x)
                    {
                        float px = x * deltaX;
                        float pz = y * deltaY;
                        float py = heights[y * resolution + x];
                        positions.Add(new Vector3(px, py, pz) + position);
                        uvs.Add(new Vector2(px / width, pz / height));

                        if (x < resolution - 1 && y < resolution - 1)
                        {
                            int v0 = y * resolution + x;
                            int v1 = v0 + 1;
                            int v2 = v0 + resolution;
                            int v3 = v2 + 1;
                            indices.Add(v0);
                            indices.Add(v2);
                            indices.Add(v3);
                            indices.Add(v0);
                            indices.Add(v3);
                            indices.Add(v1);
                        }
                    }
                }

                List<Color> colors = new(positions.Count);
                for (var i = 0; i < colors.Count; ++i)
                {
                    colors.Add(Color.white);
                }

                mesh.MarkDynamic();
                mesh.SetVertices(positions);
                mesh.SetUVs(0, uvs);
                mesh.SetColors(colors);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);
                mesh.RecalculateNormals();

                mTerrainGameObject = new GameObject("Terrain")
                {
                    tag = TerrainGenHelper.TerrainGenObjTag
                };
                var renderable = mTerrainGameObject.AddComponent<MeshRenderer>();
                var meshFilter = mTerrainGameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                var mtl = outputModifier.GetMaterial();
                if (mtl != null)
                {
                    if (setMaskTextureToTerrain)
                    {
                        mtl.SetTexture("_Mask", mMaskTexture);
                    }
                    else
                    {
                        mtl.SetTexture("_Mask", null);
                    }
                }
                else
                {
                    Debug.Log("No terrain material!");
                }
                renderable.sharedMaterial = mtl;
            }
        }
        private void CreatePlane(float width, float height, Vector3 position)
        {
            Mesh mesh = new Mesh();

            Vector3[] positions = new Vector3[]{
                new Vector3(0, 0, 0),
                new Vector3(0, 0, height),
                new Vector3(width, 0, height),
                new Vector3(width, 0, 0),
            };

            for (var idx = 0; idx < positions.Length; ++idx)
            {
                positions[idx] += position;
            }

            Vector2[] uvs = new Vector2[]{
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0),
            };

            int[] indices = {
                0,1,2,0,2,3
            };

            Color[] colors = new Color[positions.Length];
            for (var i = 0; i < colors.Length; ++i)
            {
                colors[i] = Color.white;
            }

            mesh.MarkDynamic();
            mesh.SetVertices(positions);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            mPlaneGameObject = new GameObject("Mask")
            {
                tag = TerrainGenHelper.TerrainGenObjTag
            };
            var renderer = mPlaneGameObject.AddComponent<MeshRenderer>();
            var filter = mPlaneGameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var mtl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Mask.mat");
            renderer.sharedMaterial = mtl;
            if (mtl != null)
            {
                mtl.SetTexture("_Mask", mMaskTexture);
            }

            AssetDatabase.CreateAsset(mesh, "Assets/planeMesh.mesh");
            AssetDatabase.Refresh();
        }

        private void CreateMask(int resolution, List<Color> maskData)
        {
            int textureSize = resolution;
            if (maskData.Count == 0)
            {
                textureSize = 4;
            }
            mMaskTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true);
            if (maskData != null)
            {
                mMaskTexture.SetPixels(maskData.ToArray());
                mMaskTexture.Apply();
            }

            var path = "Assets/maskTexture.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mMaskTexture, path);
            AssetDatabase.Refresh();
        }

        public void RemoveModifier(TerrainModifier modifier)
        {
            mModifiers.Remove(modifier);
        }

        private void FindAncestorModifiers(TerrainModifier modifier, List<TerrainModifier> ancestorModifiers)
        {
            var inputs = modifier.GetInputs();
            foreach (var input in inputs)
            {
                if (input.ConnectedModifierID != 0)
                {
                    FindAncestorModifiers(GetModifier(input.ConnectedModifierID), ancestorModifiers);
                }
            }

            if (modifier is not Start &&
                modifier is not OutputBase)
            {
                ancestorModifiers.Add(modifier);
            }
        }

        private TerrainModifier FindOutputModifier()
        {
            foreach (var modifier in mModifiers)
            {
                if (modifier != mOutputModifier && modifier is OutputBase)
                {
                    return modifier;
                }
            }

            return mOutputModifier;
        }

        private void RegisterCreators()
        {
            mModifierCreators["Start"] = () => { return new Start(); };
            mModifierCreators["Output"] = () => { return new Output(); };
            mModifierCreators["Noise"] = () => { return new Noise(); };
            mModifierCreators["Erosion"] = () => { return new Erosion(); };
            mModifierCreators["HeightMap"] = () => { return new HeightMap(); };
            mModifierCreators["WeightMap"] = () => { return new WeightMap(); };
            mModifierCreators["TextureBlend"] = () => { return new TextureBlend(); };
            mModifierCreators["Fault"] = () => { return new Fault(); };
            mModifierCreators["Combine"] = () => { return new Combine(); };
            mModifierCreators["Slope"] = () => { return new Slope(); };
            mModifierCreators["Height"] = () => { return new Height(); };
            mModifierCreators["ColorGradient"] = () => { return new ColorGradient(); };
            mModifierCreators["FIRFilter"] = () => { return new FIRFilter(); };
            mModifierCreators["RGBA"] = () => { return new RGBA(); };
        }

        public void Save(ISerializer writer, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_Version, "Version");

            writer.WriteList(mModifiers, "Modifiers", (modifier, index) =>
            {
                writer.WriteStructure($"Modifier {index}", () =>
                    {
                        writer.WriteString(modifier.GetName(), "Type Name");
                        modifier.Save(writer, translator);
                    });
            });
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("Version");
            mModifiers = reader.ReadList("Modifiers", (index)=>
            {
                TerrainModifier modifier = null;
                reader.ReadStructure($"Modifier {index}", ()=>
                    {
                    var typeName = reader.ReadString("Type Name");
                    modifier = CreateModifier(typeName);
                    modifier.Load(reader);
                    AddModifier(modifier);
                });
                return modifier;
            });
        }

        private TerrainModifier mStartModifier;
        private TerrainModifier mOutputModifier;
        private List<TerrainModifier> mModifiers = new();
        private GameObject mTerrainGameObject;
        private GameObject mPlaneGameObject;
        private Dictionary<string, Func<TerrainModifier>> mModifierCreators = new();
        private Texture2D mMaskTexture = null;
        private int mNextID = 0;
        private bool mShowMask = true;
        private const int m_Version = 1;
    };
}
