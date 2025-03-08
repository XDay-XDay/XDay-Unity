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



namespace XDay
{
    public readonly struct FixedManifold
    {
        public readonly Rigidbody BodyA;
        public readonly Rigidbody BodyB;
        public readonly FixedVector2 Normal;
        public readonly FixedPoint Depth;
        public readonly FixedVector2 Contact1;
        public readonly FixedVector2 Contact2;
        public readonly int ContactCount;

        public FixedManifold(
            Rigidbody bodyA, Rigidbody bodyB, 
            FixedVector2 normal, FixedPoint depth, 
            FixedVector2 contact1, FixedVector2 contact2, int contactCount)
        {
            BodyA = bodyA;
            BodyB = bodyB;
            Normal = normal;
            Depth = depth;
            Contact1 = contact1;
            Contact2 = contact2;
            ContactCount = contactCount;
        }
    }
}
