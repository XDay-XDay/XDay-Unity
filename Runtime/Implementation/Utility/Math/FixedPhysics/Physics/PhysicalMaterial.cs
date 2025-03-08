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
    public class PhysicalMaterial
    {
        public FixedPoint Density
        {
            set
            {
                if (value < PhysicsWorld.MinDensity)
                {
                    Log.Instance?.Error($"Density is too small. Min density is {PhysicsWorld.MinDensity}");
                    return;
                }

                if (value > PhysicsWorld.MaxDensity)
                {
                    Log.Instance?.Error($"Density is too large. Max density is {PhysicsWorld.MaxDensity}");
                    return;
                }

                m_Density = value;
            }
            get => m_Density;
        }

        public FixedPoint Restitution
        {
            set => m_Restitution = FixedMath.Clamp(value, FixedPoint.Zero, FixedPoint.One);
            get => m_Restitution;
        }

        public FixedPoint StaticFriction
        {
            set => m_StaticFriction = FixedMath.Clamp(value, FixedPoint.Zero, FixedPoint.One);
            get => m_StaticFriction;
        }

        public FixedPoint DynamicFriction
        {
            set => m_DynamicFriction = FixedMath.Clamp(value, FixedPoint.Zero, FixedPoint.One);
            get => m_DynamicFriction;
        }

        private FixedPoint m_Density = 1;
        private FixedPoint m_Restitution = new(0.5f);
        private FixedPoint m_StaticFriction = new(0.6f);
        private FixedPoint m_DynamicFriction = new(0.4f);
    }
}
