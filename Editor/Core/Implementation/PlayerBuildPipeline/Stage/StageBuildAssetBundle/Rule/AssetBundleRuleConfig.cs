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

namespace XDay.PlayerBuildPipeline.Editor
{
    /// <summary>
    /// asset bundle分包配置
    /// </summary>
    public partial class AssetBundleRuleConfig : ScriptableObject
    {
        public AssetBundleRule RootRule = new();
        public int NextID = 1;

        internal AssetBundleBuild[] GenerateAssetBundleBuilds()
        {
            m_BundleBuilds = new();
            m_BundleDirectories = new();

            PreProcess();

            Process();

            var ret = m_BundleBuilds.ToArray();
            m_BundleBuilds = null;

            return ret;
        }

        private void Process()
        {
            foreach (var dirInfo in m_BundleDirectories)
            {
                AssetBundleBuild build = new()
                {
                    assetBundleName = dirInfo.BundleName,
                    assetNames = dirInfo.Files.ToArray(),
                };
                m_BundleBuilds.Add(build);
            }
        }
        
        private List<AssetBundleBuild> m_BundleBuilds = new();
    }

    /// <summary>
    /// AssetBundle分包规则
    /// </summary>
    [System.Serializable]
    public class AssetBundleRule
    {
        public int ID;
        public string Name;
        public string BundleName;
        public string Description;
        public AssetBundleRuleType Type = AssetBundleRuleType.All;
        public string RelativePath = "";
        [SerializeReference]
        public AssetBundleRule ParentRule;
        [SerializeReference]
        public List<AssetBundleRule> SubRules = new();

        public string FullPath
        {
            get
            {
                if (ParentRule == null)
                {
                    return RelativePath;
                }
                return $"{ParentRule.RelativePath}/{RelativePath}";
            }
        }
    }

    public enum AssetBundleRuleType
    {
        /// <summary>
        /// 文件里所有资源打成一个包,包括子文件夹资源
        /// </summary>
        All,
        /// <summary>
        /// 文件夹里每个子文件夹生成一个bundle
        /// </summary>
        SubFolder,
    }
}
