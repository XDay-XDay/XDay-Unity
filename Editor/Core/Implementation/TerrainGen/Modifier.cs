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
using XDay.SerializationAPI;

namespace XDay.Terrain.Editor
{
    class TerrainSize
    {
        public float Width = 128.0f;
        public float Height = 128.0f;
        public int Resolution = 128;
        public float MaxHeight = 50;
        public Vector3 Position;

        public float GetStepX() { return Width / (Resolution - 1); }
        public float GetStepY() { return Height / (Resolution - 1); }
    };

    class TerrainModifierConnectionInfo
    {
        public int Index;
        public int ModifierID;
    };

    class TerrainModifierOutput
    {
        public bool IsConnectedTo(TerrainModifier modifier, int inputIndex)
        {
            if (modifier == null)
            {
                return false;
            }

            foreach (var connection in Connections)
            {
                if (connection.ModifierID == modifier.GetID() &&
                    connection.Index == inputIndex)
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            Connections.Clear();
        }

        public void Remove(int inputIndex)
        {
            for (var i = 0; i < Connections.Count; ++i)
            {
                if (Connections[i].Index == inputIndex)
                {
                    Connections.RemoveAt(i);
                    break;
                }
            }
        }

        public void Save(ISerializer writer, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_Version, "TerrainModifierOutput.Version");
            writer.WriteList(Connections, "Connections", (connection, index) =>
            {
                writer.WriteStructure($"Connection {index}", () =>
                    {
                        writer.WriteInt32(translator.Convert(connection.ModifierID), "Connected Modifier ID");
                        writer.WriteInt32(connection.Index, "Input Index");
                    });
            });
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("TerrainModifierOutput.Version");
            Connections = reader.ReadList("Connections", (index)=>
            {
                var connection = new TerrainModifierConnectionInfo();
                reader.ReadStructure($"Connection {index}", ()=>
                    {
                    connection.ModifierID = reader.ReadInt32("Connected Modifier ID");
                    connection.Index = reader.ReadInt32("Input Index");
                });
                return connection;
            });
        }

        public List<TerrainModifierConnectionInfo> Connections = new();
        private const int m_Version = 1;
    };

    class TerrainModifierInput
    {
        public TerrainModifierInput(int connectedModifierID, int outputIndex)
        {
            ConnectedModifierID = connectedModifierID;
            OutputIndex = outputIndex;
        }

        public TerrainModifierInput()
        {
        }

        public void Clear()
        {
            ConnectedModifierID = 0;
            OutputIndex = -1;
        }

        public void Save(ISerializer writer, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_Version, "TerrainModifierInput.Version");
            writer.WriteInt32(translator.Convert(ConnectedModifierID), "Connected Modifier ID");
            writer.WriteInt32(OutputIndex, "Output Index");
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("TerrainModifierInput.Version");
            ConnectedModifierID = reader.ReadInt32("Connected Modifier ID");
            OutputIndex = reader.ReadInt32("Output Index");
        }

