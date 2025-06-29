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

using System.Collections.Generic;
using UnityEngine;

namespace XDay.WorldAPI.Decoration
{
    /// <summary>
    /// decoration system interface
    /// </summary>
    public interface IDecorationSystem : IWorldPlugin
    {
        /// <summary>
        /// play animation on decoration object
        /// </summary>
        /// <param name="decorationID"></param>
        /// <param name="animationName"></param>
        /// <param name="alwaysPlay"></param>
        void PlayAnimation(int decorationID, string animationName, bool alwaysPlay = false);

        /// <summary>
        /// find decorations in a circle
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="decorationIDs"></param>
        void QueryDecorationIDsInCircle(Vector3 center, float radius, List<int> decorationIDs);

        /// <summary>
        /// show/hide decoration
        /// </summary>
        /// <param name="decorationID"></param>
        /// <param name="show"></param>
        void ShowDecoration(int decorationID, bool show);

        /// <summary>
        /// show/hide decoration in circle
        /// </summary>
        /// <param name="circleCenter"></param>
        /// <param name="circleRadius"></param>
        /// <param name="show"></param>
        void ShowDecoration(Vector3 circleCenter, float circleRadius, bool show);
    }
}
