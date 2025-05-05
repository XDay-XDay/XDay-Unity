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

using UnityEngine;

namespace XDay.WorldAPI.Decoration
{
    internal class DecorationObject : WorldObject
    {
        public override Vector3 Scale { get => m_ResourceMetadata.Scale; set => throw new System.NotImplementedException(); }
        public override Quaternion Rotation { get => m_ResourceMetadata.Rotation; set => throw new System.NotImplementedException(); }
        public override Vector3 Position { get => m_PositionInGrid; set => m_PositionInGrid = value; }
        public int LOD => m_LOD;
        public string Path => m_ResourceMetadata.ResourceDescriptor.GetPath(m_LOD);
        public int IndexInGrid => m_IndexInGrid;
        public int GridX => m_GridX;
        public int GridY => m_GridY;
        public int GPUBatchID => m_ResourceMetadata.GPUBatchID;
        public bool IsStaticObject => GPUBatchID < GameDefine.ANIMATOR_BATCH_START_ID;
        public ResourceMetadata ResourceMetadata => m_ResourceMetadata;
        public override string TypeName => "DecorationObject";

        public DecorationObject()
        {
        }

        public DecorationObject(int id, int objectIndex)
            : base(id, objectIndex)
        {
        }

        public void Init(int id, 
            int objectIndex, 
            IWorld world,
            int gridX,
            int gridY,
            int lod, 
            int index,
            float posX,
            float posY,
            float posZ,
            ResourceMetadata resourceMetadata)
        {
            InitNewID(world, id, objectIndex);

            m_Ref = 1;
            m_GridX = gridX;
            m_GridY = gridY;
            m_LOD = lod;
            m_IndexInGrid = index;
            m_PositionInGrid.x = posX;
            m_PositionInGrid.y = posY;
            m_PositionInGrid.z = posZ;
            m_ResourceMetadata = resourceMetadata;
        }

        public void AddRef()
        {
            ++m_Ref;
        }

        public bool ReleaseRef()
        {
            --m_Ref;
            Debug.Assert(m_Ref >= 0);
            return m_Ref == 0;
        }

        public bool IntersectWith(float visibleAreaMinX, float visibleAreaMinZ, float visibleAreaWidth, float visibleAreaHeight)
        {
            var worldBoundsMinX = m_PositionInGrid.x + m_ResourceMetadata.Bounds.xMin;
            if (visibleAreaMinX > worldBoundsMinX + m_ResourceMetadata.Bounds.width ||
                worldBoundsMinX > visibleAreaMinX + visibleAreaWidth)
            {
                return false;
            }

            var worldBoundsMinZ = m_PositionInGrid.z + m_ResourceMetadata.Bounds.yMin;
            if (visibleAreaMinZ > worldBoundsMinZ + m_ResourceMetadata.Bounds.height ||
                worldBoundsMinZ > visibleAreaMinZ + visibleAreaHeight)
            {
                return false;
            }

            return true;
        }

        protected override void OnInit()
        {
        }

        protected override void OnUninit()
        {
        }

        protected override WorldObjectVisibility VisibilityInternal
        {
            set => m_Visibility = value;
            get => m_Visibility;
        }

        protected override bool EnabledInternal
        {
            set => m_Enabled = value;
            get => m_Enabled;
        }

        private int m_GridX;
        private int m_GridY;
        private int m_IndexInGrid;
        private int m_LOD;
        private ResourceMetadata m_ResourceMetadata;
        private Vector3 m_PositionInGrid;
        private WorldObjectVisibility m_Visibility;
        private int m_Ref = 0;
        private bool m_Enabled = true;
    } 
}

//XDay