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
using XDay.UtilityAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    [CreateAssetMenu(menuName = "XDay/Model/Model Build Pipeline")]
    public partial class ModelBuildPipeline : ScriptableObject
    {
        public int NextID => ++m_NextID;
        public int StageCount => m_Stages.Count;
        public List<ModelBuildPipelineStage> Stages => m_Stages;
        public event Action<ModelBuildPipelineStage> EventCreateStage
        {
            add
            {
                m_EventCreateStage -= value;
                m_EventCreateStage += value;
            }
            remove
            {
                m_EventCreateStage -= value;
            }
        }
        public event Action<ModelBuildPipelineStage> EventDestroyStage
        {
            add
            {
                m_EventDestroyStage -= value;
                m_EventDestroyStage += value;
            }
            remove
            {
                m_EventDestroyStage -= value;
            }
        }

        public bool ContainsStage(Type type)
        {
            foreach (var stage in m_Stages)
            {
                if (stage.GetType() == type)
                {
                    return true;
                }
            }
            return false;
        }

        public ModelBuildPipelineStage CreateStage(Type type, string name)
        {
            if (GetStage(type) != null)
            {
                Debug.LogError($"Stage{type.Name}已经存在");
                return null;
            }

            Debug.Assert(type.IsSubclassOf(typeof(ModelBuildPipelineStage)));

            var id = NextID;
            object[] args = { id };
            var node = Activator.CreateInstance(type, args) as ModelBuildPipelineStage;
            AddStage(node);
            return node;
        }

        public T GetStage<T>() where T : ModelBuildPipelineStage
        {
            return GetStage(typeof(T)) as T;
        }

        public ModelBuildPipelineStage GetStage(Type type)
        {
            foreach (var stage in m_Stages)
            {
                if (stage.GetType() == type)
                {
                    return stage;
                }
            }
            return null;
        }

        public void AddStage(ModelBuildPipelineStage stage)
        {
            if (stage == null)
            {
                Debug.LogError("stage无效!");
                return;
            }

            if (GetStage(stage.GetType()) != null)
            {
                Debug.LogError("stage已经创建!");
                return;
            }

            m_Stages.Add(stage);
            m_EventCreateStage?.Invoke(stage);
        }

        public void RemoveStage(int index)
        {
            if (index >= 0 && index < m_Stages.Count)
            {
                var stage = m_Stages[index];

                foreach (var otherStage in m_Stages)
                {
                    if (otherStage != stage)
                    {
                        otherStage.RemovePreviousStage(stage.ID);
                    }
                }

                m_Stages.RemoveAt(index);
                m_EventDestroyStage?.Invoke(stage);
            }
        }

        public void RemoveStage(ModelBuildPipelineStage stage)
        {
            RemoveStage(m_Stages.IndexOf(stage));
        }

        public ModelBuildPipelineStage GetStage(int index)
        {
            if (index >= 0 && index < m_Stages.Count)
            {
                return m_Stages[index];
            }
            return null;
        }

        public int GetStageIndex(ModelBuildPipelineStage stage)
        {
            return m_Stages.IndexOf(stage);
        }

        public List<AnimationClip> GetAnimations(string rootFolder)
        {
            List<AnimationClip> anims = new();
            var animFolder = $"{rootFolder}/{ANIMATION_FOLDER_NAME}";
            var gameObjects = EditorHelper.QueryAssets<GameObject>(new string[] { animFolder }, true);
            foreach (var obj in gameObjects)
            {
                var type = PrefabUtility.GetPrefabAssetType(obj);
                if (type == PrefabAssetType.Model)
                {
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj));
                    foreach (var asset in allAssets)
                    {
                        if (asset is AnimationClip clip)
                        {
                            anims.Add(clip);
                        }
                    }
                }
            }
            return anims;
        }

        private GameObject GetModel(string folder)
        {
            var gameObjects = EditorHelper.QueryAssets<GameObject>(new string[] { folder }, true);
            foreach (var obj in gameObjects)
            {
                var type = PrefabUtility.GetPrefabAssetType(obj);
                if (type == PrefabAssetType.Model)
                {
                    bool hasAnim = false;
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj));
                    foreach (var asset in allAssets)
                    {
                        if (asset is AnimationClip clip && clip.length >= MIN_ANIM_LENGTH)
                        {
                            hasAnim = true;
                        }
                    }
                    if (!hasAnim)
                    {
                        return obj;
                    }
                }
            }
            return null;
        }

        [SerializeReference]
        private List<ModelBuildPipelineStage> m_Stages = new();
        [SerializeField]
        private int m_NextID;
        [SerializeReference]
        public ModelBuildPipelineSetting Setting;
        private event Action<ModelBuildPipelineStage> m_EventCreateStage;
        private event Action<ModelBuildPipelineStage> m_EventDestroyStage;
        private const float MIN_ANIM_LENGTH = 0.1f;
        //编辑器显示
        public float Zoom = 1;
        public Vector2 ViewPosition;
        public const string ANIMATION_FOLDER_NAME = "Animation";
        public const string MODEL_FOLDER_NAME = "Model";
        public const string SETTING_FOLDER_NAME = "Setting";
        public const string TEXTURE_FOLDER_NAME = "Texture";
        public const string MATERIAL_FOLDER_NAME = "Material";
        public const string MESH_FOLDER_NAME = "Mesh";
    }
}