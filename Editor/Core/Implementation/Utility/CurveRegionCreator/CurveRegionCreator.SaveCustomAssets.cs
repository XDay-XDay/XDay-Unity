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

using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor
{
#if false
    public partial class CurveRegionCreator
    {
        class MeshData
        {
            public MeshData(string path, Mesh mesh)
            {
                this.path = path;
                this.mesh = mesh;
            }

            public string path;
            public Mesh mesh;
            public int offsetInFile = -1;
        }

        class PrefabData
        {
            public PrefabData(string prefabPath, string meshPath, string materialPath)
            {
                this.prefabPath = prefabPath;
                this.meshPath = meshPath;
                this.materialPath = materialPath;
            }

            public string prefabPath;
            public string meshPath;
            public string materialPath;
            public int offsetInFile = -1;
        }

        void AddMeshAsset(Mesh mesh, string path)
        {
            MeshData data = new MeshData(path, mesh);
            mMeshies.Add(data);
        }

        void AddPrefabAsset(string meshPath, string materialPath, string prefabPath)
        {
            PrefabData data = new PrefabData(prefabPath, meshPath, materialPath);
            mPrefabs.Add(data);
        }

        void SaveMeshAsset(BinaryWriter writer, MeshData meshData)
        {
            Debug.Assert(false, "todo");
#if false
            var mesh = meshData.mesh;

            writer.Write(m_Version);

            Helper.WriteVector3Array(writer, mesh.vertices);
            List<Vector4> uvs = new List<Vector4>();
            mesh.GetUVs(0, uvs);
            bool isVector2UV = IsVector2UV(uvs);
            writer.Write(isVector2UV);
            if (isVector2UV)
            {
                writer.Write(uvs.Count);
                for (int i = 0; i < uvs.Count; ++i)
                {
                    writer.Write(uvs[i].x);
                    writer.Write(uvs[i].y);
                }
            }
            else
            {
                Utils.WriteVector4List(writer, uvs);
            }

            bool hasVertexColor = mesh.colors32 != null && mesh.colors32.Length > 0;
            writer.Write(hasVertexColor);
            if (hasVertexColor)
            {
                Utils.WriteColor32Array(writer, mesh.colors32);
            }
            int submeshCount = mesh.subMeshCount;
            writer.Write(submeshCount);

            for (int i = 0; i < submeshCount; ++i)
            {
                var triangles = mesh.GetTriangles(i);
                writer.Write(triangles.Length);
                for (int t = 0; t < triangles.Length; ++t)
                {
                    Debug.Assert(triangles[t] < ushort.MaxValue);
                    writer.Write((ushort)triangles[t]);
                }
            }

            Helper.DestroyUnityObject(meshData.mesh);
#endif
        }

        void CreateMaterialList()
        {
            foreach (var prefab in mPrefabs)
            {
                GetMaterialNameIndex(prefab.materialPath);
            }
        }

        void SavePrefabAsset(BinaryWriter writer, PrefabData prefab)
        {
            writer.Write(VersionSetting.CustomPrefabVersion);

            Utils.WriteString(writer, prefab.prefabPath);

            int meshIndex = GetMeshIndex(prefab.meshPath);
            writer.Write(meshIndex);
            int mtlIndex = GetMaterialNameIndex(prefab.materialPath);
            writer.Write(mtlIndex);
        }

        int GetMaterialNameIndex(string materialPath)
        {
            for (int i = 0; i < mMaterialPaths.Count; ++i)
            {
                if (mMaterialPaths[i] == materialPath)
                {
                    return i;
                }
            }
            mMaterialPaths.Add(materialPath);
            return mMaterialPaths.Count - 1;
        }

        int GetMeshIndex(string meshPath)
        {
            for (int i = 0; i < mMeshies.Count; ++i)
            {
                if (mMeshies[i].path == meshPath)
                {
                    return i;
                }
            }
            Debug.Assert(false, $"can't find mesh: {meshPath}");
            return -1;
        }

        void SaveCustomAsset(string path)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(VersionSetting.CustomAssetBundleVersion);

            int meshCount = mMeshies.Count;
            int prefabCount = mPrefabs.Count;

            //save offset placeholders
            int tablePosition = (int)writer.BaseStream.Position;
            int itemCount = meshCount + prefabCount;
            int[] offsetTable = new int[itemCount];
            for (int i = 0; i < offsetTable.Length; ++i)
            {
                offsetTable[i] = -1;
            }
            Utils.WriteIntArray(writer, offsetTable);
            writer.Write(meshCount);
            writer.Write(prefabCount);

            CreateMaterialList();
            Utils.WriteStringList(writer, mMaterialPaths);

            //save mesh
            for (int i = 0; i < meshCount; ++i)
            {
                offsetTable[i] = (int)writer.BaseStream.Position;
                SaveMeshAsset(writer, mMeshies[i]);
            }

            //save prefab
            writer.Write(prefabCount);
            for (int i = 0; i < prefabCount; ++i)
            {
                offsetTable[i + meshCount] = (int)writer.BaseStream.Position;
                SavePrefabAsset(writer, mPrefabs[i]);
            }

            //fix offset table
            var curPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(tablePosition, SeekOrigin.Begin);
            Utils.WriteIntArray(writer, offsetTable);
            writer.BaseStream.Position = curPos;

            var data = stream.ToArray();
            EditorUtils.WriteFile(path, data);
            writer.Close();
        }

        public bool ExistCustomPrefab(string prefabPath)
        {
            for (int i = 0; i < mPrefabs.Count; ++i){
                if (mPrefabs[i].prefabPath == prefabPath)
                {
                    return true;
                }
            }
            return false;
        }

        bool IsVector2UV(List<Vector4> uv)
        {
            int n = uv.Count;
            for (int i = 0; i < n; ++i)
            {
                if (uv[i].z != 0 || uv[i].w != 0)
                {
                    return false;
                }
            }
            return true;
        }

        List<MeshData> mMeshies = new List<MeshData>();
        List<PrefabData> mPrefabs = new List<PrefabData>();
        List<string> mMaterialPaths = new List<string>();
        private const int m_Version = 1;
    }
#endif
}

