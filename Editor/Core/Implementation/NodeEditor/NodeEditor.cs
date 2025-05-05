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
using static UnityEditor.GenericMenu;

namespace XDay.Node.Editor
{
    public interface IXNodeCoordinateConverter
    {
        Vector2 ScreenToContentPosition(Vector2 screenPos);
        Vector2 ContentToScreenPosition(Vector2 localPos);
        Vector2 GetContentSize();
    };

    public interface IXNodeEditorEventListener
    {
        void OnNodeWillBeDeleted(XNode node);
        void OnNodeDeleted(XNode node);
        void OnNodeOutputDisconnected(XNode node, int outputIndex);
        void OnNodeInputDisconnected(XNode node, int inputIndex);
        void OnNodeOutputConnected(XNode from, int outputIndex, XNode to, int inputIndex);
        void OnNodeSelected(XNode node);
        void OnNodeDoubleClicked(XNode node);
    };

    public enum XNodeSelectType
    {
        None,
        SingleSelection,
    };

    public class XNodeEditor
    {
        private class MenuItem
        {
            public string name;
            public MenuFunction onClick;
            public Func<bool> drawCondition;
        };

        public XNodeEditor(
            IXNodeCoordinateConverter coordinateConverter, 
            IXNodeEditorEventListener listener, 
            Func<XNode> createNode, 
            Func<object> getUserData,
            Action repaint)
        {
            Debug.Assert(coordinateConverter != null);

            mCoordinateConverter = coordinateConverter;
            mEventListener = listener;
            mCreateNode = createNode;
            mGetUserData = getUserData;
            m_Repaint = repaint;

            AddMenuItem("Delete", () =>
            {
                var nodes = GetSelectedNodes();
                if (nodes.Count > 0)
                {
                    foreach (var node in nodes)
                    {
                        DeleteNode(node);
                    }
                }
            }, () => { return true; });

            AddMenuItem("Disconnect", () =>
            {
                var nodes = GetSelectedNodes();
                if (nodes.Count > 0)
                {
                    foreach (var node in nodes)
                    {
                        var hitInfo = node.GetHitInfo();
                        if (hitInfo.type == XNodeItemType.Input)
                        {
                            DisconnectInput(node, hitInfo.index, true);
                        }
                        else if (hitInfo.type == XNodeItemType.Output)
                        {
                            DisconnectOutput(node, hitInfo.index, true);
                        }
                    }
                }
            }, () => { return true; });
        }

        public void OnDestroy()
        {
        }

        public void Clear()
        {
            foreach (var node in mNodes)
            {
                node.OnDestroy();
            }
            mNodes.Clear();
        }

        public void Draw()
        {
            if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                m_IsDragging = true;
            }

            foreach (var node in mNodes)
            {
                DrawConnection(node);
            }

            foreach (var node in mNodes)
            {
                DrawNode(node);
            }
            
            DrawContextMenu();

            UpdateInput();

