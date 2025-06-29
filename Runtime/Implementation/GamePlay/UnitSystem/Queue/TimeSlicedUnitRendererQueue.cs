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


namespace XDay.GamePlayAPI
{
    /// <summary>
    /// 每帧最多更新T时间
    /// </summary>
    internal class TimeSlicedUnitRendererQueue : UnitRendererQueueBase
    {
        public TimeSlicedUnitRendererQueue(UnitRendererManager manager)
            : base(manager)
        {
        }

        public override void Update()
        {
            m_Timer.Begin();
            var cur = m_Actions.First;
            while (cur != null)
            {
                var action = cur.Value;
                if (action.Type == ActionType.Create)
                {
                    m_RendererManager.CreateRenderer(action.Unit);
                    m_CreateActions.Remove(action.UnitID);
                }
                else if (action.Type == ActionType.Destroy)
                {
                    m_RendererManager.DestroyRenderer(action.UnitID);
                    m_DestroyActions.Remove(action.UnitID);
                }
                else if (action.Type == ActionType.ChangeLOD)
                {
                    m_RendererManager.ChangeRendererLOD(action.UnitID, action.LOD);
                }
                else if (action.Type == ActionType.Update)
                {
                    m_RendererManager.UpdateRenderer(action.Unit);
                }
                else
                {
                    Log.Instance?.Error($"Invalid action type: {action.Type}");
                }
                var next = cur.Next;
                ReleaseNode(cur);
                cur = next;
                var time = m_Timer.ElapsedSeconds;
                if (time >= m_MaxUpdateSeconds)
                {
                    //Debug.LogError($"经过{time}秒后create个数{createN},destroy个数{destroyN},update个数{updateN}");
                    m_Timer.Stop();
                    break;
                }
            }
        }

        private readonly SimpleStopwatch m_Timer = new();
        private const double m_MaxUpdateSeconds = 5 / 1000.0f;
    }
}
