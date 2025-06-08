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
using System.Threading;
using UnityEngine;

namespace XDay.NavigationAPI
{
    /*
     * 两种线程模式
     * 1:由框架提供一个寻路线程,其他线程提交FindPath Request放到寻路线程的request队列中,寻路线程按FIFO处理Request,并在主线程调用OnFinishPathFinding Callback
     * 2.框架不创建寻路线程,由用户自己管理线程,调用FindPath的线程需要提供一个ThreadContext,多个ThreadContext可以并行寻路
     */
    public partial class MyNavigationSystem
    {
        public struct PathFindingRequest
        {
            public PathFindingRequest(int id, Vector3 start, Vector3 end, List<Vector3> result, PathFindingOption options, System.Action<PathFindingRequest> onPathFindingFinish)
            {
                this.id = id;
                this.start = start;
                this.end = end;
                this.result = result;
                this.onPathFindingFinish = onPathFindingFinish;
                this.options = options;
            }

            public int id { get; }
            public Vector3 start { get; }
            public Vector3 end { get; }
            public List<Vector3> result { get; }
            public System.Action<PathFindingRequest> onPathFindingFinish { get; }
            public PathFindingOption options { get; }
        }

        //使用模式1,创建寻路线程
        public void CreatePathFindingThread()
        {
            Debug.Log("CreatePathFindingThread");
            mQuit = false;
            mRequests = new List<PathFindingRequest>();
            mRequestSemaphore = new AutoResetEvent(false);
            mThread = new Thread(ThreadRun);
            mThread.Start();
        }

        public void End()
        {
            if (mRequestSemaphore != null)
            {
                mQuit = true;
                mRequestSemaphore.Set();
                Thread.Yield();
                mThread = null;
            }
        }

        //called in main thread
        public void RequestFindPath(int id, Vector3 start, Vector3 end, List<Vector3> result, PathFindingOption options, System.Action<PathFindingRequest> onFinishPathFinding)
        {
            if (mThread == null)
            {
                CreatePathFindingThread();
            }

            var request = new PathFindingRequest(id, start, end, result, options, onFinishPathFinding);

            lock (mRequests)
            {
                mRequests.Add(request);
                //Debug.Log($"Add request {id}");
            }
            mRequestSemaphore.Set();
        }

        void ThreadRun()
        {
            while (!mQuit)
            {
                mRequestSemaphore.WaitOne();

                while (true)
                {
                    PathFindingRequest request = new PathFindingRequest();
                    lock (mRequests)
                    {
                        if (mRequests.Count > 0)
                        {
                            request = mRequests[mRequests.Count - 1];
                            mRequests.RemoveAt(mRequests.Count - 1);
                        }
                    }
                    if (request.result != null)
                    {
                        ExecuteRequest(request);
                    }
                    else
                    {
                        Debug.Log("request is nil");
                        break;
                    }
                }
            }

            mRequestSemaphore.Dispose();
            mRequestSemaphore = null;

            UnityEngine.Debug.Log("Path Finding Thread exit");
        }

        //在主线程触发callbacks
        public void UpdateInMainThread()
        {
            mTempQueue.Clear();
            lock (mFinishCallbacks)
            {
                //把callback拷到临时buffer,释放锁
                mTempQueue.AddRange(mFinishCallbacks);
                mFinishCallbacks.Clear();
            }
            for (int i = 0; i < mTempQueue.Count; ++i)
            {
                mTempQueue[i].onPathFindingFinish(mTempQueue[i]);
            }
        }

        void ExecuteRequest(PathFindingRequest request)
        {
            //Debug.LogError($"execute request: {request.id}");

            FindPath(request.start, request.end, request.result, null, request.options);
            lock (mFinishCallbacks)
            {
                mFinishCallbacks.Add(request);
            }
        }

        Thread mThread;
        AutoResetEvent mRequestSemaphore;
        List<PathFindingRequest> mRequests = new List<PathFindingRequest>();
        List<PathFindingRequest> mFinishCallbacks = new List<PathFindingRequest>();
        List<PathFindingRequest> mTempQueue = new List<PathFindingRequest>();
        bool mQuit;
    }
}
