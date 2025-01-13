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
using System.IO.Compression;
using XDay.NetworkAPI;

public enum CompressionType
{
    None,
    Deflate,
}

internal class CompressionStageDeflate : ICompressionStage
{
    public CompressionStageDeflate(int sizeThreshold)
    {
        m_SizeThreshold = sizeThreshold;
    }

    public void Compress(ByteStream input, ByteStream output)
    {
        var compressed = false;
        if (input.Length >= m_SizeThreshold)
        {
            output.WriteByte((byte)CompressionType.Deflate);
            var stream = new DeflateStream(output, CompressionMode.Compress); 
            input.CopyTo(stream);
            stream.Close();
            if (output.Length < input.Length + 1)
            {
                compressed = true;
            }
        }

        if (!compressed)
        {
            output.Position = 0;
            output.SetLength(0);
            output.WriteByte((byte)CompressionType.None); 
            input.WriteTo(output, 0); 
        }

        output.Position = 0;
    }

    public void Decompress(ByteStream input, ByteStream output)
    {
        switch ((CompressionType)input.Buffer[0])
        {
            case CompressionType.None:
                input.WriteTo(output, 1);
                break;
            case CompressionType.Deflate:
                input.Position += 1; 
                var stream = new DeflateStream(input, CompressionMode.Decompress); 
                stream.CopyTo(output);
                stream.Close();
                break;
            default:
                throw new Exception($"Unknow compressor {(CompressionType)input.Buffer[0]}");
        }

        output.Position = 0;
    }

    private readonly int m_SizeThreshold;
}

public class CompressionStageDeflateCreator : ICompressionStageCreator
{
    public CompressionStageDeflateCreator(int sizeThreshold)
    {
        m_SizeThreshold = sizeThreshold;
    }

    public ICompressionStage Create()
    {
        return new CompressionStageDeflate(m_SizeThreshold);
    }

    private readonly int m_SizeThreshold;
}
