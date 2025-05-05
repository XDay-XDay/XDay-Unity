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


using UnityEngine;

namespace XDay.UtilityAPI.Editor.CodeAssistant
{
    public struct Rect2D
    {
        public static Rect2D empty = new Rect2D(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

        public Rect2D(float minX, float minY, float maxX, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
        }

        public bool IsEmpty()
        {
            return maxX < minX || maxY < minY;
        }

        public float width { get { return maxX - minX; } }
        public float height { get { return maxY - minY; } }
        public Vector3 center { get { return new Vector3((minX + maxX) * 0.5f, 0, (minY + maxY) * 0.5f); } }
        public static bool operator == (Rect2D a, Rect2D b)
        {
            return Mathf.Approximately(a.minX, b.minX) && 
                Mathf.Approximately(a.minY, b.minY) &&
                Mathf.Approximately(a.maxX, b.maxX) && 
                Mathf.Approximately(a.maxY, b.maxY);
        }
        public static bool operator !=(Rect2D a, Rect2D b)
        {
            return !(a == b);
        }
        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}", minX, minY, maxX, maxY);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Contains(float x, float y)
        {
            if (x >= minX && x <= maxX && y >= minY && y <= maxY)
            {
                return true;
            }
            return false;
        }

        public static bool Intersect(Rect2D a, Rect2D b)
        {
            if (a.minX > b.maxX || a.minY > b.maxY || b.minX > a.maxX || b.minY > a.maxY)
            {
                return false;
            }
            return true;
        }

        public bool Intersect(float minX, float minY, float maxX, float maxY)
        {
            if (minX > this.maxX || minY > this.maxY || this.minX > maxX || this.minY > maxY)
            {
                return false;
            }
            return true;
        }

        public void Add(float x, float y, float width, float height)
        {
            Add(new Rect2D(x, y, x + width, y + height));
        }

        public void Add(Rect2D f)
        {
            minX = Mathf.Min(minX, f.minX);
            minY = Mathf.Min(minY, f.minY);
            maxX = Mathf.Max(maxX, f.maxX);
            maxY = Mathf.Max(maxY, f.maxY);
        }

        public void Add(float x, float y)
        {
            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x);
            maxY = Mathf.Max(maxY, y);
        }

        public float minX;
        public float minY;
        public float maxX;
        public float maxY;
    }
}

