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
using UnityEngine.Rendering;

namespace XDay.AnimationAPI.Editor
{
    internal partial class GPURigAnimationBaker : GPUAnimationBaker
    {
        protected override AnimationType GetAnimationType()
        {
            return AnimationType.Rig;
        }

        protected override FilterMode GetTextureFilterMode()
        {
            return FilterMode.Point;
        }

        protected override List<int> GetMeshRigCount(RendererBakeData[] rendererData)
        {
            var meshRigCount = new List<int>();
            foreach (var part in rendererData)
            {
                meshRigCount.Add((part as RendererRiggedAnimationBakeData).RigManager.RigCount);
            }
            return meshRigCount;
        }

        protected override Mesh CreateMeshes(string path, RendererBakeData bakedData, SkinnedMeshRenderer renderer)
        {
            var rigManager = (bakedData as RendererRiggedAnimationBakeData).RigManager;

            var skinnedMesh = renderer.sharedMesh;
            Debug.Assert(!skinnedMesh.HasVertexAttribute(VertexAttribute.TexCoord2), $"{skinnedMesh.name} TexCoord2 will be overridden");
            Debug.Assert(!skinnedMesh.HasVertexAttribute(VertexAttribute.TexCoord3), $"{skinnedMesh.name} TexCoord3 will be overridden");

            var boneWeights = skinnedMesh.boneWeights;
            var mesh = new Mesh
            {
                vertices = skinnedMesh.vertices,
                colors = skinnedMesh.colors,
                normals = skinnedMesh.normals,
                uv = skinnedMesh.uv,
                subMeshCount = skinnedMesh.subMeshCount
            };

            var vertexCount = skinnedMesh.vertexCount;
            var newWeightData = new List<Vector4>(vertexCount);
            var newBoneIndices = new List<Vector4>(vertexCount);
            var bones = renderer.bones;
            for (var i = 0; i < vertexCount; i++)
            {
                newBoneIndices.Add(
                    new Vector4(
                        rigManager.QueryRigIndex(bones[boneWeights[i].boneIndex0]),
                        rigManager.QueryRigIndex(bones[boneWeights[i].boneIndex1]),
                        rigManager.QueryRigIndex(bones[boneWeights[i].boneIndex2]),
                        rigManager.QueryRigIndex(bones[boneWeights[i].boneIndex3])
                        ));

                newWeightData.Add(new Color(
                    boneWeights[i].weight0, 
                    boneWeights[i].weight1, 
                    boneWeights[i].weight2, 
                    boneWeights[i].weight3));
            }
            mesh.SetUVs(2, newBoneIndices);
            mesh.SetUVs(3, newWeightData);

            for (var sub = 0; sub < mesh.subMeshCount; sub++)
            {
                mesh.SetIndices(skinnedMesh.GetIndices(sub), skinnedMesh.GetSubMesh(sub).topology, sub);
            }

            mesh.UploadMeshData(true);

            AssetDatabase.CreateAsset(mesh, path);

            return mesh;
        }

        protected override RendererBakeData SampleAnimation(GameObject prefab, SkinnedMeshRenderer renderer, List<AnimationClip> clips, float frameInterval)
        {
            var sampleGameObject = BakeHelper.QueryAnimationSampleGameObject(prefab);
            var rigManager = new RigManager(renderer);
            var rendererBakeData = new RendererRiggedAnimationBakeData(rigManager);
            var transforms = prefab.GetComponentsInChildren<Transform>(true);

            foreach (var clip in clips)
            {
                var bakeData = new RiggedAnimationBakeData()
                {
                    Length = clip.length,
                    Loop = clip.isLooping,
                    Clip = clip,
                };

                var clipFrameCount = Mathf.FloorToInt(clip.frameRate * clip.length);
                var sampleCount = Mathf.Min(clipFrameCount, Mathf.CeilToInt(clipFrameCount / frameInterval) + 1);
                float deltaTime = sampleCount > 1 ? clip.length / (sampleCount - 1) : 0;

                for (var i = 0; i < sampleCount; i++)
                {
                    clip.SampleAnimation(sampleGameObject, Mathf.Min(i * deltaTime, clip.length));

                    var localToWorldTransforms = GetRigsLocalToWorldTransform(renderer, transforms);

                    var rigTransforms = new Matrix4x4[rigManager.RigCount];
                    for (var idx = 0; idx < renderer.bones.Length; idx++)
                    {
                        var rigIndex = rigManager.QueryRigIndex(renderer.bones[idx]);
                        rigTransforms[rigIndex] = localToWorldTransforms[idx];
                    }

                    bakeData.RigTransformsEachFrame.Add(rigTransforms);
                }

                rendererBakeData.Add(bakeData);
            }

            return rendererBakeData;
        }

