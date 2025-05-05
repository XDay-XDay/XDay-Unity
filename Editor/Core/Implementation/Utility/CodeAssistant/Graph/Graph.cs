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

namespace XDay.UtilityAPI.Editor.CodeAssistant
{
    //可以有许多的graph，由graph view绘制
    internal class Graph
    {
        public void Create(List<GroupInfo> groups)
        {
            mGroups = groups;
            for (int i = 0; i < mGroups.Count; ++i)
            {
                CreateGroup(mGroups[i]);
            }
        }

        public void Create(TypeInfo type)
        {
            var group = new GroupInfo();
            EnumerateType(type, group);

            var groups = new List<GroupInfo>() { group};
            mGroups = groups;
            for (int i = 0; i < mGroups.Count; ++i)
            {
                CreateGroup(mGroups[i]);
            }
        }

        void CreateGroup(GroupInfo group)
        {
            foreach (var type in group.types)
            {
                CreateNode(type);
            }

            foreach (var type in group.types)
            {
                foreach (var d in type.derivedTypes)
                {
                    AddDerived(type, d);
                }
                foreach (var b in type.baseTypes)
                {
                    AddBase(type, b);
                }
            }
        }

        public List<GraphNode> GetDerivedNodes(ulong id)
        {
            return mBaseToDerived[id];
        }

        public List<GraphNode> GetBaseNodes(ulong id)
        {
            return mDerivedToBase[id];
        }

        void CreateNode(TypeInfo type)
        {
            var node = new GraphNode(type);
            mAllNodes.Add(node);
        }

        void AddBase(TypeInfo type, TypeInfo baseType)
        {
            var baseNode = GetNode(baseType);
            if (baseNode != null)
            {
                mDerivedToBase.TryGetValue(type.hash, out var list);
                if (list == null)
                {
                    list = new List<GraphNode>();
                    mDerivedToBase[type.hash] = list;
                }
                list.Add(baseNode);
            }
        }

        void AddDerived(TypeInfo type, TypeInfo derivedType)
        {
            var derivedNode = GetNode(derivedType);
            if (derivedNode != null)
            {
                mBaseToDerived.TryGetValue(type.hash, out var list);
                if (list == null)
                {
                    list = new List<GraphNode>();
                    mBaseToDerived[type.hash] = list;
                }
                list.Add(derivedNode);
            }
        }

        public GraphNode GetNode(TypeInfo type)
        {
            foreach (var node in mAllNodes)
            {
                if (node.typeInfo == type)
                {
                    return node;
                }
            }
            return null;
        }

        void EnumerateType(TypeInfo type, GroupInfo group)
        {
            group.AddType(type);
            foreach (var baseType in type.baseTypes){
                if (group.types.Contains(baseType) == false)
                {
                    EnumerateType(baseType, group);
                }
            }
            foreach (var derivedType in type.derivedTypes)
            {
                if (group.types.Contains(derivedType) == false)
                {
                    EnumerateType(derivedType, group);
                }
            }
        }

        public List<GraphNode> nodes { get { return mAllNodes; } }
        public List<GroupInfo> groups { get { return mGroups; } }

        int mID;
        List<GraphNode> mAllNodes = new List<GraphNode>();
        List<GroupInfo> mGroups;
        Dictionary<ulong, List<GraphNode>> mDerivedToBase = new Dictionary<ulong, List<GraphNode>>();
        Dictionary<ulong, List<GraphNode>> mBaseToDerived = new Dictionary<ulong, List<GraphNode>>();
    }
}


