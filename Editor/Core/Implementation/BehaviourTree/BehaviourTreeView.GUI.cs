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

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;
using XDay.WorldAPI;

namespace XDay.BehaviourTreeAPI.Editor
{
    internal partial class BehaviourTreeView
    {
        protected override void OnDrawGUI()
        {
            DrawLeft();

            DrawSplitter();

            DrawRight();

            if (Event.current.type == EventType.Repaint)
            {
                if (Application.isPlaying && Global.RuntimeBehaviourTreeManager != null)
                {
                    var trees = Global.RuntimeBehaviourTreeManager.Trees.Values;
                    if (m_Tree == null && trees.Count > 0)
                    {
                        SetTree(GetTree(trees, 0));
                    }
                }
            }

            if (m_ReplaceNode)
            {
                m_ReplaceNode = false;
                var node = GetSelectedNodeView();
                if (node == null)
                {
                    return;
                }

                CreateNodeMenu(node.WorldPosition, ActionType.ReplaceNode, null);
            }

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
            {
                var node = GetSelectedNodeView();
                if (node != null)
                {
                    DeleteNode(true);
                    e.Use();
                }
            }
        }

        private void DrawLeft()
        {
            m_ViewportArea = new Rect(0, 0, m_SplitterPosition * m_WindowContentWidth, m_WindowContentHeight);
            GUILayout.BeginArea(m_ViewportArea, EditorStyles.helpBox);

            DrawGrid();

            if (m_TextFieldStyle == null)
            {
                m_TextFieldStyle = new(GUI.skin.textField)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (m_CommentFieldStyle == null)
            {
                m_CommentFieldStyle = new(GUI.skin.textArea)
                {
                    alignment = TextAnchor.LowerLeft
                };
            }

            if (m_TextAreaStyle == null)
            {
                m_TextAreaStyle = new GUIStyle(EditorStyles.textArea);
                m_TextAreaStyle.normal.background = m_ButtonBackground;
                m_TextAreaStyle.active.background = m_ButtonBackground;
                m_TextAreaStyle.focused.background = m_ButtonBackground;
            }

            Handles.BeginGUI();

            var oldColor = Handles.color;
            Handles.color = Color.white;
            foreach (var node in m_AllNodes)
            {
                DrawNodeConnection(node);
            }

            DrawDragLine();

            Handles.color = oldColor;

            //draw nodes
            foreach (var node in m_AllNodes)
            {
                DrawNode(node);
            }

            DrawNodeEffects();

            DrawDescription();

            //DrawGridState();

            //DrawBounds();

            DrawDebug();

            Handles.EndGUI();

            GUILayout.EndArea();
        }

        private void DrawRight()
        {
            GUILayout.BeginArea(new Rect(m_SplitterPosition * m_WindowContentWidth + 5, 0, m_WindowContentWidth - m_SplitterPosition * m_WindowContentWidth - 5, m_WindowContentHeight), EditorStyles.helpBox);

            DrawTreeSelection();

            GUI.enabled = !Application.isPlaying;
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("New"))
            {
                if (m_Tree != null)
                {
                    if (EditorUtility.DisplayDialog("注意", "当前有BehaviourTree正在编辑,确定创建新的?", "确定", "取消"))
                    {
                        Save();
                        Create(ScriptableObject.CreateInstance<BehaviourTreeState>(), "New");
                    }
                }
                else
                {
                    Create(ScriptableObject.CreateInstance<BehaviourTreeState>(), "New");
                }
            }

            if (GUILayout.Button("Load"))
            {
                Load();
            }

            if (GUILayout.Button("Save"))
            {
                Save();
            }

            if (GUILayout.Button("Reset"))
            {
                Reset();
            }
            EditorGUILayout.EndHorizontal();

            m_ShowSiblingIndex = EditorGUILayout.Toggle("Show Sibling Index", m_ShowSiblingIndex);

            GUI.enabled = true;

            DrawToolbar();

            GUILayout.EndArea();
        }

        private void DrawToolbar()
        {
            GUI.enabled = m_Tree != null;
            m_SelectedPage = (PageType)GUILayout.Toolbar((int)m_SelectedPage, m_ToolbarNames);
            switch (m_SelectedPage)
            {
                case PageType.Node:
                    DrawNodePage();
                    break;
                case PageType.Variable:
                    DrawVariablePage();
                    break;
                default:
                    Debug.Assert(false, "todo");
                    break;
            }
            GUI.enabled = true;
        }

