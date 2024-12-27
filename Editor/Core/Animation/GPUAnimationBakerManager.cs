/*
 * Copyright (c) 2024 XDay
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



using System.IO;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.AnimationAPI.Editor
{
    public class GPUAnimationBakerManager
    {
        [MenuItem("XDay/Animation/Bake Selection")]
        private static void BakeSelection()
        {
            foreach (var prefab in Selection.gameObjects)
            {
                Bake(prefab);   
            }
        }

        public static bool Bake(GameObject prefab)
        {
            if (!Check(prefab))
            {
                return false;
            }

            var setting = GetBakeSetting(prefab);

            GPUAnimationBaker baker = null;
            if (setting.AnimationType == AnimationType.Rig)
            {
                baker = new GPURigAnimationBaker();
            }
            else if (setting.AnimationType == AnimationType.Vertex)
            {
                baker = new GPUVertexAnimationBaker();
            }
            else
            {
                Debug.Assert(false, $"Unknown animation type: {setting.AnimationType}");
                return false;
            }

            var ok = baker.Bake(setting);

            AssetDatabase.SaveAssetIfDirty(prefab);

            return ok;
        }

        private static BakeSetting GetBakeSetting(GameObject prefab)
        {
            var setting = prefab.GetComponent<GPUAnimationBakeSetting>().Setting;
            setting ??= new BakeSetting();
            setting.Prefab = prefab;
            setting.SetOutputFolder(Path.GetDirectoryName(AssetDatabase.GetAssetPath(prefab)));
            if (setting.AdvancedSetting.DeleteOutputFolder)
            {
                EditorHelper.DeleteFolderContent(setting.OutputFolder);
            }

            if (!Directory.Exists(setting.OutputFolder))
            {
                Directory.CreateDirectory(setting.OutputFolder);
            }

            AssetDatabase.Refresh();

            return setting;
        }

        private static bool Check(GameObject prefab)
        {
            if (prefab == null || 
                prefab.GetComponentInChildren<SkinnedMeshRenderer>() == null)
            {
                return false;
            }

            return true;
        }
    }
}

//XDay