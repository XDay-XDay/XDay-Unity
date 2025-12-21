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

namespace XDay.SystemAPI
{
    /// <summary>
    /// System按Group创建和卸载
    /// </summary>
    public interface ISystemGroup
    {
    }

    public interface IGlobalSystemGroup : ISystemGroup
    {
    }

    /// <summary>
    /// System创建时机
    /// </summary>
    public enum SystemCreateTiming
    {
        /// <summary>
        /// 系统启动时创建
        /// </summary>
        CreateOnStartup,

        /// <summary>
        /// 使用时创建
        /// </summary>
        LazyCreate,
    }

    public interface ISystem
    {
        void OnCreate(object data);
        void OnDestroy();
    }

    public interface IUpdatable
    {
        void Update(float dt);
    }

    public interface IFixedUpdatable
    {
        void FixedUpdate();
    }

    public interface ILateUpdatable
    {
        void LateUpdate(float dt);
    }

    public interface ISaveable
    {
        void Save(object data);
    }
}
