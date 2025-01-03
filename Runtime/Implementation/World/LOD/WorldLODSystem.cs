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

using XDay.SerializationAPI;
using System;
using UnityEngine;

namespace XDay.WorldAPI
{
    [XDaySerializableClass("World LOD System")]
    internal partial class WorldLODSystem : IWorldLODSystem
    {
        public int LODCount { get => m_Setups.Length; set => SetLODCountInternal(value); }
        public string TypeName => "WorldLODSystem";

        public WorldLODSystem()
        {
            m_Setups = new WorldLODSetup[1]
            {
                new(GetLODName(0), 0),
            };
        }

        public bool CheckIfLODIsSorted()
        {
            var max = float.MinValue;
            for (var i = 0; i < m_Setups.Length - 1; ++i)
            {
                if (m_Setups[i].Altitude < max)
                {
                    return false;
                }

                max = m_Setups[i].Altitude;
            }
            return true;
        }

        public void Sort()
        {
            Array.Sort(m_Setups, (a, b) => {
                return a.Altitude.CompareTo(b.Altitude);
            });
        }

        public float HeightToLOD(float height, float maxHeight)
        {
            var min = -1;
            for (var i = m_Setups.Length - 1; i >= 0; --i)
            {
                if (height >= m_Setups[i].Altitude)
                {
                    min = i;
                    break;
                }
            }

            if (min == -1)
            {
                return 0;
            }

            var max = Mathf.Clamp(min + 1, 0, LODCount - 1);
            float heightMax;
            if (max != min)
            {
                heightMax = m_Setups[max].Altitude;
            }
            else
            {
                heightMax = maxHeight;
            }

            var heightMin = m_Setups[min].Altitude;
            var range = heightMax - heightMin;
            if (Mathf.Approximately(range, 0))
            {
                return max;
            }

            return min + (height - heightMin) / range;
        }

        public IWorldLODSetup GetLOD(int index)
        {
            if (index >= 0 && index < m_Setups.Length)
            {
                return m_Setups[index];
            }
            return null;
        }

        public IWorldLODSetup QueryLOD(string name)
        {
            for (var i = 0; i < m_Setups.Length; ++i)
            {
                if (m_Setups[i].Name == name)
                {
                    return m_Setups[i];
                }
            }
            return null;
        }

        public float GetLODAltitude(int lod)
        {
            var lodLevel = GetLOD(lod);
            if (lodLevel != null)
            {
                return lodLevel.Altitude;
            }

            Debug.Assert(false, $"Invalid lod {lod}");
            return 0;
        }

        public void SetLODHeight(int lod, float height)
        {
            var lodLevel = GetLOD(lod) as WorldLODSetup;
            if (lodLevel != null)
            {
                lodLevel.Altitude = height;
            }
        }

        public int NameToIndex(string name)
        {
            for (var i = 0; i < m_Setups.Length; ++i)
            {
                if (m_Setups[i].Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        public string GetLODName(int lod)
        {
            return $"LOD {lod}";
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("WorldLODSystem.Version");

            m_Setups = deserializer.ReadArray("LOD Setups", (index) => {
                return deserializer.ReadSerializable<WorldLODSetup>($"LOD Setup {index}", false);
            });
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "WorldLODSystem.Version");

            serializer.WriteArray(m_Setups, "LOD Setups", (lod, index) => {
                serializer.WriteSerializable(lod, $"LOD Setup {index}", converter, false);
            });
        }

        public void GameDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("WorldLODSystem.Version");

            m_Setups = deserializer.ReadArray("LOD Setups", (index) => {
                return deserializer.ReadSerializable<WorldLODSetup>($"LOD Setup {index}", true);
            });
        }

        public void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "WorldLODSystem.Version");

            serializer.WriteArray(m_Setups, "LOD Setups", (lod, index) => {
                serializer.WriteSerializable(lod, $"LOD Setup {index}", converter, true);
            });
        }

        private void SetLODCountInternal(int newCount)
        {
            var oldCount = m_Setups.Length;
            if (newCount == oldCount)
            {
                return;
            }

            var newLODs = new WorldLODSetup[newCount];

            int min = Mathf.Min(newCount, oldCount);
            for (var i = 0; i < min; ++i)
            {
                newLODs[i] = new WorldLODSetup(m_Setups[i].Name, m_Setups[i].Altitude);
            }

            var addedCount = newCount - oldCount;
            if (addedCount > 0)
            {
                var prevHeight = m_Setups.Length > 0 ? m_Setups[oldCount - 1].Altitude : 0;
                for (var i = 0; i < addedCount; ++i)
                {
                    var lod = oldCount + i;
                    newLODs[lod] = new WorldLODSetup(GetLODName(lod), prevHeight + 100.0f);
                    prevHeight = newLODs[lod].Altitude;
                }
            }

            m_Setups = newLODs;
        }

        [XDaySerializableField(1, "LOD Setups")]
        private WorldLODSetup[] m_Setups;

        private const int m_Version = 1;
        private const int m_RuntimeVersion = 1;
    }
}