            m_Repaint?.Invoke();

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                m_IsDragging = false;
            }
        }

        public void AddNode(XNode node)
        {
            mNodes.Add(node);
        }

        public void ConnectNode(XNode from, int outputIndex, XNode to, int inputIndex, bool triggerEvent)
        {
            if (from == null || to == null)
            {
                return;
            }

            var output = from.GetOutput(outputIndex);

            var input = to.GetInput(inputIndex);
            Debug.Assert(input.connectedNodeID == 0);

            var connection = new ConnectionInfo
            {
                connectedNodeID = to.GetID(),
                connetedNodeInputIndex = inputIndex
            };
            output.connections.Add(connection);

            input.connectedNodeID = from.GetID();
            input.connetedNodeOutputIndex = outputIndex;

            if (triggerEvent)
            {
                mEventListener.OnNodeOutputConnected(from, outputIndex, to, inputIndex);
            }
        }

        public void DisconnectNode(XNode node, bool triggerEvent)
        {
            var outputCount = node.GetRequiredOutputCount();
            for (var i = 0; i < outputCount; ++i)
            {
                DisconnectOutput(node, i, triggerEvent);
            }

            var inputCount = node.GetRequiredInputCount();
            for (var i = 0; i < inputCount; ++i)
            {
                DisconnectInput(node, i, triggerEvent);
            }
        }

        public void DisconnectInput(XNode node, int inputIndex, bool triggerEvent)
        {
            var input = node.GetInput(inputIndex);
            if (input.connectedNodeID != 0)
            {
                var connectedNode = GetNode(input.connectedNodeID);
                var output = connectedNode.GetOutput(input.connetedNodeOutputIndex);
                output.Remove(inputIndex);
                input.Clear();

                if (triggerEvent)
                {
                    mEventListener.OnNodeInputDisconnected(node, inputIndex);
                }
            }
        }

        public void DisconnectOutput(XNode node, int outputIndex, bool triggerEvent)
        {
            var output = node.GetOutput(outputIndex);
            foreach (var connection in output.connections)
            {
                var connectedNode = GetNode(connection.connectedNodeID);
                var input = connectedNode.GetInput(connection.connetedNodeInputIndex);
                input.Clear();
                output.Clear();

                if (triggerEvent)
                {
                    mEventListener.OnNodeOutputDisconnected(node, outputIndex);
                }
            }
        }

        public List<XNode> GetNodes() { return mNodes; }

        public void AddMenuItem(string name, MenuFunction onClick, Func<bool> drawCondition)
        {
            var condition = drawCondition;
            condition ??= () => { return true; };

            var item = new MenuItem
            {
                name = name,
                onClick = onClick,
                drawCondition = condition
            };
            mMenuItems.Add(item);
        }

        public Vector2 GetContextMenuPos()
        {
            return ScreenToWorldPosition(mContextMenuPos);
        }

        public void Save(ISerializer writer, IObjectIDConverter idTranslator)
        {
            writer.WriteInt32(mVersion, "Version");

            writer.WriteList(mNodes, "Nodes", (node, index) =>
            {
                writer.WriteStructure($"Node {index}", () =>
                    {
                        mNodes[index].Save(writer, idTranslator);
                    });
            });
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("Version");

            mNodes = reader.ReadList("Nodes", (index) =>
            {
                var node = mCreateNode();
                reader.ReadStructure($"Node {index}", () =>
                    {
                        node.Load(reader);
                        node.Initialize(mGetUserData());
                    });
                return node;
            });
        }

        private void DrawConnection(XNode node)
        {
            var e = Event.current;
            float tangentLength = GetTangentLength();
            var outputCount = node.GetRequiredOutputCount();
            for (var i = 0; i < outputCount; ++i)
            {
                var output = node.GetOutput(i);
                if (node.GetHitInfo().type == XNodeItemType.Output)
                {
                    //output is selected
                    var startPos = WorldToScreenPosition(output.center + node.GetMin());
                    var endPos = e.mousePosition;

                    if (IsReleased())
                    {
                        foreach (var other in mNodes)
                        {
                            if (other != node)
                            {
                                var hit = Hit(other, out var hitInfo);
                                if (hit && hitInfo.type == XNodeItemType.Input)
                                {
                                    if (!output.IsConnectedTo(other, hitInfo.index))
                                    {
                                        var input = other.GetInput(hitInfo.index);
                                        if (input.connectedNodeID != 0)
                                        {
                                            DisconnectInput(other, hitInfo.index, true);
                                        }

                                        ConnectNode(node, i, other, hitInfo.index, true);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    else if (IsDragging())
                    {
                        EditorHelper.DrawCubeBezier(startPos, startPos + new Vector2(tangentLength, 0), endPos - new Vector2(tangentLength, 0), endPos, Color.white, 6.0f);
                    }
                }

                foreach (var connection in output.connections)
                {
                    var connectedNode = GetNode(connection.connectedNodeID);
                    var input = connectedNode.GetInput(connection.connetedNodeInputIndex);

                    var startPos = WorldToScreenPosition(output.center + node.GetMin());
                    var endPos = WorldToScreenPosition(input.center + connectedNode.GetMin());
                    EditorHelper.DrawCubeBezier(startPos, startPos + new Vector2(tangentLength, 0), endPos - new Vector2(tangentLength, 0), endPos, Color.white, 6.0f);
                }
            }
        }

        private void DrawNode(XNode node)
        {
            if (!node.IsVisible())
            {
                return;
            }
            var size = node.GetSize();
            var min = WorldToScreenPosition(node.GetMin());
            if (node.GetHitInfo().type == XNodeItemType.Self)
            {
                EditorGUI.DrawRect(new Rect(min, size), node.GetColor());
            }
            else
            {
                EditorHelper.DrawLineRect(new Rect(min, size), node.GetColor(), 2);
            }

            var inputCount = node.GetRequiredInputCount();
            var outputCount = node.GetRequiredOutputCount();
            //draw input
            for (var i = 0; i < inputCount; ++i)
            {
                var input = node.GetInput(i);
                if (IsHitInput(node, i))
                {
                    EditorHelper.DrawCircle(input.radius, WorldToScreenPosition(input.center + node.GetMin()), Color.green);
                }
                else
                {
                    EditorHelper.DrawLineCircle(input.radius, WorldToScreenPosition(input.center + node.GetMin()), Color.green, 2);
                }
            }

            for (var i = 0; i < outputCount; ++i)
            {
                var output = node.GetOutput(i);
                if (IsHitOutput(node, i))
                {
                    EditorHelper.DrawCircle(output.radius, WorldToScreenPosition(output.center + node.GetMin()), Color.blue);
                }
                else
                {
                    EditorHelper.DrawLineCircle(output.radius, WorldToScreenPosition(output.center + node.GetMin()), Color.blue, 2);
                }
            }

            EditorGUI.LabelField(new Rect(WorldToScreenPosition(node.GetPosition()), new Vector2(400, 100)), node.GetTitle());
        }

        private void DrawContextMenu()
        {
            if (IsRightReleased())
            {    
                mContextMenuPos = Event.current.mousePosition;
                var contextMenu = new GenericMenu();
                foreach (var menuItem in mMenuItems)
                {
                    if (menuItem.drawCondition())
                    {
                        contextMenu.AddItem(new GUIContent(menuItem.name), false, menuItem.onClick);
                    }
                }
                contextMenu.ShowAsContext();
            }
        }

        private Vector2 WorldToScreenPosition(Vector2 worldPos)
        {
            var pos = mCoordinateConverter.ContentToScreenPosition(worldPos);
            var origin = mCoordinateConverter.GetContentSize() * 0.5f - mViewPos;
            return origin + pos;
        }

        private Vector2 ContentToWorldPosition(Vector2 contentPos)
        {
            return contentPos - mCoordinateConverter.GetContentSize() * 0.5f + mViewPos;
        }

        private Vector2 ScreenToWorldPosition(Vector2 pos)
        {
            var contentPos = mCoordinateConverter.ScreenToContentPosition(pos);
            return ContentToWorldPosition(contentPos);
        }

        private float GetTangentLength()
        {
            return mTangentLength;
        }

        private void UpdateInput()
        {
            UpdateSelection();
            
            UpdateDoubleClick();

            UpdateNodeMove();

            UpdateViewport();
        }

        private Vector2 GetCursorPositionInContentSpace()
        {
            var cursorPos = Event.current.mousePosition;
            return mCoordinateConverter.ScreenToContentPosition(cursorPos);
        }

        private bool Hit(XNode node, out XNodeHitInfo hitInfo)
        {
            hitInfo = new();
            var cursorPos = GetCursorPositionInContentSpace();
            var worldPos = ContentToWorldPosition(cursorPos);
            bool hit = node.HitTest(worldPos, hitInfo);
            return hit;
        }

        private XNodeSelectType GetSelectType()
        {
            var e = Event.current;
            if (e.button == 0 && e.type == EventType.MouseDown)
            {
                return XNodeSelectType.SingleSelection;
            }
            return XNodeSelectType.None;
        }

        private Vector2 GetDeltaMovement()
        {
            return Event.current.delta;
        }

        private bool IsHitInput(XNode node, int inputIndex)
        {
            return node.GetHitInfo().type == XNodeItemType.Input &&
            node.GetHitInfo().index == inputIndex;
        }

        private bool IsHitOutput(XNode node, int outputIndex)
        {
            return node.GetHitInfo().type == XNodeItemType.Output &&
            node.GetHitInfo().index == outputIndex;
        }

        private void UpdateSelection()
        {
            XNodeSelectType selectType = GetSelectType();
            if (selectType != XNodeSelectType.None)
            {
                foreach (var node in mNodes)
                {
                    if (node.IsVisible())
                    {
                        var hit = Hit(node, out var hitInfo);
                        if (hit)
                        {
                            Debug.Log($"Hit {node}");
                            node.SetHitInfo(hitInfo);

                            mEventListener.OnNodeSelected(node);

                            if (selectType == XNodeSelectType.SingleSelection)
                            {
                                foreach (var otherNode in mNodes)
                                {
                                    if (otherNode != node)
                                    {
                                        otherNode.SetHitInfo(new());
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            if (selectType == XNodeSelectType.SingleSelection)
                            {
                                node.SetHitInfo(new());
                            }
                        }
                    }
                }
            }
        }

        private void UpdateNodeMove()
        {
            if (IsDragging())
            {
                mMover.Update(GetCursorPositionInContentSpace());

                foreach (var node in mNodes)
                {
                    var itemType = node.GetHitInfo().type;
                    if (itemType == XNodeItemType.Self)
                    {
                        node.Move(GetMoveOffset());
                    }
                }
            }

            if (IsReleased())
            {
                mMover.Reset();
            }
        }

        private void UpdateDoubleClick()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0) 
            {
                if (Time.realtimeSinceStartup - m_LastClickTime <= doubleClickTime &&
                    Vector2.Distance(m_LastClickPosition, currentEvent.mousePosition) < 5f)
                {
                    foreach (var node in mNodes)
                    {
                        if (node.IsVisible())
                        {
                            var itemType = node.GetHitInfo().type;
                            if (itemType == XNodeItemType.Self)
                            {
                                Debug.Log($"Double click {node}");
                                mEventListener.OnNodeDoubleClicked(node);
                                break;
                            }
                        }
                    }
                }
                m_LastClickTime = Time.realtimeSinceStartup;
                m_LastClickPosition = currentEvent.mousePosition;
            }
        }

        private void UpdateViewport()
        {
            if (IsDraggingMiddle())
            {
                mMover.Update(GetCursorPositionInContentSpace());
                mViewPos -= GetMoveOffset();
            }

            if (IsReleasedMiddle())
            {
                mMover.Reset();
            }
        }

        private List<XNode> GetSelectedNodes()
        {
            List<XNode> nodes = new();
            foreach (var node in mNodes)
            {
                if (node.IsSelected())
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }

        private void DeleteNode(XNode node)
        {
            if (node.CanDelete())
            {
                mEventListener.OnNodeWillBeDeleted(node);

                DisconnectNode(node, false);

                mEventListener.OnNodeDeleted(node);

                node.OnDestroy();
                mNodes.Remove(node);
            }
        }

        private bool IsDragging()
        {
            return m_IsDragging;
        }

        private bool IsReleased()
        {
            return Event.current.type == EventType.MouseUp && Event.current.button == 0;
        }

        private bool IsRightReleased()
        {
            return Event.current.type == EventType.MouseUp && Event.current.button == 1;
        }

        private bool IsReleasedMiddle()
        {
            return Event.current.type == EventType.MouseUp && Event.current.button == 2;
        }

        private bool IsDraggingMiddle()
        {
            return Event.current.type == EventType.MouseDrag && Event.current.button == 2;
        }

        private XNode GetNode(int id)
        {
            foreach (var node in mNodes)
            {
                if (node.GetID() == id)
                {
                    return node;
                }
            }

            return null;
        }

        private Vector2 GetMoveOffset()
        {
            return mMover.GetMovement();
        }

        private List<XNode> mNodes = new();
        private IXNodeCoordinateConverter mCoordinateConverter;
        private IXNodeEditorEventListener mEventListener;
        private float mTangentLength = 50;
        private IMover mMover = IMover.Create();
        private List<MenuItem> mMenuItems = new();
        private Vector2 mContextMenuPos;
        private Vector2 mViewPos;
        private Func<XNode> mCreateNode;
        private Func<object> mGetUserData;
        private float m_LastClickTime;
        private Vector2 m_LastClickPosition;
        private Action m_Repaint;
        private const float doubleClickTime = 0.2f;
        private const int mVersion = 1;
        private bool m_IsDragging = true;
    };
}