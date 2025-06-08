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

namespace XDay.WorldAPI.House.Editor
{
    internal class HouseInstance : House
    {
        public override string TypeName => "EditorHouseInstance";
        public int HouseID => m_HouseID;
        public int ConfigID { get => m_ConfigID; set => m_ConfigID = value; }
        public List<HouseTeleporterInstance> TeleporterInstances => m_TeleporterInstances;
        public List<HouseInteractivePointInstance> InteractivePointInstance => m_InteractivePointInstances;

        public HouseInstance()
        {
        }

        public HouseInstance(int id, int index, string name, float gridSize, IResourceDescriptor descriptor, int houseID)
            : base (id, index, name, gridSize, descriptor)
        {
            m_HouseID = houseID;
        }

        protected override void OnInit()
        {
            base.OnInit();

            foreach (var teleporter in m_TeleporterInstances)
            {
                teleporter.Initialize(this);
            }

            foreach (var point in m_InteractivePointInstances)
            {
                point.Initialize(this);
            }
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "HouseInstance.Version");

            base.EditorSerialize(serializer, label, converter);

            serializer.WriteObjectID(m_HouseID, "House ID", converter);
            serializer.WriteInt32(m_ConfigID, "Config ID");

            serializer.WriteList(m_TeleporterInstances, "Teleporter Instances", (teleporter, index) =>
            {
                serializer.WriteStructure($"Teleporter Instance {index}", () =>
                {
                    teleporter.Save(serializer, converter);
                });
            });

            serializer.WriteList(m_InteractivePointInstances, "Interactive Point Instances", (point, index) =>
            {
                serializer.WriteStructure($"Interactive Point Instance {index}", () =>
                {
                    point.Save(serializer, converter);
                });
            });
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            var version = deserializer.ReadInt32("HouseInstance.Version");

            base.EditorDeserialize(deserializer, label);

            m_HouseID = deserializer.ReadInt32("House ID");
            if (version >= 2)
            {
                m_ConfigID = deserializer.ReadInt32("Config ID");
            }

            m_TeleporterInstances = deserializer.ReadList("Teleporter Instances", (index) =>
            {
                var teleporter = new HouseTeleporterInstance();
                deserializer.ReadStructure($"Teleporter Instance {index}", () =>
                {
                    teleporter.Load(deserializer);
                });
                return teleporter;
            });

            m_InteractivePointInstances = deserializer.ReadList("Interactive Point Instances", (index) =>
            {
                var point = new HouseInteractivePointInstance();
                deserializer.ReadStructure($"Interactive Point Instance {index}", () =>
                {
                    point.Load(deserializer);
                });
                return point;
            });
        }

        protected override Transform GetItemRoot()
        {
            return m_HouseEditor.HouseInstanceRoot.transform;
        }

        protected override void AddBehaviour()
        {
            var behaviour = Root.AddComponent<HouseInstanceBehaviour>();
            behaviour.Initialize(ID, 
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); });
        }

        public void AddInteractivePointInstance(HouseInteractivePointInstance point)
        {
            point.Initialize(this);
            m_InteractivePointInstances.Add(point);
        }

        public void RemoveInteractivePointInstance(HouseInteractivePointInstance point)
        {
            var index = m_InteractivePointInstances.IndexOf(point);
            RemoveInteractivePointInstance(index);
        }

        public void RemoveInteractivePointInstance(int index)
        {
            if (index >= 0 && index < m_InteractivePointInstances.Count)
            {
                m_InteractivePointInstances[index].OnDestroy();
                m_InteractivePointInstances.RemoveAt(index);
            }
        }

        public HouseInteractivePointInstance GetInteractivePointInstance(int interactivePointID)
        {
            foreach (var instance in m_InteractivePointInstances)
            {
                if (instance.InteractivePointID == interactivePointID)
                {
                    return instance;
                }
            }
            return null;
        }

        public HouseTeleporterInstance GetTeleporterInstance(int teleporterID)
        {
            foreach (var instance in m_TeleporterInstances)
            {
                if (instance.TeleporterID == teleporterID)
                {
                    return instance;
                }
            }
            return null;
        }

        public void AddTeleporterInstance(HouseTeleporterInstance teleporter)
        {
            teleporter.Initialize(this);
            m_TeleporterInstances.Add(teleporter);
        }

        public void RemoveTeleporterInstance(HouseTeleporterInstance instance)
        {
            var index = m_TeleporterInstances.IndexOf(instance);
            RemoveTeleporterInstance(index);
        }

        public void RemoveTeleporterInstance(int index)
        {
            if (index >= 0 && index < m_TeleporterInstances.Count)
            {
                m_TeleporterInstances[index].OnDestroy();
                m_TeleporterInstances.RemoveAt(index);
            }
        }

        public HouseTeleporter GetConnectedTeleporter(HouseTeleporter teleporter)
        {
            var teleporterInstance = teleporter as HouseTeleporterInstance;
            if (teleporterInstance.ConnectedID == 0)
            {
                return FindNearestTeleporter(teleporter);
            }
            else
            {
                foreach (var pt in m_TeleporterInstances)
                {
                    if (pt.ConfigID == teleporterInstance.ConnectedID && pt.Enabled)
                    {
                        return pt;
                    }
                }
            }

            return null;
        }

#if false
        public override Vector2Int GetConnectedTeleporterCoordinate(int x, int y)
        {
            foreach (var pt in m_TeleporterInstances)
            {
                if (pt.Enabled)
                {
                    var pos = pt.GameObject.transform.position;
                    pos.y = 0;
                    var coord = PositionToCoordinate(pos);
                    if (coord.x == x &&
                        coord.y == y)
                    {
                        var connectPt = GetConnectedTeleporter(pt) as HouseTeleporterInstance;
                        if (connectPt != null && connectPt.Enabled)
                        {
                            pos = connectPt.GameObject.transform.position;
                            return PositionToCoordinate(pos);
                        }
                    }
                }
            }
            return new Vector2Int(x, y);
        }

        public override bool IsTeleporter(int x, int y)
        {
            foreach (var pt in m_TeleporterInstances)
            {
                if (pt.Enabled)
                {
                    var pos = pt.GameObject.transform.position;
                    pos.y = 0;
                    var coord = PositionToCoordinate(pos);
                    if (coord.x == x &&
                        coord.y == y)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
#endif

        private HouseTeleporter FindNearestTeleporter(HouseTeleporter teleporter)
        {
            HouseTeleporter nearestPt = null;
            var minDistance = float.MaxValue;
            foreach (var pt in m_TeleporterInstances)
            {
                if (pt.Enabled && pt != teleporter)
                {
                    var dis2 = (pt.GameObject.transform.position - teleporter.GameObject.transform.position).sqrMagnitude;
                    if (dis2 < minDistance)
                    {
                        minDistance = dis2;
                        nearestPt = pt;
                    }
                }
            }

            return nearestPt;
        }

        private int m_HouseID;
        private int m_ConfigID;
        private List<HouseTeleporterInstance> m_TeleporterInstances = new();
        private List<HouseInteractivePointInstance> m_InteractivePointInstances = new();
        private const int m_Version = 2;
    }
}