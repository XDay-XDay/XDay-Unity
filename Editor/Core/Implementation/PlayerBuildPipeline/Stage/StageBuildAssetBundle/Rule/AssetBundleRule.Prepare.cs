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
                    m_BundleDirectories.Add(new BundleDirectoryInfo() { Path = Helper.ToUnityPath(dir), BundleName = $"{rule.BundleName}-{Helper.GetPathName(dir, false)}"});
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