        private void DrawSplitter()
        {
            var splitterRect = new Rect(m_SplitterPosition * m_WindowContentWidth, 0, 5, m_WindowContentHeight);
            EditorGUI.DrawRect(splitterRect, Color.gray);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            // Dragging logic for the splitter
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                m_IsDragging = true;
            }
            if (m_IsDragging && Event.current.type == EventType.MouseDrag)
            {
                m_SplitterPosition += Event.current.delta.x / m_WindowContentWidth;
                Repaint();
            }
            if (Event.current.type == EventType.MouseUp) m_IsDragging = false;
        }

        private void DrawBounds()
        {
            //DrawRect(World2Window(new Vector2(mLayoutBounds.minX, mLayoutBounds.minY)), World2Window(new Vector2(mLayoutBounds.maxX, mLayoutBounds.maxY)), new Color(1, 0, 0, 0.5f));
        }

        private void DrawGridState()
        {
            foreach (var p in m_OccupiedCoordinates)
            {
                var worldPosMin = GridCoordinateToWorldPosition(p.Key);
                var worldPosMax = GridCoordinateToWorldPosition(p.Key + Vector2Int.one);
                var windowMin = World2Window(worldPosMin);
                var windowMax = World2Window(worldPosMax);
                int refCount = p.Value;
                DrawRect(windowMin, windowMax, refCount == 1 ? Color.blue : Color.magenta);
            }
        }

        protected override void OnMouseButtonPressed(int button, Vector2 mousePos)
        {
            if (m_Tree == null)
            {
                return;
            }
            if (button == 0)
            {
                if (!m_ViewportArea.Contains(mousePos))
                {
                    return;
                }
                m_ValidClick = true;
                m_DrawDragLine = true;

                TimeSpan timeSinceLastClick = DateTime.Now - lastClickTime;
                Vector2 deltaPosition = mousePos - lastClickPosition;
                // 判断是否满足双击条件
                if (timeSinceLastClick.TotalSeconds < DoubleClickTime &&
                    deltaPosition.magnitude < PositionTolerance)
                {
                    OnDoubleClick();
                    Event.current.Use(); // 标记事件已处理
                }
                else
                {
                    PickNode(mousePos);
                }

                // 记录当前点击信息
                lastClickTime = DateTime.Now;
                lastClickPosition = mousePos;
            }
            else if (button == 1)
            {
                PickNode(mousePos);

                GenericMenu menu = new();
                if (m_SelectionInfo.Nodes.Count > 0)
                {
                    menu.AddItem(new GUIContent("复制"), false, () => { CloneNode(false); });
                    menu.AddItem(new GUIContent("复制一个"), false, () => { CloneNode(true); });
                    menu.AddItem(new GUIContent("替换"), false, ReplaceNode);
                    menu.AddItem(new GUIContent("设置为根节点"), false, SetToRoot);
                    menu.AddItem(new GUIContent("删除"), false, () => { DeleteNode(true); });
                    menu.AddItem(new GUIContent("删除一个"), false, () => { DeleteOneNode(true); });
                    menu.AddItem(new GUIContent("打开脚本"), false, OpenCSFile);
                    menu.AddItem(new GUIContent("断点"), false, SetBreakpoint);
                }
                AddNodeMenuItems(menu, mousePos, ActionType.CreateChildNode, null);
                menu.AddItem(new GUIContent("重置视野"), false, ResetViewPosition);

                menu.ShowAsContext();

                SceneView.RepaintAll();
            }
        }

        protected override void OnMouseButtonReleased(int button, Vector2 mousePos)
        {
            if (m_Tree == null)
            {
                return;
            }

            if (button == 0)
            {
                m_ValidClick = false;
                m_DrawDragLine = false;
                Repaint();
                var selectionInfo = new SelectionInfo();
                PickNodeOnly(mousePos, selectionInfo, true);
                if (selectionInfo.Nodes.Count > 0)
                {
                    if (m_SelectionInfo.Part == Part.Top &&
                        selectionInfo.Part == Part.Bottom)
                    {
                        var childNode = m_SelectionInfo.Nodes[0].Node;
                        var parentNode = selectionInfo.Nodes[0].Node;
                        if (childNode != parentNode)
                        {
                            childNode.SetParent(parentNode as CompoundNode);
                            ReorderSibling(m_SelectionInfo.Nodes[0]);
                        }
                    }
                    else if (m_SelectionInfo.Part == Part.Bottom &&
                        selectionInfo.Part == Part.Top)
                    {
                        var parentNode = m_SelectionInfo.Nodes[0].Node;
                        var childNode = selectionInfo.Nodes[0].Node;
                        if (childNode != parentNode)
                        {
                            childNode.SetParent(parentNode as CompoundNode);
                            ReorderSibling(selectionInfo.Nodes[0]);
                        }
                    }
                }
                else
                {
                    var nodeView = GetSelectedNodeView();
                    if (nodeView != null && m_ShowDragMenu)
                    {
                        CreateNodeMenu(mousePos,
                            m_SelectionInfo.Part == Part.Bottom ? ActionType.CreateChildNode : ActionType.CreateParentNode, m_SelectionInfo.Part == Part.Bottom ? nodeView.Node as CompoundNode : null);
                    }
                }

                m_ShowDragMenu = false;
            }
        }

