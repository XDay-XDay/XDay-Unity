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

namespace XDay.WorldAPI.House
{
    internal class HouseSystemRenderer
    {
        public HouseSystemRenderer(HouseSystem houseSystem)
        {
            m_Root = new GameObject("House System");

            var n = houseSystem.HouseCount;
            for (var i = 0; i < n; i++)
            {
                var house = houseSystem.GetHouseDataByIndex(i);
                var obj = houseSystem.World.AssetLoader.LoadGameObject(house.PrefabPath);
                if (obj != null)
                {
                    var renderer = new HouseRenderer(obj, house);
                    m_Houses.Add(renderer);
                }
                else
                {
                    Debug.LogError($"Load house {house.PrefabPath} failed");
                }
            }
        }

        public void OnDestroy()
        {
            foreach (var house in m_Houses)
            {
                house.OnDestroy();
            }

            Helper.DestroyUnityObject(m_Root);
        }

        public void SetActive(bool active)
        {
            if (m_Root != null)
            {
                m_Root.SetActive(active);
            }
        }

        private readonly List<HouseRenderer> m_Houses = new();
        private readonly GameObject m_Root;
    }
}
