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



namespace XDay.WorldAPI.Decoration.Editor
{
    internal class DecorationDefine
    {
        public const string LOD_LAYER_MASK_NAME = "LOD Layer";
        public const string LOD_NAME = "LOD";
        public const string LOD_LAYER_LAYER_NAME = "LOD Layer";
        public const string ADD_DECORATION_NAME = "Add Decoration";
        public const string REMOVE_DECORATION_NAME = "Remove Decoration";
        public const string ENABLE_DECORATION_NAME = "Enable Decoration";
        public const string ROTATION_NAME = "Decoration Rotation";
        public const string SCALE_NAME = "Decoration Scale";
        public const string POSITION_NAME = "Decoration Position";
        public const string PATTERN_NAME = "Decoration Pattern Name";
        public const string CREATE_MODE = "DecorationSystem.CreateMode";
        public const string REMOVE_RANGE = "DecorationSystem.RemoveRange";
        public const string CIRCLE_RADIUS = "DecorationSystem.CircleRadius";
        public const string RECT_WIDTH = "DecorationSystem.RectWidth";
        public const string RECT_HEIGHT = "DecorationSystem.RectHeight";
        public const string OBJECT_COUNT = "DecorationSystem.ObjectCount";
        public const string SPACE = "DecorationSystem.Space";
        public const string RANDOM = "DecorationSystem.Random";
        public const string BORDER_SIZE = "DecorationSystem.BorderSize";
        public const string LINE_EQUIDISTANT = "DecorationSystem.LineEquidistant";
    }

    [System.Flags]
    internal enum LODLayerMask : byte
    {
        None = 0,
        LOD0 = 1 << 0,
        LOD1 = 1 << 1,
        LOD2 = 1 << 2,
        LOD3 = 1 << 3,
        LOD4 = 1 << 4,
        LOD5 = 1 << 5,
        LOD6 = 1 << 6,
        LOD7 = 1 << 7,

        AllLOD = 0xff,
    }
}


//XDay