        protected override void OnMouseDrag(int button, Vector2 movement)
        {
            if (m_Tree == null)
            {
                return;
            }

            if (button == 0 && movement != Vector2.zero)
            {
                var e = Event.current;
                if (m_ValidClick)
                {
                    if (m_SelectionInfo.Nodes.Count > 0)
                    {
                        if (m_SelectionInfo.Part == Part.Center)
                        {
                            GetTopMostNodes(m_SelectionInfo.Nodes, m_TempList);
                            foreach (var node in m_TempList)
                            {
                                MoveNode(node, movement, true, e.control);
                            }
                        }
                    }
                }
            }
        }

        private void DrawDragLine()
        {
            if (m_DrawDragLine)
            {
                var e = Event.current;
                if (m_SelectionInfo.Part == Part.Top)
                {
                    var start = GetSelectedNodeView().GetTopCenter();
                    var end = e.mousePosition;
                    Handles.DrawAAPolyLine(5, World2Window(start), end);
                }
                else if (m_SelectionInfo.Part == Part.Bottom)
                {
                    var start = GetSelectedNodeView().GetBottomCenter();
                    var end = e.mousePosition;
                    Handles.DrawAAPolyLine(5, World2Window(start), end);
                }
            }
        }

        private void DrawNodeConnection(NodeView nodeView)
        {
            var node = nodeView.Node;
            var parent = node.Parent;
            if (parent != null)
            {
                var parentView = GetNodeView(parent);
                var topCenter = nodeView.GetTopCenter();
                var bottomCenter = parentView.GetBottomCenter();
                Handles.DrawAAPolyLine(5, World2Window(topCenter), World2Window(bottomCenter));
            }
        }

        private void MoveNode(NodeView nodeView, Vector2 movement, bool changeSiblingIndex, bool moveTopNodeOnly)
        {
            ReleaseGrids(nodeView);
            nodeView.Move(movement);
            OccupyGrids(nodeView);

            if (changeSiblingIndex)
            {
                ReorderSibling(nodeView);
            }

            if (!moveTopNodeOnly)
            {
                if (nodeView.Node is CompoundNode compoundNode)
                {
                    foreach (var child in compoundNode.Children)
                    {
                        var childNodeView = GetNodeView(child);
                        MoveNode(childNodeView, movement, false, moveTopNodeOnly);
                    }
                }
            }
        }

        private void ReorderSibling(NodeView nodeView)
        {
            var node = nodeView.Node;
            var parent = node.Parent;
            if (parent == null)
            {
                return;
            }
            parent.Children.Sort(
                (a, b) =>
                {
                    var va = GetNodeView(a);
                    var vb = GetNodeView(b);
                    return va.WorldPosition.x.CompareTo(vb.WorldPosition.x);
                });
        }

        private void DrawNode(NodeView nodeView)
        {
            //draw nodeView
            DrawIcon(nodeView);

            var node = nodeView.Node;

            DrawTop(nodeView);

            if (node is CompoundNode)
            {
                DrawBottom(nodeView);
            }

            node.Name = DrawTextAt(nodeView.Node.Name, nodeView.Size.x, nodeView.WorldPosition + new Vector2(0, nodeView.Size.y));

            if (nodeView.ShowComment)
            {
                var siblingIndex = node.GetSiblingIndex();
                bool left = false;
                Vector2 position = nodeView.WorldPosition + nodeView.Size;
                if (siblingIndex == 0)
                {
                    left = true;
                    position = nodeView.WorldPosition + new Vector2(-m_MaxCommentAreaWidth, nodeView.Size.y);
                }
                node.Comment = DrawCommentAt(node.Comment, nodeView.WorldPosition, nodeView.Size, left);
            }
        }

