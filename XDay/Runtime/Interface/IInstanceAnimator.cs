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


using System;
using UnityEngine;

namespace XDay.AnimationAPI
{
    public enum AnimationType
    {
        Rig,
        Vertex,
    }

    public interface IGameObjectAnimator
    {
        IGameObjectAnimator Create(GameObject gameObject);
        void Play(string name, bool alwaysPlay = false);
        bool IsPlaying(string name);
    }

    public interface IInstanceAnimator
    {
        int ID { get; }
        IInstanceAnimatorSet Set { set; }
        Vector3 WorldPosition { get; set; }
        Vector3 WorldScale { get; }
        Quaternion WorldRotation { get; set; }
        Vector3 LocalPosition { get; set; }
        Vector3 LocalScale { get; set; }
        Quaternion LocalRotation { get; set; }

        void Update();
        void PlayAnimation(string name, bool alwaysPlay = false);
        void Dirty();
    }

    public interface IInstanceAnimatorManager
    {
        static IInstanceAnimatorManager Create(Func<string, InstanceAnimatorData> creator)
        {
            return new InstanceAnimatorManager(creator);
        }

        void OnDestroy();
        void Update();
        IInstanceAnimator QueryInstance(int id);
        IInstanceAnimator CreateInstance(string path, Vector3 pos, Vector3 scale, Quaternion rot);
        IInstanceAnimator CreateInstance(string path);
        void DestroyInstance(int id);
    }

    public interface IInstanceAnimatorSet
    {
        static IInstanceAnimatorSet Create()
        {
            return new InstanceAnimatorSet();
        }

        Vector3 WorldPosition { set; }
        Quaternion WorldRotation { set; }

        Vector3 InverseTransform(Vector3 worldPos);
        Quaternion InverseTransform(Quaternion worldRot);
        Vector3 Transform(Vector3 localPos);
        Quaternion Transform(Quaternion localRot);
        void RemoveAnimator(IInstanceAnimator animator);
        void AddAnimator(IInstanceAnimator animator);
    }
}
