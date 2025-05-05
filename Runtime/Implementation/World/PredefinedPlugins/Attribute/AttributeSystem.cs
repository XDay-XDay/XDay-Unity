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
using UnityEngine.Scripting;

namespace XDay.WorldAPI.Attribute
{
    [Preserve]
    internal partial class AttributeSystem : WorldPlugin, IAttributeSystem
    {
        public override List<string> GameFileNames => new() { "attribute" };
        public override string TypeName => "AttributeSystem";
        public override string Name { get => m_Name; set => throw new System.NotImplementedException(); }

        public AttributeSystem()
        {
        }

        protected override void InitInternal()
        {
        }

        protected override void UninitInternal()
        {
        }

        public int GetLayerIndex(string name)
        {
            for (var i = 0; i < m_Layers.Count; ++i)
            {
                if (m_Layers[i].Name == name)
                {
                    return i;
                }
            }
            Debug.LogError($"Invalid attribute layer {name}");
            return -1;
        }

        public string GetLayerName(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < m_Layers.Count)
            {
                return m_Layers[layerIndex].Name;
            }
            Debug.LogError($"Invalid attribute layer index {layerIndex}");
            return null;
        }

        public Vector2Int WorldPositionToCoordinate(int layerIndex, float x, float z)
        {
            var layer = GetLayer(layerIndex);
            if (layer != null)
            {
                return layer.PositionToCoordinate(x, z);
            }
            Debug.LogError($"Invalid attribute layer index {layerIndex}");
            return Vector2Int.zero;
        }

        public bool IsEmptyGrid(int x, int y)
        {
            if (GetLayer(LayerType.Obstacle) is Layer layer)
            {
                return layer.Get(x, y) == 0;
            }
            return false;
        }

        LayerBase GetLayer(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < m_Layers.Count)
            {
                return m_Layers[layerIndex];
            }
            return null;
        }

        LayerBase GetLayer(LayerType type)
        {
            foreach (var layer in m_Layers)
            {
                if (layer.Type == type)
                {
                    return layer;
                }
            }
            return null;
        }

        private string m_Name;
        private List<LayerBase> m_Layers;
    }
}