        private string DrawCommentAt(string text, Vector2 worldPosition, Vector2 size, bool left)
        {
            var textSize = GUI.skin.textField.CalcSize(new GUIContent(text));
            if (textSize.x > m_MaxCommentAreaWidth)
            {
                textSize.x = m_MaxCommentAreaWidth;
            }

            float areaWidth = textSize.x + m_XPadding;
            float areaHeight = textSize.y + m_YPadding;

            worldPosition.y += size.y;
            if (left)
            {
                worldPosition.x -= areaWidth;
            }
            else
            {
                worldPosition.x += size.x;
            }
            var textPos = World2Window(worldPosition);

            Rect textRect = new(textPos, new Vector2(WorldLengthToWindowLength(areaWidth), WorldLengthToWindowLength(areaHeight)));
            return EditorGUI.TextArea(textRect, text);
        }

        private void DrawDescription()
        {
            var nodeView = GetSelectedNodeView();
            if (nodeView == null)
            {
                return;
            }
            var attribute = Helper.GetClassAttribute<DescriptionAttribute>(nodeView.Node.GetType());
            if (attribute != null && !string.IsNullOrEmpty(attribute.Description))
            {
                GUI.enabled = false;
                EditorGUI.TextArea(new Rect(0, m_WindowContentHeight - m_DescriptionHeight, m_DescriptionWidth, m_DescriptionHeight), attribute.Description, m_TextAreaStyle);
                GUI.enabled = true;
            }
        }

        private string DrawTextAt(string name, float worldWidth, Vector2 worldPosition)
        {
            var minZoom = m_Viewer.GetMinZoom();
            var maxZoom = m_Viewer.GetMaxZoom();
            float t = (m_Viewer.GetZoom() - minZoom) / (maxZoom - minZoom);
            m_TextFieldStyle.fontSize = (int)(Mathf.Lerp(m_MinFontSize, m_MaxFontSize, Helper.EaseInExpo(1 - t)));
            //文字长度大于node size,需要把字体变小
            var textPos = World2Window(worldPosition);
            Rect textRect = new(textPos, new Vector2(WorldLengthToWindowLength(worldWidth), WorldLengthToWindowLength(m_NameHeight)));
            name = EditorGUI.TextField(textRect, name, m_TextFieldStyle);
            return name;
        }

