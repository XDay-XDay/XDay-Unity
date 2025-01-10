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
using UnityEngine;

namespace XDay.AnimationAPI
{
    public enum AnimationType
    {
        Rig,
        Vertex,
    }

    /// <summary>
    /// baked animation using mesh renderer
    /// </summary>
    public interface IGameObjectAnimator
    {
        IGameObjectAnimator Create(GameObject gameObject);

        /// <summary>
        /// play animation
        /// </summary>
        /// <param name="name"></param>
        /// <param name="alwaysPlay">always play animation no matter what current animation is</param>
        void Play(string name, bool alwaysPlay = false);

        /// <summary>
        /// is playing animation
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool IsPlaying(string name);
    }

    /// <summary>
    /// baked animation using batch renderer group
    /// </summary>
    public interface IInstanceAnimator
    {
        /// <summary>
        /// unique id
        /// </summary>
        int ID { get; }

        /// <summary>
        /// a set of animators can be transformed together
        /// </summary>
        IInstanceAnimatorSet Set { set; }
        Vector3 WorldPosition { get; set; }
        Vector3 WorldScale { get; }
        Quaternion WorldRotation { get; set; }
        Vector3 LocalPosition { get; set; }
        Vector3 LocalScale { get; set; }
        Quaternion LocalRotation { get; set; }

        /// <summary>
        /// play animation
        /// </summary>
        /// <param name="name"></param>
        /// <param name="alwaysPlay"></param>
        void Play(string name, bool alwaysPlay = false);
    }

    /// <summary>
    /// manage instanced animators
    /// </summary>
    public interface IInstanceAnimatorManager
    {
        static IInstanceAnimatorManager Create(Func<string, InstanceAnimatorData> creator = null)
        {
            return new InstanceAnimatorManager(creator);
        }

        void OnDestroy();
        void Update();

        /// <summary>
        /// get instance animator
        /// </summary>
        /// <param name="id">animator id</param>
        /// <returns></returns>
        IInstanceAnimator QueryInstance(int id);

        /// <summary>
        /// create instance animator
        /// </summary>
        /// <param name="path">path to InstanceAnimatorData object</param>
        /// <param name="pos">world position</param>
        /// <param name="scale">world scale</param>
        /// <param name="rot">world rotation</param>
        /// <returns></returns>
        IInstanceAnimator CreateInstance(string path, Vector3 pos, Vector3 scale, Quaternion rot);

        /// <summary>
        /// create instance animator
        /// </summary>
        /// <param name="data">baked animator data</param>
        /// <param name="pos">world position</param>
        /// <param name="scale">world scale</param>
        /// <param name="rot">world rotation</param>
        /// <returns></returns>
        IInstanceAnimator CreateInstance(InstanceAnimatorData data, Vector3 pos, Vector3 scale, Quaternion rot);

        /// <summary>
        /// create instance animator
        /// </summary>
        /// <param name="path">path to InstanceAnimatorData object</param>
        /// <returns></returns>
        IInstanceAnimator CreateInstance(string path);

        /// <summary>
        /// destroy instance animator
        /// </summary>
        /// <param name="id">animator id</param>
        void DestroyInstance(int id);
    }

    /// <summary>
    /// group a bunch of animators together, these animators can be transformed together
    /// </summary>
    public interface IInstanceAnimatorSet
    {
        static IInstanceAnimatorSet Create()
        {
            return new InstanceAnimatorSet();
        }

        /// <summary>
        /// set world position
        /// </summary>
        Vector3 WorldPosition { set; }

        /// <summary>
        /// set world rotation
        /// </summary>
        Quaternion WorldRotation { set; }
    }
}
