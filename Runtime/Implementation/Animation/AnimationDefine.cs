/*
 * Copyright (c) 2024 XDay
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

namespace XDay.AnimationAPI
{
    public static class AnimationDefine
    {
        public const int ANIM_FRAME_DATA_PROPERTY_INDEX = 0;
        public const int ANIM_PLAY_STATE_PROPERTY_INDEX = 1;

        public const int MAX_TEXTURE_SIZE = 4096 * 2;
        public const string ANIM_PLAY_NAME = "_AnimPlayState";
        public const string ANIM_FRAME_NAME = "_AnimFrameData";
        public const string ANIM_TEXTURE_NAME = "_AnimationTexture";
        public const string ANIM_SAMPLE_NAME = "Animation Sample";
        public static int ANIM_PLAY_ID = Shader.PropertyToID(ANIM_PLAY_NAME);
        public static int ANIM_FRAME_ID = Shader.PropertyToID(ANIM_FRAME_NAME);
    }
}


//XDay