        private void DrawButtonAt(string name, float worldWidth, Vector2 worldPosition, System.Action<int> onClickButton, bool alignCenter, Color color)
        {
            var textSize = GUI.skin.button.CalcSize(new GUIContent(name));
            float offset = (worldWidth / m_Viewer.GetZoom() - textSize.x) / 2;
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
            style.normal.background = m_ButtonBackground;
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

        private void DrawIcon(NodeView nodeView)
        {
            var pos = nodeView.WorldPosition;
            var size = nodeView.Size;
            Color color = m_IconBackgroundColor;
            if (m_SelectionInfo.Nodes.Contains(nodeView))
            {
                color = m_IconSelectColor;
            }
            if (m_Tree.Root == nodeView.Node)
            {
                color = m_RootNodeColor;
            }
            float expandWidth = 0;
            float expandHeight = 0;
            float outlineMinX = pos.x - expandWidth;
            float outlineMinY = pos.y - expandHeight;
            float outlineMaxX = outlineMinX + size.x + expandWidth * 2;
            float outlineMaxY = outlineMinY + size.y + expandHeight * 2;
            DrawRect(World2Window(new Vector2(outlineMinX, outlineMinY)), World2Window(new Vector2(outlineMaxX, outlineMaxY)), color);

            float iconMinX = pos.x + 5;
            float iconMinY = pos.y + 5;
            float iconMaxX = iconMinX + size.x - 10;
            float iconMaxY = iconMinY + size.y - m_NameHeight - 10;

            var min = World2Window(new Vector2(iconMinX, iconMinY));
            var max = World2Window(new Vector2(iconMaxX, iconMaxY));

            //DrawRect(World2Window(new Vector2(iconMinX, iconMinY)), World2Window(new Vector2(iconMaxX, iconMaxY)), Color.green);
            DrawTexture(min, max, nodeView.Icon);

            DrawProgress(nodeView);

            DrawStatus(nodeView);

            //draw sibling index
            if (m_ShowSiblingIndex)
            {
                var siblingIndex = nodeView.Node.GetSiblingIndex();
                DrawTextAt($"{siblingIndex}", (iconMaxX - iconMinX) / 2, new Vector2((iconMinX + iconMaxX) * 0.5f, (iconMinY + iconMaxY) * 0.5f));
            }
        }

        private void DrawProgress(NodeView nodeView)
        {
            if (Application.isPlaying && nodeView.Node.Started)
            {
                var pos = nodeView.WorldPosition;
                var size = nodeView.Size;
                float iconMinX = pos.x;
                float iconMinY = pos.y;
                float iconMaxX = pos.x + size.x - m_StatusIconSize;
                float iconMaxY = iconMinY + m_ProgressBarHeight;

                var min = World2Window(new Vector2(iconMinX, iconMinY));
                var max = World2Window(new Vector2(iconMaxX, iconMaxY));

                var node = nodeView.Node;
                var progress = node.Progress;
                if (Helper.GE(progress, 0))
                {
                    DrawRect(min, max, Color.black);
                    DrawRect(min, new Vector2(min.x + (max.x - min.x) * progress, max.y), Color.green);
                }
            }
        }

        private void DrawTop(NodeView node)
        {
            Color color = m_TopConnectorColor;
            if (m_SelectionInfo.Nodes.Contains(node) && m_SelectionInfo.Part == Part.Top)
            {
                color = m_TopConnectorSelectColor;
            }
            node.GetTopRect(out var min, out var max);
            DrawRect(World2Window(min), World2Window(max), color);
        }

        private void DrawBottom(NodeView node)
        {
            Color color = m_BottomConnectorColor;
            if (m_SelectionInfo.Nodes.Contains(node) && m_SelectionInfo.Part == Part.Bottom)
            {
                color = m_BottomConnectorSelectColor;
            }
            node.GetBottomRect(out var min, out var max);
            DrawRect(World2Window(min), World2Window(max), color);
        }

        private void DrawHorizontalLine(float x, float y, float width, Color color)
        {
            EditorGUI.DrawRect(new Rect(x, y, width, 1), color);
        }

        private void PickNode(Vector2 mousePosInViewportSpace)
        {
            PickNodeOnly(mousePosInViewportSpace, m_SelectionInfo, true);
            if (m_SelectionInfo.Part == Part.Bottom ||
                m_SelectionInfo.Part == Part.Top)
            {
                m_ShowDragMenu = true;
            }
        }

        private void PickNodeOnly(Vector2 mousePosInViewportSpace, SelectionInfo info, bool singleSelection)
        {
            if (!m_ViewportArea.Contains(mousePosInViewportSpace))
            {
                return;
            }

            info.Nodes.Clear();
            info.Part = Part.None;
            var mouseWorldPos = Window2World(mousePosInViewportSpace);

            for (var i = m_AllNodes.Count - 1; i >= 0; --i)
            {
                var node = m_AllNodes[i];
                var part = node.HitTest(mouseWorldPos);
                if (part != Part.None)
                {
                    info.Nodes.Add(node);
                    info.Part = part;
                    if (singleSelection)
                    {
                        break;
                    }
                }
            }
        }

        public void ResetViewPosition()
        {
            m_Viewer.ResetPosition();
        }

        private void SetBreakpoint()
        {
            var nodeView = GetSelectedNodeView();
            if (nodeView == null)
            {
                return;
            }

            nodeView.Node.HasBreakpoint = !nodeView.Node.HasBreakpoint;
        }

        private void OpenCSFile()
        {
            var nodeView = GetSelectedNodeView();
            if (nodeView == null)
            {
                return;
            }

            var path = EditorHelper.QueryScriptFilePath("BehaviourTree", nodeView.Node.GetType().Name);
            EditorHelper.OpenCSFile(path, 1);
        }

        private void ReplaceNode()
        {
            m_ReplaceNode = true;
        }

        private void CloneNode(bool cloneOne)
        {
            var node = GetSelectedNodeView();
            if (node != null)
            {
                var newNode = node.Node.Clone(cloneOne);
                var nodeView = GetNodeView(newNode);
                nodeView.WorldPosition = node.WorldPosition + new Vector2(20, 20);
            }
        }

        private void DeleteNode(bool prompt)
        {
            if (prompt && !EditorUtility.DisplayDialog("注意", "确定删除选中的树?", "确定", "取消"))
            {
                return;
            }

            var node = GetSelectedNodeView();
            if (node != null)
            {
                m_Tree.DestroyNode(node.Node, false);
            }
        }

        private void DeleteOneNode(bool prompt)
        {
            if (prompt && !EditorUtility.DisplayDialog("注意", "确定删除选中节点?", "确定", "取消"))
            {
                return;
            }

            var node = GetSelectedNodeView();
            if (node != null)
            {
                m_Tree.DestroyNode(node.Node, true);
            }
        }

        private void SetToRoot()
        {
            if (m_SelectionInfo.Nodes.Count == 0)
            {
                return;
            }

            m_Tree.SetRoot(m_SelectionInfo.Nodes[0].Node);
        }

        private void OccupyGrids(NodeView node)
        {
            var size = node.Size;
            var minCoord = WorldPositionToGridCoordinateCeil(node.WorldPosition - size * 0.5f);
            var maxCoord = WorldPositionToGridCoordinateCeil(node.WorldPosition + size * 0.5f);
            OccupyGrids(minCoord, maxCoord);
        }

        private void ReleaseGrids(NodeView node)
        {
            var size = node.Size;
            var minCoord = WorldPositionToGridCoordinateCeil(node.WorldPosition - size * 0.5f);
            var maxCoord = WorldPositionToGridCoordinateCeil(node.WorldPosition + size * 0.5f);
            ReleaseGrids(minCoord, maxCoord);
        }

        private void AddNodeMenuItem(Type type, string group, GenericMenu menu, Vector2 mousePos, ActionType action, CompoundNode parent)
        {
            menu.AddItem(new GUIContent($"{group}/{type.Name}"), false, () =>
            {
                m_CreateWindowPosition = mousePos;

                List<Node> children = null;
                bool isRoot = false;
                var selectedNode = GetSelectedNodeView();
                if (action == ActionType.ReplaceNode)
                {
                    //delete old node
                    isRoot = selectedNode.Node == m_Tree.Root;
                    m_CreateWindowPosition = World2Window(selectedNode.WorldPosition);
                    if (selectedNode.Node is CompoundNode compound)
                    {
                        children = new(compound.Children);
                        parent = compound.Parent;
                    }
                    DeleteOneNode(false);
                }

                var newNode = m_Tree.CreateNode(type, type.Name, null);
                if (isRoot)
                {
                    m_Tree.SetRoot(newNode);
                }
                if (parent != null)
                {
                    newNode.SetParent(parent);
                }
                if (action == ActionType.CreateParentNode &&
                    newNode is CompoundNode compoundParent)
                {
                    selectedNode.Node.SetParent(compoundParent);
                }
                if (children != null && newNode is CompoundNode comp)
                {
                    foreach (var child in children)
                    {
                        child.SetParent(comp);
                    }
                }
            });
        }

        private void DrawDebug()
        {
            if (m_Tree != null)
            {
                EditorGUILayout.LabelField($"Node数量{m_Tree.Nodes.Count}");
                EditorGUILayout.LabelField($"Zoom {m_Viewer.GetZoom()}");
            }
        }

        private void DrawNodePage()
        {
            var nodeView = GetSelectedNodeView();
            if (nodeView == null)
            {
                return;
            }
            var node = nodeView.Node;
            GUI.enabled = false;
            EditorGUILayout.IntField("ID", node.ID);
            GUI.enabled = true;

            GUI.enabled = !Application.isPlaying;
            node.Name = EditorGUILayout.TextField("Name", node.Name);
            node.SortOrder = EditorGUILayout.IntField("Sort Order", node.SortOrder);
            node.Comment = EditorGUILayout.TextArea(node.Comment, m_CommentFieldStyle);
            if (node is DynamicNode dyn)
            {
                dyn.Dynamic = EditorGUILayout.Toggle("Dynamic", dyn.Dynamic);
            }
            GUI.enabled = false;
            EditorGUILayout.Toggle("Started", node.Started);
            EditorGUILayout.TextField("Status", node.Status.ToString());
            GUI.enabled = true;

            EditorHelper.HorizontalLine();

            Global.GlobalVariableManager.UpdateGroupNames();
            m_Tree.VariableManager.UpdateGroupNames();

            DrawProperties(node);
            GUI.enabled = true;
        }

        private void DrawVariablePage()
        {
            if (m_Tree != null)
            {
                DrawVariables("Local", m_Tree.VariableManager);
                DrawVariables("Global", Global.GlobalVariableManager);
            }
        }

        private void DrawVariables(string name, VariableManager variableManager)
        {
            EditorGUILayout.BeginHorizontal();
            variableManager.Draw = EditorHelper.Foldout(variableManager.Draw);
            EditorGUILayout.LabelField(name, GUILayout.MaxWidth(50));

            if (EditorHelper.ImageButton(WorldHelper.GetIconPath("add.png")))
            {
                GetAllVariableTypeNames();

                SearchableMenuWindow.Show(
                GUIUtility.GUIToScreenPoint(Event.current.mousePosition),
                    m_AllCustomVariableDataTypeNames,
                    (idx, selectedItem) =>
                    {
                        var realType = Helper.CreateGenericType(typeof(Variable<>), new Type[] { m_AllCustomVariableDataTypes[idx] });
                        variableManager.CreateVariable(realType, selectedItem);
                        ExpandVariableList(m_AllCustomVariableDataTypes[idx], true, variableManager);
                    }
                );
            }

            if (EditorHelper.ImageButton(WorldHelper.GetIconPath("expand.png")))
            {
                ExpandVariableList(null, true, variableManager);
            }
            if (EditorHelper.ImageButton(WorldHelper.GetIconPath("shrink.png")))
            {
                ExpandVariableList(null, false, variableManager);
            }

            m_SearchText = EditorGUILayout.TextField(GUIContent.none, m_SearchText, EditorStyles.toolbarSearchField);

            EditorGUILayout.EndHorizontal();
            if (variableManager.Draw)
            {
                EditorHelper.IndentLayout(() =>
                {
                    var groupCount = variableManager.GroupCount;
                    for (var i = 0; i < groupCount; i++)
                    {
                        var group = variableManager.GetGroupInfo(i);
                        DrawGroup(variableManager, group);
                    }
                });
            }
        }

        private void ExpandVariableList(Type valueType, bool expand, VariableManager variableManager)
        {
            variableManager.Draw = expand;

            var n = variableManager.GroupCount;
            for (var i = 0; i < n; i++)
            {
                var group = variableManager.GetGroupInfo(i);
                if (valueType == null || group.Type == valueType)
                {
                    group.Draw = expand;
                }
            }
        }

        private void DrawGroup(VariableManager variableManager, VariableManager.Group group)
        {
            EditorGUILayout.BeginHorizontal();
            group.Draw = EditorHelper.Foldout(group.Draw);
            EditorGUILayout.LabelField(group.Type.Name);
            EditorGUILayout.EndHorizontal();
            if (group.Draw)
            {
                EditorHelper.IndentLayout(() =>
                {
                    foreach (var varID in group.VariableIDs)
                    {
                        var variable = variableManager.GetVariableByID(varID);
                        DrawVariable(variable);
                    }
                });
            }
        }

        private void DrawVariable(IVariable v)
        {
            if (MatchSearch(v.Name))
            {
                EditorGUILayout.BeginHorizontal();
                v.Draw = EditorHelper.Foldout(v.Draw);
                v.Name = EditorGUILayout.TextField(GUIContent.none, v.Name, GUILayout.MaxWidth(80));
                EditorGUILayout.EndHorizontal();
                if (v.Draw)
                {
                    EditorHelper.IndentLayout(() =>
                    {
                        DrawClassFields(v, false);
                    });
                }
            }
        }

        private void OnDoubleClick()
        {
            if (m_SelectionInfo.Nodes.Count > 0)
            {
                var nodeView = m_SelectionInfo.Nodes[0];
                nodeView.ShowComment = !nodeView.ShowComment;
            }
        }

        private void DrawTreeSelection()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (Global.RuntimeBehaviourTreeManager == null)
            {
                return;
            }

            var trees = Global.RuntimeBehaviourTreeManager.Trees.Values;
            if (m_BehaviourTreeNames == null || m_BehaviourTreeNames.Length != trees.Count)
            {
                m_BehaviourTreeNames = new string[trees.Count];
            }

            var idx = 0;
            foreach (var tree in trees)
            {
                m_BehaviourTreeNames[idx++] = tree.Name;
            }

            EditorGUIUtility.labelWidth = 100;
            var oldIndex = GetIndex(trees, m_Tree);
            var index = EditorGUILayout.Popup("Behaviour Trees", oldIndex, m_BehaviourTreeNames);
            if (index != oldIndex)
            {
                SetTree(GetTree(trees, index));
            }
            EditorGUIUtility.labelWidth = 0;
        }

