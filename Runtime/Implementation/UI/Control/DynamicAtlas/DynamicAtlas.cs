using System.Collections.Generic;
using UnityEngine;

namespace XDay.GUIAPI
{
    internal class DynamicAtlas
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="group"></param>
        /// <param name="alignment">压缩格式copy texture时需要按block对齐,alignment就是block大小</param>
        public DynamicAtlas(DynamicAtlasGroup group, int alignment, string name)
        {
            int length = (int)group;
            m_Alignment = alignment;
            m_Width = length;
            m_Height = length;
            m_Padding = alignment * 2;
            if (!string.IsNullOrEmpty(name))
            {
                m_Name = name;
            }
            CreateNewPage();
        }

        public void OnDestroy()
        {
            Clear();

            foreach (var page in m_PageList)
            {
                page.OnDestroy();
            }
        }

        public void Clear()
        {
            foreach (var data in m_UsingTexture)
            {
                DynamicAtlasMgr.S.ReleaseSaveTextureData(data.Value);
            }
            m_UsingTexture.Clear();

            m_GetTextureTaskList.Clear();
            m_WaitAddNewAreaList.Clear();

            foreach (var page in m_PageList)
            {
                page.Clear();
            }
        }

        internal void SetTexture(Sprite sprite, OnCallBackTexRect callBack)
        {
            if (sprite.texture == null)
            {
                Debug.Log("Texture is Null");
                callBack(null, new Rect(0, 0, 0, 0), Vector2.zero);
                return;
            }

            var rect = sprite.textureRect;
            if (rect.width > m_Width || rect.height > m_Height)
            {
                Debug.Log("Texture is too big");
                callBack(null, new Rect(0, 0, 0, 0), Vector2.zero);
                return;
            }

            if (m_UsingTexture.ContainsKey(sprite.name))
            {
                if (callBack != null)
                {
                    SaveTextureData textureData = m_UsingTexture[sprite.name];
                    textureData.referenceCount++;
                    Texture2D tex2D = m_PageList[textureData.texIndex].Texture;
                    callBack(tex2D, textureData.rect,  textureData.alignmentMinOffset);
                }

                return;
            }

            GetTextureData data = DynamicAtlasMgr.S.AllocateGetTextureData();
            data.name = sprite.name;
            data.callback = callBack;
            m_GetTextureTaskList.Add(data);

            OnRenderTexture(data.name, sprite);
        }

        /// <summary>
        /// Image组件用完之后
        /// </summary>
        /// <param name="name"></param>
        public void RemoveTexture(string name, bool clearRange = false)
        {
            if (m_UsingTexture.ContainsKey(name))
            {
                SaveTextureData data = m_UsingTexture[name];
                data.referenceCount--;
                if (data.referenceCount <= 0)//引用计数0
                {
                    if (clearRange)
                    {
                        Debug.Log("Remove Texture:" + name);
                        m_PageList[data.texIndex].RemoveTexture(data.rect);
                    }

                    m_UsingTexture.Remove(name);
                    DynamicAtlasMgr.S.ReleaseSaveTextureData(data);
                }
            }
        }

        private void OnRenderTexture(string name, Sprite sprite)
        {
            if (sprite.texture == null)
            {
                for (int i = m_GetTextureTaskList.Count - 1; i >= 0; i--)
                {
                    GetTextureData task = m_GetTextureTaskList[i];
                    if (task.name.Equals(name))
                    {
                        task.callback?.Invoke(null, new Rect(0, 0, 0, 0), Vector2.zero);
                    }

                    DynamicAtlasMgr.S.ReleaseGetTextureData(task);
                    m_GetTextureTaskList.RemoveAt(i);
                }

                return;
            }

            var rect = sprite.textureRect;
            var alignedWidth = AtlasHelper.CeilRound(rect.width, m_Alignment);
            var alignedHeight = AtlasHelper.CeilRound(rect.height, m_Alignment);
            IntegerRectangle useArea = InsertArea((int)alignedWidth, (int)alignedHeight, out var index);

            Debug.Assert(useArea.x % m_Alignment == 0 && useArea.y % m_Alignment == 0);

            if (useArea == null)
            {
                Debug.LogError("No Area");
                return;
            }

            Rect uv = new((useArea.x), (useArea.y), alignedWidth, alignedHeight);
            var minOffset = m_PageList[index].AddTexture(useArea.x, useArea.y, sprite, m_Alignment);

            SaveTextureData saveTextureData = DynamicAtlasMgr.S.AllocateSaveTextureData();
            saveTextureData.texIndex = index;
            saveTextureData.rect = uv;
            saveTextureData.alignmentMinOffset = minOffset;
            m_UsingTexture[name] = saveTextureData;

            for (int i = m_GetTextureTaskList.Count - 1; i >= 0; i--)
            {
                GetTextureData task = m_GetTextureTaskList[i];
                if (task.name.Equals(name))
                {
                    m_UsingTexture[name].referenceCount++;
                    if (task != null)
                    {
                        Texture2D dstTex = m_PageList[index].Texture;
                        task.callback(dstTex, uv, minOffset);
                    }
                }
                DynamicAtlasMgr.S.ReleaseGetTextureData(task);
                m_GetTextureTaskList.RemoveAt(i);
            }
        }

