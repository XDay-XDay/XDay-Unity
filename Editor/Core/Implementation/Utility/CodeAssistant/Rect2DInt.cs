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
    public struct Rect2DInt
    {
        public static Rect2DInt empty = new Rect2DInt(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);

        public Rect2DInt(int minX, int minY, int maxX, int maxY)
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

        public int width { get { return maxX - minX + 1; } }
        public int height { get { return maxY - minY + 1; } }
        public Vector3 center { get { return new Vector3((minX + maxX) * 0.5f, 0, (minY + maxY) * 0.5f); } }
        public static bool operator ==(Rect2DInt a, Rect2DInt b)
        {
            return a.minX == b.minX && a.minY == b.minY && a.maxX == b.maxX && a.maxY == b.maxY;
        }
        public static bool operator !=(Rect2DInt a, Rect2DInt b)
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

        public bool Contains(int x, int y)
        {
            if (x >= minX && x <= maxX && y >= minY && y <= maxY)
            {
                return true;
            }
            return false;
        }

        public static bool Intersect(Rect2DInt a, Rect2DInt b)
        {
            if (a.minX > b.maxX || a.minY > b.maxY || b.minX > a.maxX || b.minY > a.maxY)
            {
                return false;
            }
            return true;
        }

        public bool Intersect(int minX, int minY, int maxX, int maxY)
        {
            if (minX > this.maxX || minY > this.maxY || this.minX > maxX || this.minY > maxY)
            {
                return false;
            }
            return true;
        }

        public void Add(int x, int y, int width, int height)
        {
            Add(new Rect2DInt(x, y, x + width - 1, y + height - 1));
        }

        public void Add(Rect2DInt f)
        {
            minX = Mathf.Min(minX, f.minX);
            minY = Mathf.Min(minY, f.minY);
            maxX = Mathf.Max(maxX, f.maxX);
            maxY = Mathf.Max(maxY, f.maxY);
        }

        public void Add(int x, int y)
        {
            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x);
            maxY = Mathf.Max(maxY, y);
        }

        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
    }
}