        private int GetIndex(SortedDictionary<int, BehaviourTree>.ValueCollection collection, BehaviourTree tree)
        {
            if (tree == null)
            {
                return -1;
            }

            var idx = 0;
            foreach (var t in collection)
            {
                if (t.ID == tree.ID)
                {
                    return idx;
                }
                ++idx;
            }
            return -1;
        }

        private BehaviourTree GetTree(SortedDictionary<int, BehaviourTree>.ValueCollection collection, int index)
        {
            var idx = 0;
            foreach (var tree in collection)
            {
                if (idx == index)
                {
                    return tree;
                }
                ++idx;
            }
            return null;
        }

        private void CreateNodeMenu(Vector2 pos, ActionType action, CompoundNode parent)
        {
            GenericMenu menu = new GenericMenu();
            AddNodeMenuItems(menu, pos, action, parent);
            menu.ShowAsContext();
        }

        private void AddNodeMenuItems(GenericMenu menu, Vector2 mousePos, ActionType action, CompoundNode parent)
        {
            var allNodeTypes = Helper.GetAllSubclasses(typeof(Node));

            foreach (var type in allNodeTypes)
            {
                var attributes = type.GetCustomAttributes(false);
                string groupName = type.Name;
                foreach (var attribute in attributes)
                {
                    if (attribute is BehaviourGroupAttribute)
                    {
                        var group = (BehaviourGroupAttribute)attribute;
                        groupName = group.GroupName;
                        break;
                    }
                }
                AddNodeMenuItem(type, groupName, menu, mousePos, action, parent);
            }
        }

