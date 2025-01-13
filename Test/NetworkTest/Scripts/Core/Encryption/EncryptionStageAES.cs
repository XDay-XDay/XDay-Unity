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


using XDay.NetworkAPI;

class EncryptionStageAES : IEncryptionStage
{
    public EncryptionStageAES(byte[] key, byte[] iv)
    {
        m_EncryptionTransformer = new AESTransformer(iv, key);
        m_DecryptionTransformer = new AESTransformer(iv, key);
    }

    public int Encrypt(ByteStream input, ByteStream output)
    {
        output.Position = 0;
        output.SetLength((int)input.Length);
        return m_EncryptionTransformer.TransformData(input.Buffer, (int)input.Length, output, 0);
    }

    public int Decrypt(ByteStream input, ByteStream output)
    {
        output.Position = 0;
        output.SetLength((int)input.Length);
        return m_DecryptionTransformer.TransformData(input.Buffer, (int)input.Length, output, 0);
    }

    private readonly IDataTransformer m_EncryptionTransformer;
    private readonly IDataTransformer m_DecryptionTransformer;
}

internal class EncryptionStageAESCreator : IEncryptionStageCreator
{
    public IEncryptionStage Create(object data)
    {
        var key = data as byte[];
        return new EncryptionStageAES(key, key);
    }
}