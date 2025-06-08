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
using XDay.AssetAPI;

namespace XDay.BehaviourTreeAPI
{
    public class RuntimeBehaviourTreeManager
    {
        public SortedDictionary<int, BehaviourTree> Trees => m_Trees;

        public RuntimeBehaviourTreeManager(IAssetLoader assetLoader)
        {
            m_AssetLoader = assetLoader;
            m_NextTreeID = 0;
            m_Trees = new();
        }

        public BehaviourTree CreateTree(string treeAssetPath, string name)
        {
            var treeState = m_AssetLoader.Load<BehaviourTreeState>(treeAssetPath);
            if (treeState == null)
            {
                XDay.Log.Instance.Error($"Load {treeAssetPath} failed!");
                return null;
            }

            var tree = treeState.CreateTree(++m_NextTreeID, name);
            m_Trees.Add(tree.ID, tree);
            return tree;
        }

        public void DestroyTree(BehaviourTree tree)
        {
            if (tree != null)
            {
                tree.Stop();
                m_Trees.Remove(tree.ID);
            }
        }

        private readonly IAssetLoader m_AssetLoader;
        private int m_NextTreeID;
        private readonly SortedDictionary<int, BehaviourTree> m_Trees;
    }
}
