// The MIT License (MIT)

// Copyright (c) 2014 Hans Wolff

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Security.Cryptography;

class CounterModeCryptoTransform : ICryptoTransform
{
    public CounterModeCryptoTransform(SymmetricAlgorithm symmetricAlgorithm, byte[] key, byte[] counter)
    {
        if (symmetricAlgorithm == null) throw new ArgumentNullException("symmetricAlgorithm");
        if (key == null) throw new ArgumentNullException("key");
        if (counter == null) throw new ArgumentNullException("counter");
        if (counter.Length != symmetricAlgorithm.BlockSize / 8)
            throw new ArgumentException(string.Format(
                "Counter size must be same as block size (actual: {0}, expected: {1})",
                counter.Length, symmetricAlgorithm.BlockSize / 8));

        symmetricAlgorithm.Mode = CipherMode.ECB;
        symmetricAlgorithm.Padding = PaddingMode.None;
        counter = counter.Clone() as byte[];
        key = key.Clone() as byte[];

        m_SymmetricAlgorithm = symmetricAlgorithm;
        m_Counter = counter;
        m_OutPut = new byte[symmetricAlgorithm.BlockSize / 8];
        m_OutPutUsed = m_OutPut.Length;

        m_CounterEncryptor = symmetricAlgorithm.CreateEncryptor(key, null);
    }

    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        var output = new byte[inputCount];
        TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
        return output;
    }

    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer,
        int outputOffset)
    {
        for (var i = 0; i < inputCount; i++)
        {
            if (m_OutPutUsed >= m_OutPut.Length)
            {
                EncryptCounterThenIncrement();
            }

            var mask = m_OutPut[m_OutPutUsed++];
            outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ mask);
        }

        return inputCount;
    }

    private void EncryptCounterThenIncrement()
    {
        m_OutPutUsed = 0;
        m_CounterEncryptor.TransformBlock(m_Counter, 0, m_Counter.Length, m_OutPut, 0);
        for (var i = m_Counter.Length - 1; i >= 0; i--)
        {
            if (++m_Counter[i] != 0)
                break;
        }
    }

    public void Dispose()
    {
    }

    public int InputBlockSize => m_SymmetricAlgorithm.BlockSize / 8;
    public int OutputBlockSize => m_SymmetricAlgorithm.BlockSize / 8;
    public bool CanTransformMultipleBlocks => true;
    public bool CanReuseTransform => false;

    private readonly byte[] m_Counter;
    private readonly ICryptoTransform m_CounterEncryptor;
    private readonly SymmetricAlgorithm m_SymmetricAlgorithm;
    private byte[] m_OutPut;
    private int m_OutPutUsed;
}