        protected override AnimationTextureData CreateTexture(RendererBakeData data)
        {
            var rigData = data as RendererRiggedAnimationBakeData;
            var animationFrameOffset = new Dictionary<string, int>();
            var frameOffset = 0;
            var transformData = new List<Color>();
            foreach (var oneAnimationData in data.AnimationsBakeData)
            {
                animationFrameOffset.Add(oneAnimationData.Clip.name, frameOffset);
                var transforms = (oneAnimationData as RiggedAnimationBakeData).RigTransformsEachFrame;
                var nFrames = oneAnimationData.FrameCount;
                for (var i = 0; i < nFrames; i++)
                {
                    var rigCount = transforms[i].Length;
                    for (var r = 0; r < rigCount; r++)
                    {
                        var rigTransform = transforms[i][r] * rigData.RigManager.GetBindpose(r);
                        transformData.Add(new Color(rigTransform.m00, rigTransform.m01, rigTransform.m02, rigTransform.m03));
                        transformData.Add(new Color(rigTransform.m10, rigTransform.m11, rigTransform.m12, rigTransform.m13));
                        transformData.Add(new Color(rigTransform.m20, rigTransform.m21, rigTransform.m22, rigTransform.m23));
                        transformData.Add(new Color(rigTransform.m30, rigTransform.m31, rigTransform.m32, rigTransform.m33));
                        frameOffset += 4;
                    }
                }
            }

            var textureSize = NextPOTSize(transformData.Count);
            if (textureSize.x > AnimationDefine.MAX_TEXTURE_SIZE ||
                textureSize.y > AnimationDefine.MAX_TEXTURE_SIZE)
            {
                Debug.Assert(false, $"Texture size {textureSize} is invalid, max supported texture size is {AnimationDefine.MAX_TEXTURE_SIZE}X{AnimationDefine.MAX_TEXTURE_SIZE}");
                return null;
            }

            var pixels = new Color[textureSize.x * textureSize.y];
            System.Array.Copy(transformData.ToArray(), pixels, transformData.Count);

            var textureData = new AnimationTextureData
            {
                Size = textureSize,
                Pixels = pixels,
                AnimationsFrameOffset = animationFrameOffset,
            };

            return textureData;
        }

        public static Vector2Int NextPOTSize(int pixelCount)
        {
            var size = Mathf.CeilToInt(Mathf.Sqrt(pixelCount));
            if (Mathf.NextPowerOfTwo(size) == size)
            {
                return new Vector2Int(size, size);
            }

            var textureHeight = Mathf.NextPowerOfTwo(size);
            var textureWidth = textureHeight;
            while (textureHeight != 0)
            {
                if (pixelCount > textureWidth * textureHeight)
                {
                    return new Vector2Int(textureWidth, textureHeight * 2);
                }

                textureHeight /= 2;
            }

            Debug.Assert(false);
            return Vector2Int.zero;
        }

        private int GetIndex(Transform[] transforms, Transform transform)
        {
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == transform)
                {
                    return i;
                }
            }
            return -1;
        }

        private Matrix4x4[] GetRigsLocalToWorldTransform(SkinnedMeshRenderer renderer, Transform[] transforms)
        {
            var rigsLocaltransforms = new Matrix4x4[renderer.bones.Length];
            foreach (var transform in transforms)
            {
                var idx = GetIndex(renderer.bones, transform);
                if (idx < 0)
                {
                    continue;
                }
                rigsLocaltransforms[idx] = transform.localToWorldMatrix;
            }
            return rigsLocaltransforms;
        }
    }
}

//XDay