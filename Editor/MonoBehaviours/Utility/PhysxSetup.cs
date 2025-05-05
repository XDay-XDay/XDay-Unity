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
    [ExecuteInEditMode]
    public class PhysxSetup : MonoBehaviour
    {
#if UNITY_EDITOR_WIN
        public static PhysxSetup Setup { get { return m_Setup; } }
        public PxFoundation Foundation { get { return m_Foundation; } }
        public PxPhysics Physics { get { return m_Physics; } }
        public PxMaterial Material { get { return m_PhysicsMaterial; } }
        public PxScene Scene { get { return m_Scene; } }
        public PxCooking Cooking { get { return m_Cooking; } }
#endif

        public bool Raycast(Vector3 origin, Vector3 dir, out Vector3 pos, out Vector3 normal)
        {
            pos = Vector3.zero;
            normal = Vector3.up;
#if UNITY_EDITOR_WIN
            if (m_Scene.raycast(origin.ToPxVec3(), dir.ToPxVec3(), 100000, m_RaycastBuffer, PxHitFlag.DEFAULT | PxHitFlag.MESH_BOTH_SIDES))
            {
                if (m_RaycastBuffer.hasBlock)
                {
                    pos = m_RaycastBuffer.block.position.ToVector3();
                    normal = m_RaycastBuffer.block.normal.ToVector3();
                    return true;
                }
            }
#endif
            return false;
        }

        private void Awake()
        {
            m_Setup = this;
#if UNITY_EDITOR_WIN
            m_ErrorCallback = new PxErrorToExceptionCallback();
            m_AllocatorCallback = new PxDefaultAllocator();
            m_Foundation = PxFoundation.create(PxVersion.PX_PHYSICS_VERSION, m_AllocatorCallback, m_ErrorCallback);

            m_Pvd = m_Foundation.createPvd();
            m_PvdTransport = PxPvdTransport.create("localhost", 5425, 200);
            if (!m_Pvd.connect(m_PvdTransport, PxPvdInstrumentationFlag.ALL))
            {
                m_Pvd.release();
                m_Pvd = null;
                m_PvdTransport.release();
                m_PvdTransport = null;
            }
            else
                Debug.Log("Connected to PVD.");

            m_Physics = m_Foundation.createPhysics(PxVersion.PX_PHYSICS_VERSION, new PxTolerancesScale(), m_Pvd);

            m_DefaultCpuDispatcher = PxDefaultCpuDispatcher.create((uint)SystemInfo.processorCount);

            var sceneDesc = new PxSceneDesc(m_Physics.getTolerancesScale())
            {
                cpuDispatcher = m_DefaultCpuDispatcher,
                filterShader = PxDefaultSimulationFilterShader.function,
                gravity = new PxVec3(0, -9.8f, 0),
                broadPhaseType = PxBroadPhaseType.ABP
            };
            m_Scene = m_Physics.createScene(sceneDesc);
            sceneDesc.destroy();

            var pvdClient = m_Scene.getScenePvdClient();
            pvdClient?.setScenePvdFlags(PxPvdSceneFlag.TRANSMIT_CONTACTS | PxPvdSceneFlag.TRANSMIT_CONSTRAINTS | PxPvdSceneFlag.TRANSMIT_SCENEQUERIES);

            m_Cooking = m_Foundation.createCooking(PxVersion.PX_PHYSICS_VERSION, new PxCookingParams(m_Physics.getTolerancesScale()));
            m_PhysicsMaterial = m_Physics.createMaterial(0.5f, 0.5f, 0.05f);
#endif
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR_WIN
            m_Scene?.release();
            m_Scene = null;
            m_PhysicsMaterial?.release();
            m_PhysicsMaterial = null;
            m_DefaultCpuDispatcher?.release();
            m_DefaultCpuDispatcher = null;
            m_Cooking?.release();
            m_Cooking = null;
            
            m_Physics?.release();
            m_Physics = null;
            m_Pvd?.release();
            m_Pvd = null;
            m_PvdTransport?.release();
            m_PvdTransport = null;
            m_Foundation?.release();
            m_Foundation = null;
            m_AllocatorCallback?.destroy();
            m_AllocatorCallback = null;
            m_ErrorCallback?.destroy();
            m_ErrorCallback = null;
#endif
        }

        private void FixedUpdate()
        {
#if UNITY_EDITOR_WIN
            if (m_Scene != null)
            {
                m_Scene.simulate(Time.fixedDeltaTime);
                m_Scene.fetchResults(true);
            }
#endif
        }

        private void BeforeReload()
        {
            OnDestroy();
        }

#if UNITY_EDITOR_WIN
        private PxPhysics m_Physics;
        private PxFoundation m_Foundation;
        private PxPvdTransport m_PvdTransport;
        private PxRaycastBuffer m_RaycastBuffer = new PxRaycastBuffer();
        private PxAllocatorCallback m_AllocatorCallback;
        private PxDefaultCpuDispatcher m_DefaultCpuDispatcher;
        private PxCooking m_Cooking;
        private PxMaterial m_PhysicsMaterial;
        private PxScene m_Scene;
        private PxErrorCallback m_ErrorCallback;
        private PxPvd m_Pvd;
#endif
        private static PhysxSetup m_Setup;
    }
}

#endif