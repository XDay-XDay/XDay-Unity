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
using UnityEngine;
using System.IO;
using Google.Protobuf;
using System.Threading;
using XDay.NetworkAPI;

internal class ProtocalStageProtoBuf : IProtocalStage
{
    public void Encode(object protocal, ByteStream output)
    {
        var message = protocal as IMessage;
        var requestID = Interlocked.Increment(ref m_CurrentRequestID);
        var prop = message.GetType().GetProperty("RequestID");
        prop?.SetValue(message, (ulong)requestID);
        WriteHeader(message, output);
        WriteTo(message, output);
        Debug.Log($"Send [{message.GetType().Name}]: {message}");
    }

    public object Decode(ByteStream input)
    {
        var typeID = XDay.UtilityAPI.Helper.BytesToInt32(input.Buffer, 0);
        input.Position += 4;

        var message = CreateProtocal((uint)typeID);
        if (message != null)
        {
            ReadFrom(message, input);
            Debug.Log($"Receive [{message.GetType().Name}]: {message}");
        }
        return message;
    }

    public void RegisterProtocal(Type type)
    {
        var typeID = (uint)Helper.DJB2Hash(type.FullName.ToLower());
        m_ProtocalTypeIDToCreator.Add(typeID, () => { return (IMessage)Activator.CreateInstance(type); });
        m_ProtocalTypeToTypeID[type] = typeID;
    }

    public void RegisterProtocal<T>() where T : class, new()
    {
        RegisterProtocal(typeof(T));
    }

    private IMessage CreateProtocal(uint msgTypeID)
    {
        m_ProtocalTypeIDToCreator.TryGetValue(msgTypeID, out var creator);
        if (creator == null)
        {
            Debug.LogError($"{msgTypeID} not registered!");
            return null;
        }
        return creator();
    }

    private uint GetMessageTypeID(IMessage message)
    {
        var ok = m_ProtocalTypeToTypeID.TryGetValue(message.GetType(), out var id);
        if (!ok)
        {
            Debug.Assert(false, $"GetMessageTypeID {message.GetType()} failed");
        }

        return id;
    }

    private void WriteHeader(IMessage message, ByteStream stream)
    {
        var typeID = GetMessageTypeID(message);

        stream.WriteInt32((int)typeID);
    }

    private void WriteTo(IMessage message, Stream output)
    {
        m_CodedOutputStream.Initialize(output);
        message.WriteTo(m_CodedOutputStream);
        m_CodedOutputStream.Flush();
    }

    private void ReadFrom(IMessage message, Stream input)
    {
        m_CodedInputStream.Initialize(input);
        message.MergeFrom(m_CodedInputStream);
        m_CodedInputStream.CheckReadEndOfStreamTag();
    }

    private readonly Dictionary<uint, Func<IMessage>> m_ProtocalTypeIDToCreator = new();
    private readonly Dictionary<Type, uint> m_ProtocalTypeToTypeID = new();
    private long m_CurrentRequestID = 0;
    private readonly CodedOutputStream m_CodedOutputStream = new();
    private readonly CodedInputStream m_CodedInputStream = new()
    {
        DiscardUnknownFields = false,
        ExtensionRegistry = null,
    };
}

public class ProtocalStageProtoBufCreator : IProtocalStageCreator
{
    public IProtocalStage Create()
    {
        return new ProtocalStageProtoBuf();
    }
}
