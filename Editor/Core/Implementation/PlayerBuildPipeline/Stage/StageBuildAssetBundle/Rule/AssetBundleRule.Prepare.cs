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
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.PlayerBuildPipeline.Editor
{
    public partial class AssetBundleRuleConfig
    {
        private void PreProcess()
        {
            PreProcess(RootRule);

            m_BundleDirectories.Sort((a, b) => { return b.Path.Length.CompareTo(a.Path.Length); });

            var files = Directory.GetFiles(RootRule.RelativePath, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                AddToBundle(Helper.ToUnityPath(file));
            }
        }

        private void AddToBundle(string filePath)
        {
            if (filePath.EndsWith(".meta") ||
                filePath.EndsWith(".cs") ||
                filePath.EndsWith(".unity"))
            {
                return;
            }

            foreach (var dirInfo in m_BundleDirectories)
            {
                if (filePath.StartsWith(dirInfo.Path))
                {
                    dirInfo.Files.Add(filePath);
                    return;
                }
            }

            Debug.LogError($"file {filePath} not in any bundle!");
        }

        /// <summary>
        /// 找出哪些文件夹需要打成asset bundle已经其包含文件
        /// </summary>
        /// <param name="rootRule"></param>
        private void PreProcess(AssetBundleRule rule)
        {
            foreach (var subRule in rule.SubRules)
            {
                PreProcess(subRule);
            }

            if (rule.Type == AssetBundleRuleType.All)
            {
                m_BundleDirectories.Add(new BundleDirectoryInfo() { Path = rule.FullPath, BundleName = rule.BundleName });
            }
            else if (rule.Type == AssetBundleRuleType.SubFolder)
            {
                var dirs = Directory.GetDirectories(rule.FullPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var dir in dirs)
                {
                    var validDir = Helper.ToUnityPath(dir);
                    m_BundleDirectories.Add(new BundleDirectoryInfo() { Path = Helper.ToUnityPath(dir), BundleName = $"{rule.BundleName}-{Helper.GetPathName(validDir, false)}"});
                }
            }
            else
            {
                Debug.Assert(false, $"Todo: {rule.Type}");
            }
        }

        private List<BundleDirectoryInfo> m_BundleDirectories = new();

        private class BundleDirectoryInfo
        {
            public string Path;
            public string BundleName;
            public List<string> Files = new();
        }
    }
}
