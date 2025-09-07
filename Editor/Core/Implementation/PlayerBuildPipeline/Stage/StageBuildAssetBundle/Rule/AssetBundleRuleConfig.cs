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

        internal AssetBundleBuild[] GenerateAssetBundleBuilds()
        {
            m_BundleBuilds = new();

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
