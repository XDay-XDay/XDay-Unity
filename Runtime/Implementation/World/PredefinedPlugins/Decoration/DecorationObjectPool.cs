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

namespace XDay.WorldAPI.Decoration
{
    internal class DecorationObjectPool
    {
        public void Release(DecorationObject decoration)
        {
#if DECORATION_DEBUG
            m_CreatedDecorations.Remove(decoration);
#endif
            m_Pool.Release(decoration);
        }

        public DecorationObject Get(int objectID,
            bool enabled,
            int objectIndex, 
            IWorld world,
            int gridX, 
            int gridY,
            int logicLOD,
            int renderLOD,
            int indexInTile,
            float posX,
            float posY,
            float posZ,
            ResourceMetadata resourceMetadata,
            bool visible)
        {
            var decoration = m_Pool.Get();
            decoration.Init(objectID, objectIndex, world, gridX, gridY, logicLOD, renderLOD, indexInTile, posX, posY, posZ, resourceMetadata);
            decoration.SetVisibility(visible ? WorldObjectVisibility.Invisible : WorldObjectVisibility.Visible);
            decoration.SetEnabled(enabled);
#if DECORATION_DEBUG
            m_CreatedDecorations.Add(decoration);
#endif
            return decoration;
        }

#if DECORATION_DEBUG
        private readonly List<DecorationObject> m_CreatedDecorations = new();
        public void DebugDraw()
        {
#if UNITY_EDITOR
            var color = UnityEditor.Handles.color;
            UnityEditor.Handles.color = Color.red;
            foreach (var obj in m_CreatedDecorations)
            {
                var bounds = GetWorldBounds(obj);
                UnityEditor.Handles.DrawWireCube(bounds.center, bounds.size);
            }
            UnityEditor.Handles.color = color;
#endif
        }

        private Bounds GetWorldBounds(DecorationObject decoration)
        {
            var worldBounds = new Bounds(decoration.Position + decoration.ResourceMetadata.Bounds.center.ToVector3XZ(), decoration.ResourceMetadata.Bounds.size.ToVector3XZ());
            return worldBounds;
        }
#endif

        private readonly IConcurrentObjectPool<DecorationObject> m_Pool = IConcurrentObjectPool<DecorationObject>.Create(() => { return new DecorationObject(); }, 1600);
    }
}

//XDay