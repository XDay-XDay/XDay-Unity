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
#if UNITY_EDITOR


using NVIDIA.PhysX;
using NVIDIA.PhysX.UnityExtensions;
using UnityEngine;
using UnityEditor;

namespace XDay.UtilityAPI
{
    //设置PhysX,可以直接使用PhysX的接口,不受Unity限制
    [ExecuteInEditMode]
    public class PhysxSetup : MonoBehaviour
    {
        void Awake()
        {
            mEngine = this;
#if UNITY_EDITOR_WIN
            // Create PxFoundation
            mAllocatorCallback = new PxDefaultAllocator();
            mErrorCallback = new PxErrorToExceptionCallback();
            mFoundation = PxFoundation.create(PxVersion.PX_PHYSICS_VERSION, mAllocatorCallback, mErrorCallback);

            // Setup PVD connection (optional)
            mPvd = mFoundation.createPvd();
            mPvdTransport = PxPvdTransport.create("localhost", 5425, 200);
            if (!mPvd.connect(mPvdTransport, PxPvdInstrumentationFlag.ALL))
            {
                mPvd.release();
                mPvd = null;
                mPvdTransport.release();
                mPvdTransport = null;
            }
            else
                Debug.Log("Connected to PVD.");

            // Create PxPhysics
            mPhysics = mFoundation.createPhysics(PxVersion.PX_PHYSICS_VERSION, new PxTolerancesScale(), mPvd);

            // Create CPU dispatcher
            mCpuDispatcher = PxDefaultCpuDispatcher.create((uint)SystemInfo.processorCount);

            // Create PxScene
            var sceneDesc = new PxSceneDesc(mPhysics.getTolerancesScale());
            sceneDesc.cpuDispatcher = mCpuDispatcher;
            sceneDesc.filterShader = PxDefaultSimulationFilterShader.function;
            sceneDesc.gravity = new PxVec3(0, -9.8f, 0);
            sceneDesc.broadPhaseType = PxBroadPhaseType.ABP;
            mScene = mPhysics.createScene(sceneDesc);
            sceneDesc.destroy();

            // Set PVD flags
            var pvdClient = mScene.getScenePvdClient();
            if (pvdClient != null)
                pvdClient.setScenePvdFlags(PxPvdSceneFlag.TRANSMIT_CONTACTS | PxPvdSceneFlag.TRANSMIT_CONSTRAINTS | PxPvdSceneFlag.TRANSMIT_SCENEQUERIES);

            mPhysicsMaterial = mPhysics.createMaterial(0.5f, 0.5f, 0.05f);

            // Create PxCooking
            mCooking = mFoundation.createCooking(PxVersion.PX_PHYSICS_VERSION, new PxCookingParams(mPhysics.getTolerancesScale()));
#endif
            //reload之前先清理状态
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        }

        void BeforeReload()
        {
            OnDestroy();
        }

        void OnDestroy()
        {
#if UNITY_EDITOR_WIN
            mScene?.release();
            mScene = null;
            mPhysicsMaterial?.release();
            mPhysicsMaterial = null;
            mCpuDispatcher?.release();
            mCpuDispatcher = null;
            mCooking?.release();
            mCooking = null;
            
            mPhysics?.release();
            mPhysics = null;
            mPvd?.release();
            mPvd = null;
            mPvdTransport?.release();
            mPvdTransport = null;
            mFoundation?.release();
            mFoundation = null;
            mErrorCallback?.destroy();
            mErrorCallback = null;
            mAllocatorCallback?.destroy();
            mAllocatorCallback = null;
#endif
        }

        void FixedUpdate()
        {
#if UNITY_EDITOR_WIN
            if (mScene != null)
            {
                mScene.simulate(Time.fixedDeltaTime);
                mScene.fetchResults(true);
            }
#endif
        }

        public bool Raycast(Vector3 origin, Vector3 dir, out Vector3 pos, out Vector3 normal)
        {
            pos = Vector3.zero;
            normal = Vector3.up;
#if UNITY_EDITOR_WIN
            if (mScene.raycast(origin.ToPxVec3(), dir.ToPxVec3(), 100000, mBuffer, PxHitFlag.DEFAULT | PxHitFlag.MESH_BOTH_SIDES))
            {
                if (mBuffer.hasBlock)
                {
                    pos = mBuffer.block.position.ToVector3();
                    normal = mBuffer.block.normal.ToVector3();
                    return true;
                }
            }
#endif
            return false;
        }

#if UNITY_EDITOR_WIN
        public static PhysxSetup engine { get { return mEngine; } }
        public PxFoundation foundation { get { return mFoundation; } }
        public PxPhysics physics { get { return mPhysics; } }
        public PxMaterial material { get { return mPhysicsMaterial; } }
        public PxScene scene { get { return mScene; } }
        public PxCooking cooking { get { return mCooking; } }

        PxAllocatorCallback mAllocatorCallback;
        PxErrorCallback mErrorCallback;
        PxFoundation mFoundation;
        PxPhysics mPhysics;
        PxPvdTransport mPvdTransport;
        PxPvd mPvd;
        PxDefaultCpuDispatcher mCpuDispatcher;
        PxScene mScene;
        PxCooking mCooking;
        PxMaterial mPhysicsMaterial;
        PxRaycastBuffer mBuffer = new PxRaycastBuffer();
#endif
        static PhysxSetup mEngine;
    }
}

#endif