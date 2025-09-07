using System.Collections.Generic;
using System.Diagnostics;
using XDay.GUIAPI;

namespace XDay.GUIAPI
{
    internal class AtlasOperationQueue
    {
        public void Add(UIImage image)
        {
            m_Queue.Add(image);
        }

        public void Remove(UIImage image)
        {
            for (var i = 0; i < m_Queue.Count; i++)
            {
                if (m_Queue[i] == image)
                {
                    m_Queue[i] = m_Queue[^1];
                    m_Queue.RemoveAt(m_Queue.Count - 1);
                }
            }
        }

        public void Update()
        {
            var n = 0;
            m_Stopwatch.Restart();
            for (var i = m_Queue.Count - 1; i >= 0; i--)
            {
                var image = m_Queue[i];
                if (image != null)
                {
                    image.Process();
                    ++n;
                }
                m_Queue.RemoveAt(i);
                var timeCost = m_Stopwatch.ElapsedMilliseconds;
                if (timeCost > m_MaxCostMs)
                {
                    //UnityEngine.Debug.LogError($"处理了{n}个,耗时{timeCost}毫秒,在第{UnityEngine.Time.frameCount}帧");
                    break;
                }
            }
        }

        private List<UIImage> m_Queue = new();
        private Stopwatch m_Stopwatch = new();
        private const int m_MaxCostMs = 10;
    }
}
