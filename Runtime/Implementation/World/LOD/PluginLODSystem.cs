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

namespace XDay.WorldAPI
{
    public delegate void LODCountChangeCallback(int oldCount, int newCount);

    [XDaySerializableClass("PluginLODSystem")]
    internal partial class PluginLODSystem : IPluginLODSystem
    {
        public event LODChangeCallback EventLODChanged;
        public event LODCountChangeCallback EventLODCountChanged;
        public int CurrentLOD => m_CurrentLOD;
        public int PreviousLOD => m_PreviousLOD;
        public int LODCount { get => m_LODs.Length; set => SetLODCountInternal(value); }
        public IWorldLODSystem WorldLODSystem => m_WorldLODSystem;
        public string TypeName => "PluginLODSystem";

        public PluginLODSystem()
            : this(1)
        {
        }

        public PluginLODSystem(int lodCount)
        {
            Debug.Assert(lodCount >= 1);

            m_LODs = new PluginLODSetup[lodCount];
            for (var i = 0; i < lodCount; ++i)
            {
                m_LODs[i] = new PluginLODSetup($"LOD {i}", i * 100, 0);
            }
        }

        public void Init(IWorldLODSystem lodSystem)
        {
            m_WorldLODSystem = lodSystem as WorldLODSystem;
            for (var i = 0; i < m_LODs.Length; ++i)
            {
                m_LODs[i].Altitude = m_WorldLODSystem.GetLOD(i).Altitude;
            }
        }

        public IPluginLODSetup GetLOD(int index)
        {
            if (index >= 0 && index < m_LODs.Length)
            {
                return m_LODs[index];
            }
            return null;
        }

        public bool ValidateLODName()
        {
            for (var i = 1; i < m_LODs.Length; ++i)
            {
                if (m_WorldLODSystem.NameToIndex(m_LODs[i].Name) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void ChangeLODName(string oldName, string newName)
        {
            var lod = QueryLOD(oldName);
            if (lod != null)
            {
                lod.Name = newName;
            }
        }

        public int QueryLOD(float altitude)
        {
            for (var i = m_LODs.Length - 1; i >= 0; --i)
            {
                if (altitude >= m_LODs[i].Altitude)
                {
                    return i;
                }
            }
            return -1;
        }

        public IPluginLODSetup QueryLOD(string name)
        {
            for (var i = 0; i < m_LODs.Length; ++i)
            {
                if (m_LODs[i].Name == name)
                {
                    return m_LODs[i];
                }
            }
            return null;
        }

        public int TryChangeLOD(int curLOD, float altitude)
        {
            if (IsTolerant(curLOD, altitude))
            {
                return curLOD;
            }

            var nextLOD = curLOD;
            for (var lod = m_LODs.Length - 1; lod >= 0; --lod)
            {
                if (altitude >= m_LODs[lod].Altitude)
                {
                    if (lod != curLOD)
                    {
                        if (curLOD == -1 ||
                            altitude - m_LODs[lod].Altitude >= m_LODs[lod].Tolerance)
                        {
                            nextLOD = lod;
                        }
                        else
                        {
                            nextLOD = lod - 1;
                        }
                    }

                    break;
                }
            }
            return Mathf.Clamp(nextLOD, 0, m_LODs.Length - 1);
        }

        public bool Update(float cameraAltitude)
        {
            var changed = false;
            if (!Mathf.Approximately(m_PreviousCheckAltitude, cameraAltitude))
            {
                m_PreviousCheckAltitude = cameraAltitude;

                var newLOD = TryChangeLOD(m_CurrentLOD, cameraAltitude);
                if (newLOD != m_CurrentLOD)
                {
                    changed = true;

                    EventLODChanged?.Invoke(m_CurrentLOD, newLOD);

                    m_PreviousLOD = m_CurrentLOD;
                    m_CurrentLOD = newLOD;
                }
            }
            return changed;
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("PluginLODSystem.Version");
            m_LODs = deserializer.ReadArray("LOD Setups", (index) => {
                return deserializer.ReadSerializable<IPluginLODSetup>($"LOD Setup {index}", false);
            });
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "PluginLODSystem.Version");
            serializer.WriteArray(m_LODs, "LOD Setups", (lodRef, index) => {
                serializer.WriteSerializable(lodRef, $"LOD Setup {index}", converter, false); 
            });
        }

        public void GameDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("PluginLODSystem.Version");
            m_LODs = deserializer.ReadArray("LOD Setups", (index) => {
                return deserializer.ReadSerializable<IPluginLODSetup>($"LOD Setup {index}", true);
            });
        }

        public void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "PluginLODSystem.Version");
            serializer.WriteArray(m_LODs, "LOD Setups", (lodRef, index) => {
                serializer.WriteSerializable(lodRef, $"LOD Setup {index}", converter, true);
            });
        }

        private void SetLODCountInternal(int newCount)
        {
            var oldCount = m_LODs.Length;
            newCount = Mathf.Clamp(newCount, 0, m_WorldLODSystem.LODCount);
            if (newCount == oldCount)
            {
                return;
            }

            var newLODs = new PluginLODSetup[newCount];
            
            var addedCount = newCount - oldCount;
            if (addedCount > 0)
            {
                for (var i = 0; i < oldCount; ++i)
                {
                    newLODs[i] = new PluginLODSetup(m_LODs[i].Name, m_LODs[i].Altitude, m_LODs[i].Tolerance);
                }

                var prevLODAltitude = 0.0f;
                if (oldCount > 0)
                {
                    prevLODAltitude = m_LODs[oldCount - 1].Altitude;
                }

                var worldLODSystem = m_WorldLODSystem as WorldLODSystem;
                for (var i = 0; i < addedCount; ++i)
                {
                    var lod = oldCount + i;
                    newLODs[lod] = new PluginLODSetup(worldLODSystem.GetLODName(lod), prevLODAltitude + 100, 0);
                    prevLODAltitude = newLODs[lod].Altitude;
                }
            }
            else
            {
                for (var i = 0; i < newCount; ++i)
                {
                    newLODs[i] = new PluginLODSetup(m_LODs[i].Name, m_LODs[i].Altitude, m_LODs[i].Tolerance);
                }
            }

            m_LODs = newLODs;

            EventLODCountChanged?.Invoke(oldCount, newCount);
        }

        private bool IsTolerant(int lod, float altitude)
        {
            if (lod < 0)
            {
                return false;
            }
            
            var lodSetup = m_LODs[lod];
            var delta = altitude - lodSetup.Altitude;
            return
                delta >= 0 &&
                delta <= lodSetup.Tolerance;
        }

        [XDaySerializableField(1, "LOD Setups")]
        protected IPluginLODSetup[] m_LODs;
        private int m_CurrentLOD = 0;
        private int m_PreviousLOD = 0;
        private float m_PreviousCheckAltitude = 0;
        protected WorldLODSystem m_WorldLODSystem;

        private const int m_Version = 1;
        private const int m_RuntimeVersion = 1;
    }
}
