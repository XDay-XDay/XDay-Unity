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


using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor.CodeAssistant
{
    public partial class GraphView : GraphNodeEditor
    {
        [System.Flags]
        enum DetailInfoType
        {
            None = 0,
            ClassComment = 1,
            MethodComment = 2,
            FullMethodList = 4,
            PropertyComment = 8,
            FullPropertyList = 16,
        }

        class DetailDisplayInfo
        {
            public DetailInfoType detailType = DetailInfoType.None;
            public GraphNode node;
            public Object obj;
            public float widthMultiplier = 1;
        }

        struct LinePair
        {
            public LinePair(GraphNode a, GraphNode b)
            {
                this.a = a;
                this.b = b;
            }

            public GraphNode a;
            public GraphNode b;
        }

        class SelectionInfo
        {
            public List<GraphNode> nodes = new List<GraphNode>();
        };

        public GraphView() : base(100000, 100000)
        {
            mButtonBackground = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }
            mButtonBackground.SetPixels32(pixels);
            mButtonBackground.Apply();
        }
        
        public void OnDestroy()
        {
            UnityEngine.Object.DestroyImmediate(mButtonBackground);
            mButtonBackground = null;
        }

        protected override void OnReset()
        {
            mActiveGraph = null;
            mDetailInfo.node = null;
            mDetailInfo.detailType = DetailInfoType.None;
        }

        internal void SetGraph(Graph graph, float windowWidth, float windowHeight)
        {
            ResetViewPosition();
            //mAlignToGrid = true;
            Reset();

            mViewer.SetWorldPosition(-windowWidth * 0.5f, -windowHeight * 0.5f);
            mActiveGraph = graph;

            mNeedLayout = true;
        }

        protected override void OnDrawGUI()
        {
            if (mTextFieldStyle == null)
            {
                mTextFieldStyle = GUI.skin.textField;
            }

            if (mNeedLayout)
            {
                LayoutGroups();
            }

            if (mActiveGraph == null)
            {
                return;
            }

            Handles.BeginGUI();

            DrawNodeLines();

            //draw nodes
            foreach (var node in mActiveGraph.nodes)
            {
                DrawNode(node);
            }

            DrawDetailInfo();
            //DrawGridState();

            //DrawBounds();

            Handles.EndGUI();
        }

        void DrawBounds()
        {
            DrawRect(World2Window(new Vector2(mLayoutBounds.minX, mLayoutBounds.minY)), World2Window(new Vector2(mLayoutBounds.maxX, mLayoutBounds.maxY)), new Color(1, 0, 0, 0.5f));
        }

        void DrawGridState()
        {
            foreach (var p in mOccupiedCoordinates)
            {
                var worldPosMin = GridCoordinateToWorldPosition(p.Key);
                var worldPosMax = GridCoordinateToWorldPosition(p.Key + Vector2Int.one);
                var windowMin = World2Window(worldPosMin);
                var windowMax = World2Window(worldPosMax);
                int refCount = p.Value;
                DrawRect(windowMin, windowMax, refCount == 1 ? Color.blue : Color.magenta);
            }
        }

        void DrawDetailInfo()
        {
            if (mDetailInfo.node != null)
            {
                if (mDetailInfo.detailType.HasFlag(DetailInfoType.FullMethodList))
                {
                    DrawFullMethodList(mDetailInfo.node);
                }
                else if (mDetailInfo.detailType.HasFlag(DetailInfoType.FullPropertyList))
                {
                    DrawFullPropertyList(mDetailInfo.node);
                }

                if (mDetailInfo.detailType.HasFlag(DetailInfoType.ClassComment))
                {
                    var node = mDetailInfo.node;
                    DrawCommentAt(mDetailInfo.obj.comment, node.worldPosition + new Vector2(node.size.x + X_PADDING, node.size.y));
                }
                else if (mDetailInfo.detailType.HasFlag(DetailInfoType.MethodComment))
                {
                    var node = mDetailInfo.node;
                    DrawCommentAt(mDetailInfo.obj.comment, node.worldPosition + new Vector2(node.size.x * mDetailInfo.widthMultiplier + X_PADDING, node.size.y));
                }
                else if (mDetailInfo.detailType.HasFlag(DetailInfoType.PropertyComment))
                {
                    var node = mDetailInfo.node;
                    DrawCommentAt(mDetailInfo.obj.comment, node.worldPosition + new Vector2(node.size.x * mDetailInfo.widthMultiplier + X_PADDING, node.size.y));
                }
            }
        }

        protected override void OnMouseButtonPressed(int button, Vector2 mousePos)
        {
            if (mActiveGraph == null)
            {
                return;
            }

            if (button == 0)
            {
                PickNode(mousePos);
            }
            else if (button == 1)
            {
                PickNode(mousePos);

                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("重置视野"), false, ResetViewPosition);
                menu.ShowAsContext();

                SceneView.RepaintAll();
            }
        }

        protected override void OnMouseButtonReleased(int button)
        {
        }

        protected override void OnMouseDrag(int button, Vector2 movement)
        {
            if (mActiveGraph == null)
            {
                return;
            }

            if (button == 0 && movement != Vector2.zero)
            {
                if (mSelectionInfo.nodes.Count > 0)
                {
                    foreach (var node in mSelectionInfo.nodes)
                    {
                        ReleaseGrids(node);
                        node.Move(movement);
                        OccupyGrids(node);
                    }
                }
            }
        }

        void DrawNode(GraphNode node)
        {
            var pos = AlignToGrid(node.worldPosition);
            var size = node.size;
            //draw node
            Color color = Color.gray;
            if (mSelectionInfo.nodes.Contains(node))
            {
                color = mOutlineColor;
            }
            float expandWidth = 3;
            float expandHeight = 3;
            float outlineMinX = pos.x - expandWidth;
            float outlineMinY = pos.y - expandHeight;
            float outlineMaxX = outlineMinX + size.x + expandWidth * 2;
            float outlineMaxY = outlineMinY + size.y + expandHeight * 2;
            DrawRect(World2Window(new Vector2(outlineMinX, outlineMinY)), World2Window(new Vector2(outlineMaxX, outlineMaxY)), color);

            var nodeMinPos = World2Window(pos);
            var nodeMaxPos = World2Window(pos + size);

            Rect2DInt r = Rect2DInt.empty;
            r.Add((int)nodeMinPos.x, (int)nodeMinPos.y);
            r.Add((int)nodeMaxPos.x, (int)nodeMaxPos.y);
            //DrawLineRect(nodeMinPos, nodeMaxPos, Color.red);

            DrawNodeContent(node);
        }

        void DrawCommentAt(string text, Vector2 worldPosition)
        {
            var textSize = GUI.skin.textField.CalcSize(new GUIContent(text));
            var textPos = World2Window(worldPosition);
            Rect textRect = new Rect(textPos, new Vector2(textSize.x + X_PADDING, textSize.y + Y_PADDING));
            EditorGUI.TextArea(textRect, text);
        }

        void DrawTextAt(string name, float worldWidth, Vector2 worldPosition)
        {
            var textSize = GUI.skin.textField.CalcSize(new GUIContent(name));
            float offset = (worldWidth / mViewer.GetZoom() - textSize.x) / 2;
            if (offset < 0)
            {
                //文字长度大于node size,需要把字体变小
            }
            var textPos = World2Window(worldPosition);
            textPos.x += offset;
            Rect textRect = new Rect(textPos, textSize);
            //EditorGUI.LabelField(textRect, name);

            EditorGUI.TextField(textRect, name);
            var e = Event.current;
            if (e.type == EventType.MouseDown && textRect.Contains(e.mousePosition))
            {
                //int a = 1;
            }
        }

        void DrawButtonAt(string name, float worldWidth, Vector2 worldPosition, System.Action<int> onClickButton, bool alignCenter, Color color)
        {
            var textSize = GUI.skin.button.CalcSize(new GUIContent(name));
            float offset = (worldWidth / mViewer.GetZoom() - textSize.x) / 2;
            if (offset < 0)
            {
                //文字长度大于node size,需要把字体变小
            }
            var textPos = World2Window(worldPosition);
            if (alignCenter)
            {
                textPos.x += offset;
            }
            Rect textRect = new Rect(textPos, textSize);
            var style = GUI.skin.button;
            style.normal.background = mButtonBackground;
            var originalColor = style.normal.textColor;
            style.normal.textColor = color;
            if (GUI.Button(textRect, name, style))
            {
                if (onClickButton != null)
                {
                    onClickButton(Event.current.button);
                }
            }
            style.normal.textColor = originalColor;
            style.normal.background = null;
        }

        void DrawNodeContent(GraphNode node)
        {
            var data = Utility.GetData();
            string projectName = data.csProjectName;

            var originalSize = GUI.skin.button.fontSize;
            GUI.skin.button.fontSize = Mathf.CeilToInt(originalSize / mViewer.GetZoom());
            if (data.displayMode == DisplayMode.Icon)
            {
                //draw class name
#if false
                DrawButtonAt(node.typeInfo.nameInfo.modifiedName, node.size.x, node.worldPosition + new Vector2(0, node.size.y), () =>
                {
                    Utility.OpenCSFile(data.vsDTEFilePath, node.typeInfo.filePath, node.typeInfo.lineNumber, projectName);
                }, true, Utility.GetTypeColor(node.typeInfo));
                //DrawTextAt(node.typeInfo.nameInfo.modifiedName, node.size.x, node.worldPosition + new Vector2(0, node.size.y));
#else
                var pos = World2Window(node.worldPosition + new Vector2(0, node.size.y));

                //wzw temp
#if false
                CustomButton.Draw(node.typeInfo.nameInfo.modifiedName, pos, (int button) => {
                    if (button == 0)
                    {
                        Utility.OpenCSFile(data.vsDTEFilePath, node.typeInfo.filePath, node.typeInfo.lineNumber, projectName);
                    }
                    else if (button == 2)
                    {
                        mDetailInfo.node = node;
                        mDetailInfo.detailType |= DetailInfoType.ClassComment;
                        mDetailInfo.obj = mDetailInfo.node.typeInfo;
                    }
                }, Mathf.FloorToInt(12 / mViewer.GetZoom()), node.size.x / mViewer.GetZoom(), Utility.GetTypeColor(node.typeInfo));
#endif
#endif
            }
            else
            {
                //draw class name
                DrawButtonAt(node.typeInfo.nameInfo.modifiedName, node.size.x, node.worldPosition + new Vector2(0, node.size.y), (int button) =>
                {
                    if (button == 0)
                    {
                        Utility.OpenCSFile(data.vsDTEFilePath, node.typeInfo.filePath, node.typeInfo.lineNumber, projectName);
                    }
                    else if (button == 2)
                    {
                        mDetailInfo.node = node;
                        mDetailInfo.detailType |= DetailInfoType.ClassComment;
                        mDetailInfo.obj = mDetailInfo.node.typeInfo;
                    }
                }, true, Utility.GetTypeColor(node.typeInfo));

                //draw methods
                var methods = node.typeInfo.methods;
                int methodCount = Mathf.Min(methods.Count, MAX_DISPLAY_ITEM_COUNT);
                for (int i = 0; i < methodCount; ++i)
                {
                    DrawButtonAt(methods[i].modifiedName, node.size.x, node.worldPosition + new Vector2(0, node.size.y - i * node.oneItemDisplayHeight - node.methodListPositionOffset), (int button) =>
                    {
                        if (button == 0)
                        {
                            Utility.OpenCSFile(data.vsDTEFilePath, node.typeInfo.filePath, methods[i].lineNumber, projectName);
                        }
                        else if (button == 2)
                        {
                            mDetailInfo.obj = methods[i];
                            mDetailInfo.detailType |= DetailInfoType.MethodComment;
                            mDetailInfo.node = node;
                            mDetailInfo.widthMultiplier = 1.0f;
                        }
                    }, false, Utility.METHOD_COLOR);
                }

                //draw properties
                var properties = node.typeInfo.properties;
                int propertyCount = Mathf.Min(properties.Count, MAX_DISPLAY_ITEM_COUNT);
                for (int i = 0; i < propertyCount; ++i)
                {
                    DrawButtonAt(properties[i].fullName, node.size.x, node.worldPosition + new Vector2(0, node.size.y - i * node.oneItemDisplayHeight - node.propertyListPositionOffset), (int button) =>
                    {
                        if (button == 0)
                        {
                            Utility.OpenCSFile(data.vsDTEFilePath, node.typeInfo.filePath, properties[i].lineNumber, projectName);
                        }
                        else if (button == 2)
                        {
                            mDetailInfo.obj = properties[i];
                            mDetailInfo.detailType |= DetailInfoType.PropertyComment;
                            mDetailInfo.node = node;
                            mDetailInfo.widthMultiplier = 1.0f;
                        }
                    }, false, Utility.PROPERTY_COLOR);
                }

                if (node.typeInfo.methods.Count > 0)
                {
                    var methodButtonMinPos = World2Window(new Vector2(node.size.x - DETAIL_BUTTON_WIDTH, node.size.y - NAME_LABEL_HEIGHT) + node.worldPosition);
                    if (GUI.Button(new Rect(methodButtonMinPos.x, methodButtonMinPos.y, DETAIL_BUTTON_WIDTH / mViewer.GetZoom(), DETAIL_BUTTON_HEIGHT / mViewer.GetZoom()), "?"))
                    {
                        mDetailInfo.detailType |= DetailInfoType.FullMethodList;
                        mDetailInfo.node = node;
                    }
                }

                if (node.typeInfo.properties.Count > 0)
                {
                    var propertyButtonMinPos = World2Window(new Vector2(node.size.x - DETAIL_BUTTON_WIDTH, node.size.y - node.propertyListPositionOffset) + node.worldPosition);
                    if (GUI.Button(new Rect(propertyButtonMinPos.x, propertyButtonMinPos.y, DETAIL_BUTTON_WIDTH / mViewer.GetZoom(), DETAIL_BUTTON_HEIGHT / mViewer.GetZoom()), "?"))
                    {
                        mDetailInfo.detailType |= DetailInfoType.FullPropertyList;
                        mDetailInfo.node = node;
                    }
                }

                var pos = World2Window(node.worldPosition + new Vector2(0, node.size.y - node.methodListPositionOffset));
                DrawHorizontalLine(pos.x, pos.y, node.size.x / mViewer.GetZoom(), Color.yellow);

                pos = World2Window(node.worldPosition + new Vector2(0, node.size.y - node.propertyListPositionOffset));
                DrawHorizontalLine(pos.x, pos.y, node.size.x / mViewer.GetZoom(), Color.red);
            }
            GUI.skin.button.fontSize = originalSize;
        }

        void DrawFullMethodList(GraphNode node)
        {
            //draw methods
            var data = Utility.GetData();
            string projectName = data.csProjectName;
            var methods = node.typeInfo.methods;
            Vector2 offset = new Vector2(node.size.x + 20, 0);
            for (int i = 0; i < methods.Count; ++i)
            {
                DrawButtonAt(methods[i].modifiedFullName, node.size.x, node.worldPosition + new Vector2(0, node.size.y - i * node.oneItemDisplayHeight - node.methodListPositionOffset) + offset, (int button) => {
                    if (button == 0)
                    {
                        Utility.OpenCSFile(data.vsDTEFilePath, node.typeInfo.filePath, methods[i].lineNumber, projectName);
                    }
                    else if (button == 2)
                    {
                        mDetailInfo.obj = methods[i];
                        mDetailInfo.detailType |= DetailInfoType.MethodComment;
                        mDetailInfo.node = node;
                        mDetailInfo.widthMultiplier = 2.0f;
                    }
                }, false, Utility.METHOD_COLOR);
            }
        }

        void DrawFullPropertyList(GraphNode node)
        {
            //draw properties
            var data = Utility.GetData();
            string projectName = data.csProjectName;
            var properties = node.typeInfo.properties;
            Vector2 offset = new Vector2(node.size.x + 20, 0);
            for (int i = 0; i < properties.Count; ++i)
            {
                DrawButtonAt(properties[i].fullName, node.size.x, node.worldPosition + new Vector2(0, node.size.y - i * node.oneItemDisplayHeight - node.propertyListPositionOffset) + offset, (int button) => {
                    if (button == 0)
                    {
                        Utility.OpenCSFile(data.vsDTEFilePath, node.typeInfo.filePath, properties[i].lineNumber, projectName);
                    }
                    else if (button == 2)
                    {
                        mDetailInfo.obj = properties[i];
                        mDetailInfo.detailType |= DetailInfoType.PropertyComment;
                        mDetailInfo.node = node;
                        mDetailInfo.widthMultiplier = 2.0f;
                    }
                }, false, Utility.PROPERTY_COLOR);
            }
        }

        void DrawHorizontalLine(float x, float y, float width, Color color)
        {
            EditorGUI.DrawRect(new Rect(x, y, width, 1), color);
        }

        void DrawNodeLines()
        {
            mLinePairs.Clear();

            var groups = mActiveGraph.groups;
            foreach (var group in groups)
            {
                AddNodeLinesInGroup(group);
            }

            float alpha = 0.35f;
            Color red = new Color(1, 0, 0, alpha);
            Color yellow = new Color(1, 1, 0, alpha);
            Color white = new Color(1, 1, 1, alpha);
            foreach (var pair in mLinePairs)
            {
                var nodeA = pair.a;
                var nodeB = pair.b;
                var nodeAPos = World2Window(AlignToGrid(nodeA.worldPosition) + nodeA.size * 0.5f);
                var nodeBPos = World2Window(AlignToGrid(nodeB.worldPosition) + nodeB.size * 0.5f);

                Handles.color = white;
                DrawMark(nodeBPos, nodeAPos);

                Handles.DrawLine(nodeAPos, nodeBPos);
            }
        }

        void DrawMark(Vector2 fromPos, Vector2 toPos)
        {
            var center = (fromPos + toPos) * 0.5f;
            var dir = toPos - fromPos;
            dir.Normalize();
            var side = new Vector2(dir.y, -dir.x);
            var v0 = center + side * MARK_LENGTH * 0.5f;
            var v1 = center - side * MARK_LENGTH * 0.5f;
            var v2 = center + MARK_LENGTH * dir;
            Handles.DrawLine(v0, v1);
            Handles.DrawLine(v1, v2);
            Handles.DrawLine(v2, v0);
        }

        void AddLine(TypeInfo baseType)
        {
            foreach (var derivedType in baseType.derivedTypes)
            {
                bool found = false;
                for (int i = 0; i < mLinePairs.Count; ++i)
                {
                    if (mLinePairs[i].a.typeInfo == baseType &&
                        mLinePairs[i].b.typeInfo == derivedType)
                    {
                        found = true;
                        break;
                    }
                }
                if (found == false)
                {
                    var nodeA = mActiveGraph.GetNode(baseType);
                    var nodeB = mActiveGraph.GetNode(derivedType);
                    if (nodeA != null && nodeB != null)
                    {
                        mLinePairs.Add(new LinePair(nodeA, nodeB));
                    }
                }
            }

            foreach (var derivedType in baseType.derivedTypes)
            {
                AddLine(derivedType);
            }
        }

        void AddNodeLinesInGroup(GroupInfo group)
        {
            foreach (var type in group.rootTypes)
            {
                AddLine(type);
            }
        }

        void PickNode(Vector2 mousePosInViewportSpace)
        {
            PickNodeOnly(mousePosInViewportSpace, mSelectionInfo, true);
        }

        void PickNodeOnly(Vector2 mousePosInViewportSpace, SelectionInfo info, bool singleSelection)
        {
            info.nodes.Clear();
            var mouseWorldPos = Window2World(mousePosInViewportSpace);
            mDetailInfo.node = null;
            mDetailInfo.detailType = DetailInfoType.None;

            foreach (var node in mActiveGraph.nodes)
            {
                var pos = AlignToGrid(node.worldPosition);
                if (mouseWorldPos.x >= pos.x && mouseWorldPos.x < pos.x + node.size.x &&
                    mouseWorldPos.y >= pos.y && mouseWorldPos.y < pos.y + node.size.y)
                {
                    info.nodes.Add(node);
                    if (singleSelection)
                    {
                        break;
                    }
                }
            }
        }

        public void ResetViewPosition()
        {
            mViewer.ResetPosition();
        }

        void OccupyGrids(GraphNode node)
        {
            var size = node.size;
            var minCoord = WorldPositionToGridCoordinateCeil(node.worldPosition - size * 0.5f);
            var maxCoord = WorldPositionToGridCoordinateCeil(node.worldPosition + size * 0.5f);
            OccupyGrids(minCoord, maxCoord);
        }

        void ReleaseGrids(GraphNode node)
        {
            var size = node.size;
            var minCoord = WorldPositionToGridCoordinateCeil(node.worldPosition - size * 0.5f);
            var maxCoord = WorldPositionToGridCoordinateCeil(node.worldPosition + size * 0.5f);
            ReleaseGrids(minCoord, maxCoord);
        }

        Color mOutlineColor = new Color(0, 1, 0, 1);
        Color mBackgroundColor = new Color(1, 1, 1, 1);
        Color mInitNodeColor = new Color(1, 0, 0, 1);

        SelectionInfo mSelectionInfo = new SelectionInfo();
        DetailDisplayInfo mDetailInfo = new DetailDisplayInfo();

        List<LinePair> mLinePairs = new List<LinePair>();
        Graph mActiveGraph;

        bool mNeedLayout = false;
        Texture2D mButtonBackground;

        const float MARK_LENGTH = 10;
    };
}


