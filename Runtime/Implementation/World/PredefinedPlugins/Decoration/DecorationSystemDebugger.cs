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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Decoration
{
    internal class DecorationSystemDebugger : MonoBehaviour
    {
        public float HitRadius = 5f;
        public bool Show = true;

        public void Init(IWorld world)
        {
            m_World = world;
            m_DecorationSystem = world.QueryPlugin<DecorationSystem>();
            m_TextRoot = new GameObject("DecorationSystemDebugger Text Root");
        }

        private void OnDestroy()
        {
            Helper.DestroyUnityObject(m_TextRoot);
        }

        private void Update()
        {
            m_TextRoot.SetActive(Show);

            if (!Show)
            {
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                var pos = Helper.RayCastWithXZPlane(Input.mousePosition, m_World.CameraManipulator.Camera);
                List<int> objectIDs = new();
                m_DecorationSystem.QueryDecorationIDsInCircle(pos, HitRadius, objectIDs, DecorationTagType.All);
                foreach (var id in objectIDs)
                {
                    var index = m_DecorationSystem.GetObject(id).ObjectIndex;
                    Debug.LogError($"点中装饰物ID:{id} Index:{index}");
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_World.CameraManipulator.SetPosition(m_World.CameraManipulator.RenderPosition + 500 * m_World.CameraManipulator.Forward);
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                m_World.CameraManipulator.SetPosition(m_World.CameraManipulator.RenderPosition - 500 * m_World.CameraManipulator.Forward);
            }
        }

        private void OnDrawGizmos()
        {
            if (!Show)
            {
                return;
            }

            var bounds = m_DecorationSystem.Bounds;
            var color = Gizmos.color;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            
            var xTileCount = m_DecorationSystem.XGridCount;
            var yTileCount = m_DecorationSystem.YGridCount;
            var tileWidth = m_DecorationSystem.GridWidth;
            var tileHeight = m_DecorationSystem.GridHeight;
            Gizmos.color = Color.magenta;

            bool createObject = m_TextRoot.transform.childCount == 0;
            for (var x = 0; x < xTileCount; ++x)
            {
                for (var y = 0; y < yTileCount; ++y)
                {
                    var center = new Vector3(
                        bounds.min.x + (x + 0.5f) * tileWidth, 
                        0,
                        bounds.min.z + (y + 0.5f) * tileHeight);

                    Gizmos.DrawWireCube(center, new Vector3(tileWidth, 0, tileHeight));

                    if (createObject)
                    {
                        var obj = new GameObject();
                        obj.transform.SetParent(m_TextRoot.transform);
                        obj.transform.position = center - new Vector3(tileWidth * 0.3f, 0, - tileHeight * 0.1f);
                        obj.transform.localScale = Vector3.one * (tileWidth / 3.0f);
                        var nameComponent = obj.AddComponent<DisplayName>();
                        nameComponent.Create($"{x}_{y}", m_World.CameraManipulator.Camera, true);
                    }
                }
            }
            Gizmos.color = color;
        }

        private IWorld m_World;
        private DecorationSystem m_DecorationSystem;
        private GameObject m_TextRoot;
    }
}
