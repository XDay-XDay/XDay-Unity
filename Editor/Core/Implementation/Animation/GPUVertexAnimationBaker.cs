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

namespace XDay.AnimationAPI.Editor
{
    internal partial class GPUVertexAnimationBaker : GPUAnimationBaker
    {
        protected override List<int> GetMeshRigCount(RendererBakeData[] parts)
        {
            return new();
        }

        protected override AnimationType GetAnimationType()
        {
            return AnimationType.Vertex;
        }

        protected override Mesh CreateMeshes(string path, RendererBakeData bakedData, SkinnedMeshRenderer renderer)
        {
            var originalMesh = renderer.sharedMesh;
            originalMesh = CombineMeshVertex(originalMesh);

            var bakedMesh = new Mesh
            {
                vertices = originalMesh.vertices,
                colors = originalMesh.colors,
                normals = originalMesh.normals,
                subMeshCount = originalMesh.subMeshCount
            };

            var originalUV = originalMesh.uv;
            var newUV = new List<Vector3>(originalUV.Length);
            for (var i = 0; i < originalUV.Length; ++i)
            {
                newUV.Add(new Vector3(originalUV[i].x, originalUV[i].y, i));
            }
            bakedMesh.SetUVs(0, newUV);

            for (var i = 0; i < bakedMesh.subMeshCount; ++i)
            {
                bakedMesh.SetIndices(originalMesh.GetIndices(i), originalMesh.GetSubMesh(i).topology, i);
            }

            bakedMesh.UploadMeshData(true);

            Object.DestroyImmediate(originalMesh);

            AssetDatabase.CreateAsset(bakedMesh, path);

            return bakedMesh;
        }

        protected override RendererBakeData SampleAnimation(GameObject prefab, SkinnedMeshRenderer renderer, List<AnimationClip> clips, float frameInterval)
        {
            var sampleGameObject = BakeHelper.QueryAnimationSampleGameObject(prefab);

            var rendererBakeData = new RendererVertexAnimationBakeData();
            var tempMesh = new Mesh();
            foreach (var clip in clips)
            {
                var bakeData = new VertexAnimationBakeData()
                {
                    Clip = clip,
                    Loop = clip.isLooping,
                    Framerate = clip.frameRate,
                    Length = clip.length,
                };

                var clipFrameCount = Mathf.FloorToInt(clip.frameRate * clip.length);
                var sampleCount = Mathf.Min(clipFrameCount, Mathf.CeilToInt(clipFrameCount / frameInterval) + 1);
                float deltaTime = sampleCount > 1 ? clip.length / (sampleCount - 1) : 0;
                for (var i = 0; i < sampleCount; i++)
                {
                    tempMesh.Clear();
                    clip.SampleAnimation(sampleGameObject, Mathf.Min(i * deltaTime, clip.length));
                    renderer.BakeMesh(tempMesh);

                    var combinedMesh = CombineMeshVertex(tempMesh);
                    bakeData.VerticesEachFrame.Add(combinedMesh.vertices);
                    Object.DestroyImmediate(combinedMesh);
                }

                rendererBakeData.Add(bakeData);
            }
            Object.DestroyImmediate(tempMesh);

            for (var i = 0; i < rendererBakeData.AnimationsBakeData.Count; ++i)
            {
                var animData = rendererBakeData.AnimationsBakeData[i] as VertexAnimationBakeData;
                animData.NormalizedLength = (float)animData.FrameCount / rendererBakeData.FrameCountOfAllAnimations;
            }

            return rendererBakeData;
        }

        //newUV and normal maybe wrong after combine
        private Mesh CombineMeshVertex(Mesh mesh)
        {
            var combinedMesh = Object.Instantiate(mesh);
            if (!m_CombineVertex)
            {
                return combinedMesh;
            }
            combinedMesh.Clear();

            var vertices = mesh.vertices;
            var colors = mesh.colors;
            var uvs = mesh.uv;
            var submeshCount = mesh.subMeshCount;

            Dictionary<Vector3, int> uniqueVertices = new();
            List<Vector3> combinedVertices = new();
            List<Color> combinedColors = new();
            List<Vector2> combinedUVs = new();
            var submeshes = new List<int>[submeshCount];
            for (var sub = 0; sub < submeshCount; sub++)
            {
                submeshes[sub] = new();
                var indices = mesh.GetIndices(sub);
                for (var i = 0; i < indices.Length; i++)
                {
                    var index = indices[i];
                    var pos = vertices[index];
                    var exist = uniqueVertices.TryGetValue(pos, out var idx);
                    if (!exist)
                    {
                        combinedVertices.Add(pos);
                        if (colors.Length > 0)
                        {
                            combinedColors.Add(colors[index]);
                        }
                        if (uvs.Length > 0)
                        {
                            combinedUVs.Add(uvs[index]);
                        }
                        idx = combinedVertices.Count - 1;
                        uniqueVertices[pos] = idx;
                    }
                    submeshes[sub].Add(idx);
                }
            }

            combinedMesh.SetVertices(combinedVertices);
            if (combinedColors.Count > 0)
            {
                combinedMesh.SetColors(combinedColors);
            }
            if (combinedUVs.Count > 0)
            {
                combinedMesh.SetUVs(0, combinedUVs);
            }
            combinedMesh.indexFormat = mesh.indexFormat;
            combinedMesh.subMeshCount = submeshCount;

            for (var sub = 0; sub < submeshCount; sub++)
            {
                combinedMesh.SetIndices(submeshes[sub], MeshTopology.Triangles, sub);
            }

            combinedMesh.RecalculateBounds();

            return combinedMesh;
        }

        protected override AnimationTextureData CreateTexture(RendererBakeData data)
        {
            var vertexData = data as RendererVertexAnimationBakeData;
            var textureData = new AnimationTextureData()
            {
                Size = new Vector2Int(vertexData.GetMeshVertexCount(), vertexData.FrameCountOfAllAnimations)
            };

            if (textureData.Size.x > AnimationDefine.MAX_TEXTURE_SIZE ||
                textureData.Size.y > AnimationDefine.MAX_TEXTURE_SIZE)
            {
                Debug.Assert(false, $"Texture size {textureData.Size} is invalid, max supported texture size is {AnimationDefine.MAX_TEXTURE_SIZE}X{AnimationDefine.MAX_TEXTURE_SIZE}");
                return null;
            }

            var frameOffset = 0;
            var pixels = new List<Color>();
            foreach (var animationData in vertexData.AnimationsBakeData)
            {
                var animData = animationData as VertexAnimationBakeData;
                textureData.AnimationsFrameOffset.Add(animationData.Clip.name, frameOffset);
                foreach (var frameVertices in animData.VerticesEachFrame)
                {
                    foreach (var vertex in frameVertices)
                    {
                        pixels.Add(new Color(vertex.x, vertex.y, vertex.z, 0));
                    }
                }
                frameOffset += animData.VerticesEachFrame.Count;
            }
            textureData.Pixels = pixels.ToArray();
            return textureData;
        }

        protected override FilterMode GetTextureFilterMode()
        {
            return FilterMode.Bilinear;
        }

        private bool m_CombineVertex = false;
    }
}

//XDay