        private void GetAllVariableTypeNames()
        {
            m_AllCustomVariableDataTypeNames.Clear();
            m_AllCustomVariableDataTypes.Clear();

            List<Type> predefinedTypes = new()
            {
                typeof(string),
                typeof(int),
                typeof(float),
                typeof(bool),
                typeof(Vector2),
                typeof(Vector3),
                typeof(Vector4),
                typeof(Vector2Int),
                typeof(Vector3Int),
                typeof(Color),
                typeof(GameObject),
                typeof(Transform),
            };

            var allCustomVariableDataTypes = Helper.GetClassesImplementingInterface(typeof(IVariableData));
            allCustomVariableDataTypes.AddRange(predefinedTypes);
            foreach (var type in allCustomVariableDataTypes)
            {
                AddVariableDataType(type);
            }
        }

        private void AddVariableDataType(Type type)
        {
            m_AllCustomVariableDataTypeNames.Add(type.Name);
            m_AllCustomVariableDataTypes.Add(type);
        }

        private bool MatchSearch(string name)
        {
            if (string.IsNullOrEmpty(m_SearchText))
            {
                return true;
            }

            if (name.IndexOf(m_SearchText) >= 0)
            {
                return true;
            }

            return false;
        }

        private NodeView GetSelectedNodeView()
        {
            if (m_SelectionInfo.Nodes.Count == 0)
            {
                return null;
            }

            return m_SelectionInfo.Nodes[0];
        }

