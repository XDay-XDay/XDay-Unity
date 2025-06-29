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
using FixMath.NET;
using UnityEngine;

class BepuHelper
{
    public static UnityEngine.Vector3 ToUnityVector3(BEPUutilities.Vector3 v)
    {
        return new UnityEngine.Vector3((float)v.X, (float)v.Y, (float)v.Z);
    }

    public static UnityEngine.Quaternion ToUnityQuaternion(BEPUutilities.Quaternion v)
    {
        float x = (float)Math.Round((float)v.X, 4);
        float y = (float)Math.Round((float)v.Y, 4);
        float z = (float)Math.Round((float)v.Z, 4);
        float w = (float)Math.Round((float)v.W, 4);
        if (!Mathf.Approximately(x, 0) ||
            !Mathf.Approximately(y, 0) ||
            !Mathf.Approximately(z, 0) ||
            !Mathf.Approximately(w, 1))
        {
            int a = 1;
        }
        return new UnityEngine.Quaternion(x, y, z, w);
    }

    public static BEPUutilities.Vector3 ToBEPUVector3(UnityEngine.Vector3 v)
    {
        return new BEPUutilities.Vector3((Fix64)v.x, (Fix64)v.y, (Fix64)v.z);
    }
}
