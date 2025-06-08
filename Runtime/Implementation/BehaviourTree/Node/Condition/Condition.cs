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

namespace XDay.BehaviourTreeAPI
{
    public abstract class Condition : Node
    {
        public Condition(int id, string name, BehaviourTree tree)
            : base(id, name, tree)
        {
        }

        protected override NodeStatus OnTick()
        {
            var status = TestSuccess();
            if (status == NodeStatus.Success)
            {
                return Complete(true);
            }
            else if (status == NodeStatus.Fail)
            {
                return Complete(false);
            }
            else
            {
                Debug.Assert(false, "不能返回其他状态");
                return Complete(false);
            }
        }

        //返回成功或失败
        public abstract NodeStatus TestSuccess();

        public override bool CanExecute()
        {
            return false;
        }
    }
}