        private const int m_MinFontSize = 5;
        private const int m_MaxFontSize = 25;
        private const float m_NameHeight = 20;
        private const float m_XPadding = 40;
        private const float m_YPadding = 40;
        private const float m_MaxCommentAreaWidth = 200;
        private const float m_StatusIconSize = 30;
        private const float m_DynamicIconSize = 20;
        private const float m_ProgressBarHeight = 10;
        private const float m_DescriptionWidth = 250;
        private const float m_DescriptionHeight = 70;
        private Vector2 m_CreateWindowPosition;
        private Color m_IconBackgroundColor = new(0.3f, 0.3f, 0.3f, 1);
        private Color m_IconSelectColor = Color.gray;
        private Color m_RootNodeColor = new Color32(255, 127, 39, 255);
        private Color m_TopConnectorColor = Color.gray;
        private Color m_BottomConnectorColor = Color.gray;
        private Color m_TopConnectorSelectColor = Color.cyan;
        private Color m_BottomConnectorSelectColor = Color.cyan;
        private Texture2D m_ButtonBackground;
        private GUIStyle m_TextFieldStyle;
        private GUIStyle m_CommentFieldStyle;
        private GUIStyle m_TextAreaStyle;
        private float m_SplitterPosition = 0.65f;
        private bool m_IsDragging;
        private Rect m_ViewportArea;
        private DateTime lastClickTime;
        private Vector2 lastClickPosition;
        private const float DoubleClickTime = 0.3f; // 双击时间阈值（秒）
        private const float PositionTolerance = 5f; // 位置容差（像素）
        private string[] m_BehaviourTreeNames;
        private bool m_ValidClick = false;
        private PageType m_SelectedPage = PageType.Node;
        private List<string> m_AllCustomVariableDataTypeNames = new();
        private List<Type> m_AllCustomVariableDataTypes = new();
        private bool m_ReplaceNode = false;
        private bool m_DrawDragLine = false;
        private bool m_ShowDragMenu = false;
        private bool m_ShowSiblingIndex = false;
        private string m_SearchText = "";
        private readonly GUIContent[] m_ToolbarNames = new GUIContent[]
        {
            new GUIContent("Node", ""),
            new GUIContent("Variable", ""),
        };
        private enum PageType
        {
            Node,
            Variable,
        }
        private enum ActionType
        {
            CreateChildNode,
            ReplaceNode,
            CreateParentNode,
        }
    }
}