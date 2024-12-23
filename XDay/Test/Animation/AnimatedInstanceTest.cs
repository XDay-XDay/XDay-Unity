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

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using XDay.AnimationAPI;

public class AnimatedInstanceTest : MonoBehaviour
{
    public string AnimationDataPath;

    // Shake is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_AnimatedInstanceManager = IInstanceAnimatorManager.Create((path) => { 
            return AssetDatabase.LoadAssetAtPath<InstanceAnimatorData>(path);
        });

        var instance = m_AnimatedInstanceManager.CreateInstance(AnimationDataPath, Vector3.zero, Vector3.one, Quaternion.identity);
        instance.PlayAnimation("Drunk Walk");

        m_AnimatedInstanceManager.DestroyInstance(instance.ID);

        instance = m_AnimatedInstanceManager.CreateInstance(AnimationDataPath, Vector3.zero, Vector3.one, Quaternion.identity);
        instance.PlayAnimation("Drunk Walk");
    }

    private void OnDestroy()
    {
        m_AnimatedInstanceManager.OnDestroy();
    }

    // Update is called once per frame
    void Update()
    {
        m_AnimatedInstanceManager.Update();
    }

    private IInstanceAnimatorManager m_AnimatedInstanceManager;
}

#endif