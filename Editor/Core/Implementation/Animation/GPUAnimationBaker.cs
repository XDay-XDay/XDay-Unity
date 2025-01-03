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
    internal abstract partial class GPUAnimationBaker
    {
        public bool Bake(BakeSetting setting)
        {
            if (Check(setting.Prefab))
            {
                var renderersData = BakeRenderers(setting);
                CreatePrefab(setting, renderersData);
                AssetDatabase.Refresh();
                return true;
            }
            return false;
        }

        protected MaterialContainer CreateMaterials(BakeSetting setting, Material[] originalMaterials, MeshRenderer bakedRenderer, Texture2D animTexture, int materialIndexOffset)
        {
            var newMaterials = new Material[originalMaterials.Length];
            for (var i = 0; i < originalMaterials.Length; i++)
            {
                newMaterials[i] = new Material(setting.Shader)
                {
                    name = originalMaterials[i].name,
                    enableInstancing = true,
                };

                BakeHelper.AssignRenderQueue(newMaterials[i], originalMaterials[i], setting.AdvancedSetting.RenderQueue);
                BakeHelper.CopyMaterialProperties(newMaterials[i], originalMaterials[i]);

                newMaterials[i].SetTexture(AnimationDefine.ANIM_TEXTURE_NAME, animTexture);

                AssetDatabase.CreateAsset(newMaterials[i], setting.MaterialPath(materialIndexOffset + i));
            }
            bakedRenderer.sharedMaterials = newMaterials;
            return new MaterialContainer() { Materials = newMaterials };
        }

        private void CreatePrefab(BakeSetting setting, RendererBakeData[] renderersBakeData)
        {
            var baked = NewPrefab(setting.Prefab);

            var animatedInstanceBakeData = CreateAnimatedInstanceBakeData(setting);
            
            CreateMeshRenderers(baked, setting, animatedInstanceBakeData, renderersBakeData);

            var renderersTextureData = CreateMaterialsAndTextures(setting, baked, animatedInstanceBakeData, renderersBakeData);
            var animationMetadata = CreateAnimationMetadata(setting, renderersBakeData, renderersTextureData);
            if (animationMetadata != null)
            {
                if (setting.RenderMode == RenderMode.BatchRendererGroup)
                {
                    animatedInstanceBakeData.Metadata = animationMetadata;
                }
                else
                {
                    var animationSetting = baked.AddComponent<AnimationEntrance>();
                    animationSetting.AnimationType = GetAnimationType();
                    animationSetting.Metadata = animationMetadata;

                    Modify(setting, baked);

                    FileUtil.DeleteFileOrDirectory(setting.PrefabPath());
                    PrefabUtility.SaveAsPrefabAsset(baked, setting.PrefabPath());
                }
            }

            if (animatedInstanceBakeData != null)
            {
                AssetDatabase.CreateAsset(animatedInstanceBakeData, setting.BatchRendererGroupDataPath());
            }

            Object.DestroyImmediate(baked);
        }

        private GameObject NewPrefab(GameObject prefab)
        {
            var baked = Object.Instantiate(prefab);
            baked.name = prefab.name;
            baked.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            baked.transform.localScale = Vector3.one;
            return baked;
        }

        private InstanceAnimatorData CreateAnimatedInstanceBakeData(BakeSetting setting)
        {
            InstanceAnimatorData data = null;
            if (setting.RenderMode == RenderMode.BatchRendererGroup)
            {
                data = ScriptableObject.CreateInstance<InstanceAnimatorData>();
                data.AnimType = GetAnimationType();
                setting.Prefab.GetComponent<GPUAnimationBakeSetting>().Setting.AdvancedSetting.InstanceAnimatorData = data;
                EditorUtility.SetDirty(setting.Prefab);
            }
            return data;
        }

        private void CreateMeshRenderers(GameObject baked, BakeSetting setting, InstanceAnimatorData animatedInstanceBakeData, RendererBakeData[] bakedDatas)
        {
            var skinnedMeshRenderers = baked.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (animatedInstanceBakeData != null)
            {
                animatedInstanceBakeData.Meshes = new Mesh[skinnedMeshRenderers.Length];
            }

            for (var i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                var skinnedMeshRenderer = skinnedMeshRenderers[i];

                var meshRenderer = skinnedMeshRenderer.gameObject.AddComponent<MeshRenderer>();
                BakeHelper.CopyRendererProperties(skinnedMeshRenderer, meshRenderer);

                var meshFilter = skinnedMeshRenderer.gameObject.AddComponent<MeshFilter>();
                meshFilter.gameObject.transform.localScale = Vector3.one;
                meshFilter.gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                meshFilter.sharedMesh = CreateMeshes(setting.MeshPath(i), bakedDatas[i], skinnedMeshRenderer);

                Object.DestroyImmediate(skinnedMeshRenderer);

                if (animatedInstanceBakeData != null)
                {
                    animatedInstanceBakeData.Meshes[i] = meshFilter.sharedMesh;
                }
            }
        }

        private RendererBakeData[] BakeRenderers(BakeSetting setting)
        {
            var skinnedMeshRenderers = setting.Prefab.transform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var bakedDatas = new RendererBakeData[skinnedMeshRenderers.Length];
            for (var i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                bakedDatas[i] = SampleAnimation(setting.Prefab, skinnedMeshRenderers[i], setting.Animations, setting.AdvancedSetting.SampleFrameInterval);
            }
            return bakedDatas;
        }

        private AnimationBakeMetadata CreateAnimationMetadata(BakeSetting setting, RendererBakeData[] renderersBakeData, AnimationTextureData[] meshesTextureData)
        {
            var defaultClip = setting.GetDefaultAnimation();
            if (defaultClip == null)
            {
                return null;
            }

            var data = ScriptableObject.CreateInstance<AnimationBakeMetadata>();
            data.FrameCountOfAllAnimations = renderersBakeData[0].FrameCountOfAllAnimations;
            data.Animations = new();
            data.DefaultAnimName = defaultClip.name;

            foreach (var clip in renderersBakeData[0].AnimationsBakeData)
            {
                var name = clip.Clip.name;
                data.Animations.Add(new AnimationMetadata()
                {
                    Name = name,
                    GUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip.Clip)),
                    Length = clip.Length,
                    Loop = clip.Loop,
                    FrameCount = clip.FrameCount,
                    MeshFrameOffset = GetMeshFrameOffsets(renderersBakeData, meshesTextureData, name),
                    MeshBoneCount = GetMeshRigCount(renderersBakeData),
                });
            }

            AssetDatabase.CreateAsset(data, setting.AnimationMetadataPath());

            return data;
        }

        private List<int> GetMeshFrameOffsets(RendererBakeData[] parts, AnimationTextureData[] textureDataForEachPart, string animName)
        {
            var offsets = new List<int>();
            for (var i = 0; i < parts.Length; i++)
            {
                var partTextureData = textureDataForEachPart[i];
                var offset = partTextureData.AnimationsFrameOffset[animName];
                offsets.Add(offset);
            }
            return offsets;
        }

        protected AnimationTextureData[] CreateMaterialsAndTextures(BakeSetting setting, GameObject bakedPrefab, InstanceAnimatorData animatedInstanceBakeData, RendererBakeData[] renderersData)
        {
            if (animatedInstanceBakeData != null)
            {
                animatedInstanceBakeData.Materials = new List<MaterialContainer>(renderersData.Length);
            }
            var meshRenderers = bakedPrefab.GetComponentsInChildren<MeshRenderer>();
            var skinnedMeshRenderers = setting.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            var rendererTexturesData = new AnimationTextureData[renderersData.Length];

            var materialIndexOffset = 0;
            for (var i = 0; i < renderersData.Length; i++)
            {
                rendererTexturesData[i] = CreateTexture(renderersData[i]);

                var animTexture = BakeHelper.CreateTexture(rendererTexturesData[i].Pixels, rendererTexturesData[i].Size, setting.TexturePath(i), GetTextureFilterMode());

                var materials = CreateMaterials(setting, skinnedMeshRenderers[i].sharedMaterials, meshRenderers[i], animTexture, materialIndexOffset);
                materialIndexOffset += materials.Materials.Length;

                if (animatedInstanceBakeData != null)
                {
                    animatedInstanceBakeData.Materials.Add(materials);
                }
            }

            return rendererTexturesData;
        }

        protected abstract Mesh CreateMeshes(string path, RendererBakeData bakedData, SkinnedMeshRenderer renderer);
        protected abstract RendererBakeData SampleAnimation(GameObject prefab, SkinnedMeshRenderer renderer, List<AnimationClip> clips, float sampleFrameInterval);
        protected abstract List<int> GetMeshRigCount(RendererBakeData[] parts);
        protected abstract AnimationType GetAnimationType();
        protected abstract AnimationTextureData CreateTexture(RendererBakeData bakeData);
        protected abstract FilterMode GetTextureFilterMode();
    }
}

//XDay