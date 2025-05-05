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

#if UNITY_EDITOR_WIN

using NVIDIA.PhysX;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor
{
    public class PhysxTerrainCollider
    {
        public void OnDestroy()
        {
            mHeightField?.release();
            mHeightField = null;
            mTerrainActor?.release();
            mTerrainActor = null;
        }

        public void Init(Vector3 pos, int resolution, float tileWidth, float tileHeight, float[] heights, float heightRange)
        {
            var physics = PhysxSetup.engine.physics;
            // Create PxRigidStatic for ground
            mTerrainActor = physics.createRigidStatic(new PxTransform(PxIDENTITY.PxIdentity));

            mHeightRange = heightRange;

            int n = heights.Length;
            var samples = new PxHeightFieldSample[n];
            int k = 0;
            for (int i = 0; i < resolution + 1; ++i)
            {
                for (int j = 0; j < resolution + 1; ++j)
                {
                    PxHeightFieldSample sample = new PxHeightFieldSample();
                    int idx = j * ((int)resolution + 1) + i;
                    sample.height = (short)(heights[idx] * 0x7fff);
                    sample.materialIndex0 = sample.materialIndex1 = 0;
                    sample.setTessFlag();
                    samples[k] = sample;
                    ++k;
                }
            }

            // Managed memory should be pinned before passing it to native function
            var pinSamples = GCHandle.Alloc(samples, GCHandleType.Pinned);

            // Allocate and initialize PxHeightFieldDesc
            var desc = new PxHeightFieldDesc();
            desc.nbRows = (uint)resolution + 1;
            desc.nbColumns = (uint)resolution + 1;
            desc.samples.stride = (uint)Marshal.SizeOf<PxHeightFieldSample>();
            desc.samples.data = Marshal.UnsafeAddrOfPinnedArrayElement(samples, 0);

            // Create PxHeightField
            mHeightField = PhysxSetup.engine.cooking.createHeightField(desc, physics.getPhysicsInsertionCallback());
            // Unpin managed memory
            pinSamples.Free();

            // Add height field shape
            float scale = mHeightRange / 0x7fff;
            if (scale == 0)
            {
                scale = 1.0f;
            }
            mTerrainActor.createExclusiveShape(new PxHeightFieldGeometry(mHeightField, scale, tileHeight / resolution, tileWidth / resolution), PhysxSetup.engine.material);
            mTerrainActor.getShape(0).setLocalPose(new PxTransform(pos.x, 0, pos.z));

            // Add ground actor to scene
            PhysxSetup.engine.scene.addActor(mTerrainActor);
        }

        public void UpdateHeights(float[] heights, int resolution, float tileWidth, float tileHeight, float heightRange)
        {
            var physics = PhysxSetup.engine.physics;
            mHeightRange = heightRange;

            int n = heights.Length;
            var samples = new PxHeightFieldSample[n];
            int k = 0;
            int r1 = resolution + 1;
            for (int i = 0; i < r1; ++i)
            {
                for (int j = 0; j < r1; ++j)
                {
                    PxHeightFieldSample sample = new PxHeightFieldSample();
                    int idx = j * r1 + i;
                    sample.height = (short)(heights[idx] * 0x7fff);
                    sample.materialIndex0 = sample.materialIndex1 = 0;
                    sample.setTessFlag();
                    samples[k] = sample;
                    ++k;
                }
            }

            // Managed memory should be pinned before passing it to native function
            var pinSamples = GCHandle.Alloc(samples, GCHandleType.Pinned);

            // Allocate and initialize PxHeightFieldDesc
            var desc = new PxHeightFieldDesc();
            desc.nbRows = (uint)resolution + 1;
            desc.nbColumns = (uint)resolution + 1;
            desc.samples.stride = (uint)Marshal.SizeOf<PxHeightFieldSample>();
            desc.samples.data = Marshal.UnsafeAddrOfPinnedArrayElement(samples, 0);

            // Unpin managed memory
            pinSamples.Free();

            mHeightField.modifySamples(0, 0, desc);

            float scale = mHeightRange / 0x7fff;
            if (scale == 0)
            {
                scale = 1.0f;
            }

            mTerrainActor.getShape(0).setGeometry(new PxHeightFieldGeometry(mHeightField, scale, tileHeight / resolution, tileWidth / resolution));
        }

        public void CreateDebugObject(int x, int y, int resolution, float tileWidth, float tileHeight)
        {
            var obj = new GameObject("debug tile");
            var renderer = obj.AddComponent<MeshRenderer>();
            var filter = obj.AddComponent<MeshFilter>();
            var mesh = new Mesh();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = new Material(Shader.Find("Unlit/Color"));
            int r = resolution + 1;
            var vertices = new Vector3[r * r];
            var pos = new Vector3(x * tileWidth, 0, tileHeight * y);
            obj.transform.position = pos;

            List<int> indices = new List<int>();
            float gridWidth = tileWidth / resolution;
            float gridHeight = tileHeight / resolution;
            float scale = mHeightRange / 0x7fff;
            if (scale == 0)
            {
                scale = 1.0f;
            }
            for (int i = 0; i < r; ++i)
            {
                for (int j = 0; j < r; ++j)
                {
                    float height = mHeightField.getHeight(j, i);
                    vertices[i * r + j] = new Vector3(j * gridWidth, height * scale, i * gridHeight);

                    if (i != r - 1 && j != r - 1)
                    {
                        int v0 = i * r + j;
                        int v1 = (i + 1) * r + j;
                        int v2 = v1 + 1;
                        int v3 = v0 + 1;
                        indices.Add(v0);
                        indices.Add(v1);
                        indices.Add(v2);
                        indices.Add(v0);
                        indices.Add(v2);
                        indices.Add(v3);
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }

        PxRigidStatic mTerrainActor;
        PxHeightField mHeightField;
        float mHeightRange;
    }
}

#endif
