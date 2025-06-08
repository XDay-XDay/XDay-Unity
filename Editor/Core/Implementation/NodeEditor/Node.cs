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

namespace XDay.Node.Editor
{
    public class XNodeInput
    {
        public int connectedNodeID = 0;
        public int connetedNodeOutputIndex = -1;
        public Vector2 center;
        public float radius = 0;

        public void Clear()
        {
            connectedNodeID = 0;
            connetedNodeOutputIndex = -1;
        }

        public void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(mVersion, "XNodeInput.Version");
            writer.WriteInt32(converter.Convert(connectedNodeID), "Connected Node ID");
            writer.WriteInt32(connetedNodeOutputIndex, "Output Index");
            writer.WriteVector2(center, "Center");
            writer.WriteSingle(radius, "Radius");
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("XNodeInput.Version");
            connectedNodeID = reader.ReadInt32("Connected Node ID");
            connetedNodeOutputIndex = reader.ReadInt32("Output Index");
            center = reader.ReadVector2("Center");
            radius = reader.ReadSingle("Radius");
        }

        private const int mVersion = 1;
    };

    public class ConnectionInfo
    {
        public int connectedNodeID = 0;
        public int connetedNodeInputIndex = -1;
    };

    public class XNodeOutput
    {
        public List<ConnectionInfo> connections = new();
        public Vector2 center;
        public float radius = 0;

        public bool IsConnectedTo(XNode other, int inputIndex)
        {
            if (other == null)
            {
                return false;
            }

            foreach (var connection in connections)
            {
                if (connection.connectedNodeID == other.GetID() &&
                    connection.connetedNodeInputIndex == inputIndex)
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            connections.Clear();
        }

        public void Remove(int inputIndex)
        {
            for (var i = 0; i < connections.Count; ++i)
            {
                if (connections[i].connetedNodeInputIndex == inputIndex)
                {
                    connections.RemoveAt(i);
                    break;
                }
            }
        }

        public void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(mVersion, "XNodeOutput.Version");
            writer.WriteList(connections, "Connections", (connection, index) =>
            {
                writer.WriteStructure($"Connection {index}", () =>
                    {
                        writer.WriteInt32(converter.Convert(connection.connectedNodeID), "Connected Node ID");
                        writer.WriteInt32(connection.connetedNodeInputIndex, "Input Index");
                    });
            });
            writer.WriteVector2(center, "Center");
            writer.WriteSingle(radius, "Radius");
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("XNodeOutput.Version");
            connections = reader.ReadList("Connections", (index) =>
            {
                var connection = new ConnectionInfo();
                reader.ReadStructure($"Connection {index}", () =>
                    {
                        connection.connectedNodeID = reader.ReadInt32("Connected Node ID");
                        connection.connetedNodeInputIndex = reader.ReadInt32("Input Index");
                    });
                return connection;
            });
            center = reader.ReadVector2("Center");
            radius = reader.ReadSingle("Radius");
        }

        private const int mVersion = 1;
    };

    public enum XNodeItemType
    {
        Unknown = -1,
        Self,
        Input,
        Output,
    };

    public class XNodeHitInfo
    {
        public XNodeItemType type = XNodeItemType.Unknown;
        public int index = -1;
    };

    public abstract class XNode
    {
        public virtual void OnDestroy()
        {
        }

        public void Initialize(object userData)
        {
            mUserData = userData;
            CalculateNodeInfo();
        }

        public abstract int GetID();
        public abstract void OnDraw();
        public abstract string GetTitle();
        public abstract int GetRequiredInputCount();
        public abstract int GetRequiredOutputCount();
        public abstract bool CanDelete();
        public abstract bool IsVisible();
        public abstract Color GetColor();

        public void SetPosition(Vector2 pos)
        {
            mPosition = pos;
        }

        public Vector2 GetPosition() { return mPosition; }
        public void Move(Vector2 offset)
        {
            mPosition += offset;
        }
        public Vector2 GetMin() { return mPosition - mSize * 0.5f; }
        public void SetSize(Vector2 size)
        {
            mSize = size;
        }
        public Vector2 GetSize() { return mSize; }

        public void SetHitInfo(XNodeHitInfo hitInfo)
        {
            mHitInfo = hitInfo;
        }
        public XNodeHitInfo GetHitInfo() { return mHitInfo; }

        public bool IsSelected() { return mHitInfo.type != XNodeItemType.Unknown; }

        public XNodeInput GetInput(int index)
        {
            if (index >= 0 && index < mInputs.Count)
            {
                return mInputs[index];
            }
            return null;
        }
        public XNodeOutput GetOutput(int index)
        {
            if (index >= 0 && index < mOutputs.Count)
            {
                return mOutputs[index];
            }
            return null;
        }

        public bool HitTest(Vector2 pos, XNodeHitInfo hitInfo)
        {
            var min = GetMin();
            var max = min + mSize;
            for (var i = 0; i < mOutputs.Count; ++i)
            {
                if (Helper.PointInCircle(pos - min, mOutputs[i].center, mOutputs[i].radius))
                {
                    hitInfo.index = i;
                    hitInfo.type = XNodeItemType.Output;
                    return true;
                }
            }

            for (var i = 0; i < mInputs.Count; ++i)
            {
                if (Helper.PointInCircle(pos - min, mInputs[i].center, mInputs[i].radius))
                {
                    hitInfo.index = i;
                    hitInfo.type = XNodeItemType.Input;
                    return true;
                }
            }

            if (Helper.PointInRect(pos, min, max))
            {
                hitInfo.type = XNodeItemType.Self;
                return true;
            }

            return false;
        }

        public virtual void Save(ISerializer writer, IObjectIDConverter translator)
        {
            writer.WriteInt32(mVersion, "XNode.Version");

            writer.WriteVector2(mPosition, "Position");
            writer.WriteVector2(mSize, "Size");

            writer.WriteList(mInputs, "Inputs", (input, index) =>
            {
                writer.WriteStructure($"Input {index}", () =>
                    {
                        input.Save(writer, translator);
                    });
            });

            writer.WriteList(mOutputs, "Outputs", (output, index) =>
            {
                writer.WriteStructure($"Output {index}", () =>
                    {
                        output.Save(writer, translator);
                    });
            });
        }

        public virtual void Load(IDeserializer reader)
        {
            reader.ReadInt32("XNode.Version");

            mPosition = reader.ReadVector2("Position");
            mSize = reader.ReadVector2("Size");

            mInputs = reader.ReadList("Inputs", (index) =>
            {
                var input = new XNodeInput();
                reader.ReadStructure($"Input {index}", () =>
                    {
                        input.Load(reader);
                    });
                return input;
            });

            mOutputs = reader.ReadList("Outputs", (index) =>
            {
                var output = new XNodeOutput();
                reader.ReadStructure($"Output {index}", () =>
                    {
                        output.Load(reader);
                    });
                return output;
            });
        }

        private void CalculateNodeInfo()
        {
            if (mInputs.Count == 0)
            {
                var size = GetSize();

                var inputCount = GetRequiredInputCount();
                var outputCount = GetRequiredOutputCount();

                Helper.Resize(mInputs, inputCount);
                Helper.Resize(mOutputs, outputCount);

                var radiusVec = (size * 0.1f);
                float radius = Mathf.Max(radiusVec.x, radiusVec.y);

                float xSpacing = 10;
                float ySpacing = 10;

                int spaceCount = inputCount + 1;
                float h = size.y - ySpacing * spaceCount;
                float maxRadius = h / inputCount * 0.5f;
                radius = Mathf.Min(radius, maxRadius);

                float inputX = xSpacing + radius;
                float inputY = ySpacing + radius;
                float outputX = size.x - radius - ySpacing;
                float outputY = inputY;

                for (var i = 0; i < inputCount; ++i)
                {
                    var input = new XNodeInput()
                    {
                        radius = radius,
                        center = new Vector2(inputX, inputY),
                    };
                    mInputs.Add(input);

                    inputY += ySpacing + radius * 2;
                }

                for (var i = 0; i < outputCount; ++i)
                {
                    var output = new XNodeOutput()
                    {
                        radius = radius,
                        center = new Vector2(outputX, outputY),
                    };
                    
                    mOutputs.Add(output);

                    outputY += ySpacing + radius * 2;
                }
            }
        }

        protected object mUserData = null;
        private List<XNodeInput> mInputs = new();
        private List<XNodeOutput> mOutputs = new();
        private Vector2 mPosition;
        private Vector2 mSize = new Vector2(100, 100);
        private XNodeHitInfo mHitInfo = new();
        private const int mVersion = 1;
    };
}