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

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using XDay.BehaviourTreeAPI;
using XDay.UtilityAPI;

namespace Assets.XDay.Test
{
    internal class BehaviourTreeTest : MonoBehaviour
    {
        public BehaviourTreeState TreeState;

        private void Start()
        {
            Global.InitRuntime(new GameAssetLoader(), EditorHelper.QueryAssetFilePath<VariableManagerState>());
            m_Tree1 = Global.RuntimeBehaviourTreeManager.CreateTree(AssetDatabase.GetAssetPath(TreeState), "Test Tree1");
        }

        private void OnDestroy()
        {
        }

        private void Update()
        {
            m_Tree1?.Tick();
        }

        public void Run()
        {
            m_Tree1?.Run();
        }

        private BehaviourTree m_Tree1;
    }

    [CustomEditor(typeof(BehaviourTreeTest))]
    internal class BehaviourTreeTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Run"))
            {
                (target as BehaviourTreeTest).Run();
            }
        }
    }
}

#endif