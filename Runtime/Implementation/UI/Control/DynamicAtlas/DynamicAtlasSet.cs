using System.Collections.Generic;

namespace XDay.GUIAPI
{
    internal class DynamicAtlasSet
    {
        public string Name => m_Name;

        public DynamicAtlasSet(string name)
        {
            m_Name = name;
            GetDynamicAtlas(DynamicAtlasGroup.Size_2048, name);
        }

        public void OnDestroy()
        {
            foreach (var kv in m_DynamicAtlasMap)
            {
                kv.Value.OnDestroy();
            }
            m_DynamicAtlasMap.Clear();
        }

        public void Clear()
        {
            foreach (var kv in m_DynamicAtlasMap)
            {
                kv.Value.Clear();
            }
        }

        public DynamicAtlas GetDynamicAtlas(DynamicAtlasGroup group, string name)
        {
            DynamicAtlas atlas;
            if (m_DynamicAtlasMap.ContainsKey(group))
            {
                atlas = m_DynamicAtlasMap[group];
            }
            else
            {
                atlas = new DynamicAtlas(group, alignment: AtlasHelper.GetAlignment(), name);
                m_DynamicAtlasMap[group] = atlas;
            }
            return atlas;
        }

        internal Dictionary<DynamicAtlasGroup, DynamicAtlas> Atlases => m_DynamicAtlasMap;

        private readonly string m_Name;
        private readonly Dictionary<DynamicAtlasGroup, DynamicAtlas> m_DynamicAtlasMap = new();
    }
}
