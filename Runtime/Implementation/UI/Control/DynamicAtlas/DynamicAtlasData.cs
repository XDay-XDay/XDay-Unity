using UnityEngine;

namespace XDay.GUIAPI
{
    internal class SaveTextureData
    {
        public int texIndex = -1;
        public int referenceCount = 0;
        public Rect rect;
        //为了对齐而产生的偏移
        public Vector2 alignmentMinOffset;
    }

    internal class GetTextureData
    {
        public string name;
        public OnCallBackTexRect callback;
    }

    internal class IntegerRectangle
    {
        public int x;
        public int y;
        public int width;
        public int height;

        public int Right => x + width;
        public int Top => y + height;
        public int Size => width * height;

        public IntegerRectangle(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public override string ToString()
        {
            return string.Format("x{0}_y:{1}_width:{2}_height{3}_top:{4}_right{5}", x, y, width, height, Top, Right);
        }
    }
}