        private IntegerRectangle InsertArea(int width, int height, out int index)
        {
            IntegerRectangle result = null;
            IntegerRectangle freeArea = null;
            DynamicAtlasPage page = null;
            for (int i = 0; i < m_PageList.Count; i++)
            {
                int fIndex = m_PageList[i].GetFreeAreaIndex(width, height, m_Padding, m_Alignment);
                if (fIndex >= 0)
                {
                    page = m_PageList[i];
                    freeArea = page.FreeAreasList[fIndex];
                    break;
                }
            }

            if (freeArea == null)
            {
                Debug.LogError($"************** {m_Name} No Free Area----Create New Page {m_PageList.Count} *************");
                page = CreateNewPage();
                freeArea = page.FreeAreasList[0];
            }

            result = new IntegerRectangle(freeArea.x, freeArea.y, width, height);
            GenerateNewFreeAreas(result, page);

            page.RemoveFreeArea(freeArea);
            index = page.Index;
            return result;
        }

        private void GenerateNewFreeAreas(IntegerRectangle target, DynamicAtlasPage page)
        {
            int x = target.x;
            int y = target.y;
            int right = target.Right + 1 + m_Padding;
            int top = target.Top + 1 + m_Padding;

            IntegerRectangle targetWithPadding = null;
            if (m_Padding == 0)
                targetWithPadding = target;

            for (int i = page.FreeAreasList.Count - 1; i >= 0; i--)
            {
                IntegerRectangle area = page.FreeAreasList[i];
                if (!(x >= area.Right || right <= area.x || y >= area.Top || top <= area.y))
                {
                    targetWithPadding ??= new IntegerRectangle(target.x, target.y, target.width + m_Padding, target.height + m_Padding);

                    GenerateDividedAreas(targetWithPadding, area, m_WaitAddNewAreaList);
                    IntegerRectangle topOfStack = page.FreeAreasList.Pop();
                    if (i < page.FreeAreasList.Count)
                    {
                        // Move the one on the top to the freed position
                        page.FreeAreasList[i] = topOfStack;
                    }
                }
            }

            FilterSelfSubAreas(m_WaitAddNewAreaList);
            while (m_WaitAddNewAreaList.Count > 0)
            {
                var free = m_WaitAddNewAreaList.Pop();
                page.AddFreeArea(free);
            }
        }

        private void GenerateDividedAreas(IntegerRectangle divider, IntegerRectangle area, List<IntegerRectangle> results)
        {
            int count = 0;

            int rightDelta = area.Right - divider.Right;
            if (rightDelta > 0)
            {
                results.Add(new IntegerRectangle(divider.Right, area.y, rightDelta, area.height));
                count++;
            }

            int leftDelta = divider.x - area.x;
            if (leftDelta > 0)
            {
                results.Add(new IntegerRectangle(area.x, area.y, leftDelta, area.height));
                count++;
            }

            int bottomDelta = area.Top - divider.Top;
            if (bottomDelta > 0)
            {
                results.Add(new IntegerRectangle(area.x, divider.Top, area.width, bottomDelta));
                count++;
            }

            int topDelta = divider.y - area.y;
            if (topDelta > 0)
            {
                results.Add(new IntegerRectangle(area.x, area.y, area.width, topDelta));
                count++;
            }

            if (count == 0 && (divider.width < area.width || divider.height < area.height))
            {
                // Only touching the area, store the area itself
                results.Add(area);

            }
        }

        private void FilterSelfSubAreas(List<IntegerRectangle> areas)
        {
            for (int i = areas.Count - 1; i >= 0; i--)
            {
                IntegerRectangle filtered = areas[i];
                for (int j = areas.Count - 1; j >= 0; j--)
                {
                    if (i != j)
                    {
                        IntegerRectangle area = areas[j];
                        if (filtered.x >= area.x && filtered.y >= area.y && filtered.Right <= area.Right && filtered.Top <= area.Top)
                        {
                            IntegerRectangle topOfStack = areas.Pop();
                            if (i < areas.Count)
                            {
                                // Move the one on the top to the freed position
                                areas[i] = topOfStack;
                            }
                            break;
                        }
                    }
                }
            }
        }

        private DynamicAtlasPage CreateNewPage()
        {
            var page = new DynamicAtlasPage(m_PageList.Count, m_Width, m_Height, m_Name);
            m_PageList.Add(page);
            return page;
        }

        internal List<DynamicAtlasPage> Pages => m_PageList;

        private readonly int m_Width;
        private readonly int m_Height = 0;
        private readonly int m_Padding;
        private readonly int m_Alignment = 0;
        private readonly string m_Name;
        private readonly List<DynamicAtlasPage> m_PageList = new();
        private readonly List<GetTextureData> m_GetTextureTaskList = new();
        private readonly List<IntegerRectangle> m_WaitAddNewAreaList = new();
        private readonly Dictionary<string, SaveTextureData> m_UsingTexture = new();
    }
}