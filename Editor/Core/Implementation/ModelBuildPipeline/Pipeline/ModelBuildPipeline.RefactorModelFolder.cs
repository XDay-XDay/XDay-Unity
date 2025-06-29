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
using System.IO;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    public partial class ModelBuildPipeline
    {
        /// <summary>
        /// 将模型和动画放到一个文件夹后便可以自动整理模型文件夹
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <param name="recursive">是否检查子文件夹</param>
        public static void CreateModelFolder(string rootFolder, bool recursive)
        {
            var model = GetModel(rootFolder);
            if (model == null)
            {
                bool enumerated = false;
                if (recursive)
                {
                    foreach (var dir in Directory.EnumerateDirectories(rootFolder))
                    {
                        enumerated = true;
                        CreateModelFolder(Helper.ToUnityPath(dir), true);
                    }
                }
                if (!enumerated)
                {
                    Debug.LogError($"{rootFolder}里没有找到模型");
                }
                return;
            }

            var anims = GetAnimationGameObjects(rootFolder);

            string settingFolder = $"{rootFolder}/{SETTING_FOLDER_NAME}";
            string modelFolder = $"{rootFolder}/{MODEL_FOLDER_NAME}";
            string textureFolder = $"{rootFolder}/{TEXTURE_FOLDER_NAME}";
            string materialFolder = $"{rootFolder}/{MATERIAL_FOLDER_NAME}";
            var animFolder = $"{rootFolder}/{ANIMATION_FOLDER_NAME}";
            Helper.CreateDirectory(rootFolder);
            Helper.CreateDirectory(animFolder);
            Helper.CreateDirectory($"{rootFolder}/{MODEL_FOLDER_NAME}");
            Helper.CreateDirectory(settingFolder);
            Helper.CreateDirectory($"{rootFolder}/{MESH_FOLDER_NAME}");
            Helper.CreateDirectory(textureFolder);
            Helper.CreateDirectory(materialFolder);

            var modelPath = AssetDatabase.GetAssetPath(model);
            ExtractTextures(modelPath, textureFolder);
            ExtractMaterials(modelPath, materialFolder);

            //move animations
            foreach (var anim in anims)
            {
                EditorHelper.MoveAsset(anim, animFolder);
            }

            //foreach (var stage in m_Stages)
            //{
            //    CreateSetting(stage.SettingType, settingFolder);
            //}
            AssetDatabase.Refresh();

            var fileName = Helper.GetPathName(modelPath, true);
            AssetDatabase.MoveAsset(modelPath, $"{modelFolder}/{fileName}");
            AssetDatabase.Refresh();

            //删除自动生成的文件夹
            AssetDatabase.DeleteAsset($"{rootFolder}/{Helper.GetPathName(modelPath, false)}.fbm");
        }

        private static void ExtractTextures(string modelPath, string textureFolder)
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (modelImporter == null)
            {
                Debug.LogError("Failed to get ModelImporter for: " + modelPath);
                return;
            }
            modelImporter.ExtractTextures(textureFolder);

            AssetDatabase.Refresh();
        }

        private static void ExtractMaterials(string modelPath, string materialFolder)
        {
            var modelFolder = Helper.GetFolderPath(modelPath);
            var unityMaterialFolder = $"{modelFolder}/Materials";
            bool exists = Directory.Exists(unityMaterialFolder);

            ModelImporter modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (modelImporter == null)
            {
                Debug.LogError("Failed to get ModelImporter for: " + modelPath);
                return;
            }

            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            modelImporter.materialLocation = ModelImporterMaterialLocation.External;
            modelImporter.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            modelImporter.materialSearch = ModelImporterMaterialSearch.Local;
            modelImporter.SaveAndReimport();
            AssetDatabase.WriteImportSettingsIfDirty(modelPath);
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);

            var materials = GetMaterialsInFolder(unityMaterialFolder);
            foreach (var material in materials)
            {
                var oldPath = AssetDatabase.GetAssetPath(material);
                var fileName = Helper.GetPathName(oldPath, true);
                var newPath = $"{materialFolder}/{fileName}";
                AssetDatabase.MoveAsset(oldPath, newPath);
            }

            AssetDatabase.Refresh();

            //modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;
            //modelImporter.SaveAndReimport();

            if (!exists)
            {
                AssetDatabase.DeleteAsset(unityMaterialFolder);
            }

            AssetDatabase.Refresh();
        }

        private static List<Material> GetMaterialsInFolder(string folder)
        {
            return EditorHelper.QueryAssets<Material>(new string[] { folder });
        }

        private static List<GameObject> GetAnimationGameObjects(string folder)
        {
            List<GameObject> anims = new();
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
                    if (hasAnim)
                    {
                        anims.Add(obj);
                    }
                }
            }
            return anims;
        }
    }
}
