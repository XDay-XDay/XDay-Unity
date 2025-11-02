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
using UnityEngine;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Region.Editor
{
    internal class RegionSystemLayer : WorldObject
    {
        public RegionSystemLayerRenderer Renderer => m_Renderer;
        public RegionSystem System => World.QueryObject<RegionSystem>(m_RegionSystemID);
        public override string TypeName => "RegionSystemLayer";
        public string Name { get => m_Name; set => m_Name = value; }
        public Dictionary<int, RegionObject> Regions => m_Regions;
        public int RegionCount => Regions.Count;
        protected override bool EnabledInternal { get => m_Visible; set => m_Visible = value; }
        protected override WorldObjectVisibility VisibilityInternal
        {
            set { }
            get => WorldObjectVisibility.Visible;
        }

        public RegionSystemLayer()
        {
        }

        public RegionSystemLayer(int id, int index, string name, int regionSystemID)
            : base(id, index)
        {
            m_Name = name;
            m_RegionSystemID = regionSystemID;
        }

        protected override void OnInit()
        {
            foreach (var kv in m_Regions)
            {
                kv.Value.Init(World);
            }

            m_Renderer = new RegionSystemLayerRenderer(System.Renderer.Root.transform, this);

            ShowObjects();
        }

        protected override void OnUninit()
        {
            m_Renderer.OnDestroy();
            foreach (var kv in m_Regions)
            {
                kv.Value.Uninit();
            }
        }

        private void ShowObjects()
        {
            foreach (var kv in m_Regions)
            {
                m_Renderer.ToggleVisibility(kv.Value);
            }
        }

        internal void Update()
        {
            m_Renderer?.Update();
        }

        internal bool Contains(int objectID)
        {
            return m_Regions.ContainsKey(objectID);
        }

        internal void AddObject(RegionObject region)
        {
            m_Regions.Add(region.ID, region);
        }

        internal bool DestroyObject(int regionID)
        {
            foreach (var region in m_Regions.Values)
            {
                if (region.ID == regionID)
                {
                    region.Uninit();
                    m_Regions.Remove(region.ID);
                    return true;
                }
            }
            Debug.Assert(false, $"Destroy object {regionID} failed!");
            return false;
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_EditorVersion, "RegionSystemLayer.Version");

            serializer.WriteString(m_Name, "Name");
            serializer.WriteBoolean(m_Visible, "Visible");
            serializer.WriteObjectID(m_RegionSystemID, "Region System ID", converter);

            var allObjects = new List<RegionObject>();
            foreach (var p in m_Regions)
            {
                allObjects.Add(p.Value);
            }

            serializer.WriteList(allObjects, "Objects", (obj, index) =>
            {
                serializer.WriteSerializable(obj, $"Object {index}", converter, false);
            });
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            deserializer.ReadInt32("RegionSystemLayer.Version");

            m_Name = deserializer.ReadString("Name");
            m_Visible = deserializer.ReadBoolean("Visible");
            m_RegionSystemID = deserializer.ReadInt32("Region System ID");

            var allObjects = deserializer.ReadList("Objects", (index) =>
            {
                return deserializer.ReadSerializable<RegionObject>($"Object {index}", false);
            });
            foreach (var obj in allObjects)
            {
                m_Regions.Add(obj.ID, obj);
            }
        }

        private string m_Name;
        private int m_RegionSystemID;
        private Dictionary<int, RegionObject> m_Regions = new();
        private RegionSystemLayerRenderer m_Renderer = null;
        [SerializeField]
        private bool m_Visible = true;
        private const int m_EditorVersion = 1;
    }
}
