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
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor.CodeAssistant
{
    public partial class GraphView : GraphNodeEditor
    {
        class NodePositionInfo
        {
            public Vector2 totalPosition;
            public int count;
        }

        void LayoutGroups()
        {
            mNodeAveragePositions = new Dictionary<TypeInfo, NodePositionInfo>();
            mNeedLayout = false;
            var groups = mActiveGraph.groups;
            var windowSize = CodeAssistantEditor.GetInstance().position.size;

            mLayoutBounds = Rect2D.empty;

            Vector2 position = Vector2.zero;
            position.y = windowSize.y * 0.25f;
            position.x = -windowSize.x * 0.25f;
            foreach (var group in groups)
            {
                Rect2D groupBounds = LayoutGroup(group, position);
                position.x += groupBounds.width + X_PADDING;
                mLayoutBounds.Add(groupBounds);
            }

            foreach (var pair in mNodeAveragePositions)
            {
                var pos = pair.Value.totalPosition / pair.Value.count;
                var node = mActiveGraph.GetNode(pair.Key);
                if (node != null)
                {
                    node.worldPosition = pos;
                }
            }
        }

        //返回group的bounds
        Rect2D LayoutGroup(GroupInfo group, Vector2 position)
        {
            Rect2D groupBounds = Rect2D.empty;
            foreach (var rootType in group.rootTypes)
            {
                Rect2D nodeBounds = LayoutNode(rootType, group, position, true, out _);
                groupBounds.Add(nodeBounds);
                position.x += nodeBounds.width + X_PADDING;
            }
            return groupBounds;
        }

        Rect2D LayoutNode(TypeInfo type, GroupInfo group, Vector2 position, bool isRoot, out Vector2 nodeSize)
        {
            Rect2D nodeBounds = Rect2D.empty;
            GraphNode node = mActiveGraph.GetNode(type);
            nodeSize = Vector2.zero;
            if (node != null)
            { 
                Debug.Assert(node != null);
                CalculateNodeSize(type, node);

                nodeSize = node.size;
                if (!isRoot)
                {
                    position.y -= (nodeSize.y + Y_PADDING);
                }

                if (type.isExternal)
                {
                    position.y += (nodeSize.y + Y_PADDING);
                }

                nodeBounds.Add(position.x, position.y, nodeSize.x, nodeSize.y);

                AddNodePosition(type, position);

                foreach (var d in type.derivedTypes)
                {
                    var childNodeBounds = LayoutNode(d, group, position, false, out var childNodeSize);
                    nodeBounds.Add(childNodeBounds);
                    position.x += childNodeSize.x + X_PADDING;   
                }
            }
            return nodeBounds;
        }

        void AddNodePosition(TypeInfo type, Vector2 position)
        {
            mNodeAveragePositions.TryGetValue(type, out var info);
            if (info == null)
            {
                info = new NodePositionInfo();
                mNodeAveragePositions[type] = info;
            }
            info.count++;
            info.totalPosition += position;
        }

        void CalculateNodeSize(TypeInfo type, GraphNode node)
        {
            var textSize = mTextFieldStyle.CalcSize(new GUIContent(node.typeInfo.nameInfo.modifiedName));
            float oneItemDisplayHeight = textSize.y;
            int methodCount = Mathf.Min(type.methods.Count, MAX_DISPLAY_ITEM_COUNT);
            int propertyCount = Mathf.Min(type.properties.Count, MAX_DISPLAY_ITEM_COUNT);
            float maxSize = textSize.x;
            for (int i = 0; i < methodCount; ++i)
            {
                var size = mTextFieldStyle.CalcSize(new GUIContent(node.typeInfo.methods[i].modifiedName));
                if (size.x > maxSize)
                {
                    maxSize = size.x;
                }
            }
            for (int i = 0; i < propertyCount; ++i)
            {
                var size = mTextFieldStyle.CalcSize(new GUIContent(node.typeInfo.properties[i].fullName));
                if (size.x > maxSize)
                {
                    maxSize = size.x;
                }
            }

            float extendingWidth = 40;
            float nodeWidth = maxSize + extendingWidth;

            float propertyOffset = methodCount * oneItemDisplayHeight + NAME_LABEL_HEIGHT;
            float totalHeight = NAME_LABEL_HEIGHT + propertyOffset + propertyCount * oneItemDisplayHeight;
            node.size = new Vector2(nodeWidth, totalHeight);
            node.methodListPositionOffset = NAME_LABEL_HEIGHT;
            node.propertyListPositionOffset = propertyOffset;
            node.oneItemDisplayHeight = oneItemDisplayHeight;
        }

        Dictionary<TypeInfo, NodePositionInfo> mNodeAveragePositions = new Dictionary<TypeInfo, NodePositionInfo>();
        const float NAME_LABEL_HEIGHT = 20;
        const int MAX_DISPLAY_ITEM_COUNT = 5;
        const float DETAIL_BUTTON_WIDTH = 20;
        const float DETAIL_BUTTON_HEIGHT = 20;
        const float X_PADDING = 40;
        const float Y_PADDING = 40;
        Rect2D mLayoutBounds;
        GUIStyle mTextFieldStyle;
    }
}

