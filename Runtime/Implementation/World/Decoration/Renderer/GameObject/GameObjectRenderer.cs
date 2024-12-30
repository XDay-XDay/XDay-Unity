/*
 * Copyright (c) 2024 XDay
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

using UnityEngine.Scripting;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Decoration
{
    [Preserve]
    internal class GameObjectRenderer
    {
        public GameObject GameObject => m_GameObject;

        public void Init(GameObject gameObject)
        {
            Debug.Assert(m_Ref == 0);
            m_Ref = 1;
            m_GameObject = gameObject;
        }

        public void Uninit()
        {
            Debug.Assert(m_Ref == 0);
            m_GameObject = null;
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_GameObject);
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

        private GameObject m_GameObject;
        private int m_Ref = 0;
    }
}

//XDay