        public int ConnectedModifierID = 0;
        public int OutputIndex = -1;
        private const int m_Version = 1;
    };

    public interface ITerrainModifierSetting
    {
        void Save(ISerializer writer);
        void Load(IDeserializer reader);
    };

    abstract class TerrainModifier
    {
        public TerrainModifier(int id)
        {
            mID = id;
        }

        public TerrainModifier()
        {
        }

        public void OnDestroy()
        {
            mGenerator.RemoveModifier(this);
            Disconnect();
        }

        public void Initialize(TerrainGenerator generator)
        {
            mGenerator = generator;

            if (mSetting == null)
            {
                mSetting = CreateSetting();
                mInputs = new TerrainModifierInput[GetMaxInputCount()];
                for (var i = 0; i < mInputs.Length; ++i)
                {
                    mInputs[i] = new();
                }
                mOutputs = new TerrainModifierOutput[GetMaxOutputCount()];
                for (var i = 0; i < mOutputs.Length; ++i)
                {
                    mOutputs[i] = new();
                }
            }
        }

        public virtual int GetID() { return mID; }
        public virtual TerrainSize GetSize()
        {
            for (var i = 0; i < GetMaxInputCount(); ++i)
            {
                var inputModifier = GetInputModifier(i);
                if (inputModifier != null)
                {
                    return inputModifier.GetSize();
                }
            }
            return new TerrainSize { Width = 0, Height = 0, Resolution = 0 };
        }

        public abstract string GetName();
        public virtual int GetMaxInputCount() { return 1; }
        public virtual int GetMaxOutputCount() { return 1; }
        public virtual bool CanDelete() { return true; }
        public virtual Color GetDisplayColor() { return new Color(1, 1, 0, 0.5f); }

        public abstract void Execute();
        public virtual List<float> GetHeightData() { return mHeightData; }
        public virtual List<Color> GetMaskData() { return mMaskData; }
        public virtual bool HasMaskData() { return false; }
        public abstract void DrawInspector();
        public virtual bool IsVisible() { return true; }

        public void OutputTo(int outputIndex, TerrainModifier modifier, int inputIndex)
        {
            for (var i = (int)mOutputs.Length - 1; i >= 0; --i)
            {
                if (mOutputs[i].IsConnectedTo(modifier, inputIndex))
                {
                    return;
                }
            }

            var connection = new TerrainModifierConnectionInfo
            {
                Index = inputIndex,
                ModifierID = modifier.GetID()
            };
            mOutputs[outputIndex].Connections.Add(connection);
            modifier.mInputs[inputIndex] = new TerrainModifierInput(GetID(), outputIndex);
        }

        public void Disconnect()
        {
            for (var i = 0; i < mOutputs.Length; ++i)
            {
                DisconnectOutput(i);
            }

            for (var i = 0; i < mInputs.Length; ++i)
            {
                DisconnectInput(i);
            }
        }

        public void DisconnectInput(int inputIndex)
        {
            if (mInputs[inputIndex].ConnectedModifierID != 0)
            {
                var modifier = mGenerator.GetModifier(mInputs[inputIndex].ConnectedModifierID);
                if (modifier != null)
                {
                    var output = modifier.GetOutputs()[mInputs[inputIndex].OutputIndex];
                    output.Remove(inputIndex);
                }
                mInputs[inputIndex].Clear();
            }
        }

        public void DisconnectOutput(int outputIndex)
        {
            foreach (var connection in mOutputs[outputIndex].Connections)
            {
                var modifier = mGenerator.GetModifier(connection.ModifierID);
                var input = modifier.GetInputs()[connection.Index];
                input.Clear();
                mOutputs[outputIndex].Clear();
            }
        }

        public ITerrainModifierSetting GetSetting() { return mSetting; }
        public TerrainModifierInput[] GetInputs() { return mInputs; }
        public TerrainModifierOutput[] GetOutputs() { return mOutputs; }

        public virtual TerrainModifier GetInputModifier(int index)
        {
            var input = GetInputs()[index];
            if (input.ConnectedModifierID == 0)
            {
                return mGenerator.GetStartModifier();
            }
            return mGenerator.GetModifier(input.ConnectedModifierID);
        }

        public virtual TerrainModifier GetAnyValidInputModifier()
        {
            for (var i = 0; i < GetMaxInputCount(); ++i)
            {
                var input = GetInputs()[i];
                if (input.ConnectedModifierID != 0)
                {
                    return mGenerator.GetModifier(input.ConnectedModifierID);
                }
            }

            return mGenerator.GetStartModifier();
        }

        public TerrainModifier GetPureInputModifier(int index)
        {
            var input = GetInputs()[index];
            if (input.ConnectedModifierID == 0)
            {
                return null;
            }
            return mGenerator.GetModifier(input.ConnectedModifierID);
        }

        public abstract ITerrainModifierSetting CreateSetting();

        protected void SetMaskDataToHeightMap()
        {
            var heightData = GetHeightData();
            if (heightData != null)
            {
                mMaskData.Clear();
                mMaskData.Capacity = heightData.Count;
                List<float> heights = new(heightData);
                TerrainGenHelper.NormalizeHeights(heights);
                for (var i = 0; i < heights.Count; ++i)
                {
                    float height = heights[i];

                    mMaskData.Add(new Color(height, height, height, height));
                }
            }
        }

        public void Save(ISerializer writer, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_Version, "TerrainModifier.Version");
            writer.WriteInt32(translator.Convert(mID), "ID");

            writer.WriteStructure("Setting", () =>
            {
                mSetting.Save(writer);
            });

            writer.WriteArray(mInputs, "Inputs", (input, index) =>
            {
                writer.WriteStructure($"Input {index}", () =>
                    {
                        input.Save(writer, translator);
                    });
            });

            writer.WriteArray(mOutputs, "Outputs", (output, index) =>
            {
                writer.WriteStructure($"Output {index}", () =>
                    {
                        output.Save(writer, translator);
                    });
            });
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("TerrainModifier.Version");

            mID = reader.ReadInt32("ID");

            mSetting = CreateSetting();
            reader.ReadStructure("Setting", () =>
            {
                mSetting.Load(reader);
            });

            mInputs = reader.ReadArray("Inputs", (index) =>
            {
                var input = new TerrainModifierInput();
                reader.ReadStructure($"Input {index}", () =>
                    {
                        input.Load(reader);
                    });
                return input;
            });

            mOutputs = reader.ReadArray("Outputs", (index) =>
            {
                var output = new TerrainModifierOutput();
                reader.ReadStructure($"Output {index}", () =>
                    {
                        output.Load(reader);
                    });
                return output;
            });
        }

        protected TerrainGenerator mGenerator;
        protected List<Color> mMaskData = new();
        protected List<float> mHeightData = new();
        private int mID = 0;
        private ITerrainModifierSetting mSetting;
        private TerrainModifierInput[] mInputs;
        private TerrainModifierOutput[] mOutputs;
        private const int m_Version = 1;
    };

    /// <summary>
    /// will create view from output modifier
    /// </summary>
    abstract class OutputBase : TerrainModifier
    {
        public OutputBase(int id)
            : base(id)
        {
        }

        public OutputBase()
        {
        }

        public void SetTempInput(TerrainModifier input) { mTempInput = input; }
        public TerrainModifier GetTempInput()
        {
            return mTempInput;
        }
        public abstract Material GetMaterial();
        public override int GetMaxInputCount() { return 0; }
        public override int GetMaxOutputCount() { return 0; }
        public override bool CanDelete() { return false; }
        public override bool IsVisible() { return false; }
        public override void Execute()
        {
            var inputModifier = GetTempInput();

            float maxHeight = mGenerator.GetMaxHeight();

            mHeightData.Clear();
            var heightData = inputModifier.GetHeightData();
            if (heightData != null)
            {
                mHeightData = new(heightData);

                var size = GetSize();

                for (var i = 0; i < size.Resolution; ++i)
                {
                    for (var j = 0; j < size.Resolution; ++j)
                    {
                        mHeightData[i * size.Resolution + j] *= maxHeight;
                    }
                }
            }

            mMaskData.Clear();
            var maskData = inputModifier.GetMaskData();
            if (maskData != null)
            {
                mMaskData = new(maskData);
            }
        }
        public override TerrainSize GetSize()
        {
            return mTempInput.GetSize();
        }

        public override Color GetDisplayColor()
        {
            return new Color(1, 0, 1, 0.5f);
        }

        private TerrainModifier mTempInput;
    };
}
