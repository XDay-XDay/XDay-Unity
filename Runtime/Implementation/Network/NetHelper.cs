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

namespace XDay.NetworkAPI
{
    public static class NetHelper
    {
        public static void Int32ToBytes(int value, byte[] array)
        {
            array[0] = (byte)(value & 0xff);
            array[1] = (byte)((value >> 8) & 0xff);
            array[2] = (byte)((value >> 16) & 0xff);
            array[3] = (byte)((value >> 24) & 0xff);
        }

        public static unsafe int BytesToInt32(byte[] array, int offset)
        {
            return 
                array[offset + 3] | 
                (array[offset + 2] << 8) | 
                (array[offset + 1] << 16) | 
                (array[offset + 0] << 24);
        }
    }
}

//XDay