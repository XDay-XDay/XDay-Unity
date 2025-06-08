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
using UnityEditor;
using UnityEngine;

namespace XDay.Terrain.Editor
{
    internal class Erosion : TerrainModifier
    {
        public class Setting : ITerrainModifierSetting
        {
            public int iterateCount = 50000;
            public int radius = 3;
            public float inertia = 0.05f;
            public float depositSpeed = 0.3f;
            public float sedimentCapacityFactor = 4;
            public float minSedimentCapacity = 0.01f;
            public float erodeSpeed = 0.3f;
            public float gravity = 4;
            public float evaporateSpeed = 0.01f;
            public int maxDropletLifetime = 30;
            public float initialWaterVolume = 1.0f;
            public float initialSpeed = 1.0f;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteInt32(iterateCount, "Iterate Count");
                writer.WriteInt32(radius, "Radius");
                writer.WriteSingle(inertia, "Inertia");
                writer.WriteSingle(depositSpeed, "Deposit Speed");
                writer.WriteSingle(sedimentCapacityFactor, "Sediment Capacity Factor");
                writer.WriteSingle(minSedimentCapacity, "Min Sediment Capacity");
                writer.WriteSingle(erodeSpeed, "Erode Speed");
                writer.WriteSingle(gravity, "Gravity Speed");
                writer.WriteSingle(evaporateSpeed, "Evaporate Speed");
                writer.WriteInt32(maxDropletLifetime, "Max Droplet Lifetime");
                writer.WriteSingle(initialWaterVolume, "Initial Water Volume");
                writer.WriteSingle(initialSpeed, "Initial Speed");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                iterateCount = reader.ReadInt32("Iterate Count");
                radius = reader.ReadInt32("Radius");
                inertia = reader.ReadSingle("Inertia");
                depositSpeed = reader.ReadSingle("Deposit Speed");
                sedimentCapacityFactor = reader.ReadSingle("Sediment Capacity Factor");
                minSedimentCapacity = reader.ReadSingle("Min Sediment Capacity");
                erodeSpeed = reader.ReadSingle("Erode Speed");
                gravity = reader.ReadSingle("Gravity Speed");
                evaporateSpeed = reader.ReadSingle("Evaporate Speed");
                maxDropletLifetime = reader.ReadInt32("Max Droplet Lifetime");
                initialWaterVolume = reader.ReadSingle("Initial Water Volume");
                initialSpeed = reader.ReadSingle("Initial Speed");
            }

            private const int m_Version = 1;
        };

        public Erosion(int id) : base(id) { }
        public Erosion() { }

        public override void Execute()
        {
            mHeightData.Clear();
            var heightData = GetInputModifier(0).GetHeightData();
            if (heightData != null)
            {
                mHeightData.AddRange(heightData);
            }

            InitializeBrushIndices();

            SimulateParticles();

            SetMaskDataToHeightMap();
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override string GetName()
        {
            return "Erosion";
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());
            setting.iterateCount = EditorGUILayout.IntField("Iterate Count", setting.iterateCount);
            setting.radius = EditorGUILayout.IntField("Radius", setting.radius);
            setting.inertia = EditorGUILayout.FloatField("Inertia", setting.inertia);
            setting.depositSpeed = EditorGUILayout.FloatField("Deposit Speed", setting.depositSpeed);
            setting.sedimentCapacityFactor = EditorGUILayout.FloatField("Sediment Capacity Factor", setting.sedimentCapacityFactor);
            setting.minSedimentCapacity = EditorGUILayout.FloatField("Minimum Sediment Capacity", setting.minSedimentCapacity);
            setting.erodeSpeed = EditorGUILayout.FloatField("Erode Speed", setting.erodeSpeed);
            setting.gravity = EditorGUILayout.FloatField("Gravity", setting.gravity);
            setting.evaporateSpeed = EditorGUILayout.FloatField("Evaporate Speed", setting.evaporateSpeed);
            setting.maxDropletLifetime = EditorGUILayout.IntField("Maximum Droplet Lifetime", setting.maxDropletLifetime);
            setting.initialWaterVolume = EditorGUILayout.FloatField("Initial Water Volume", setting.initialWaterVolume);
            setting.initialSpeed = EditorGUILayout.FloatField("Initial Speed", setting.initialSpeed);
        }

        private void SimulateParticles()
        {
            var setting = GetSetting() as Setting;
            var size = GetSize();

            for (int iteration = 0; iteration < setting.iterateCount; iteration++)
            {
                // Create water droplet at random point on map
                float posX = Random.value * (size.Resolution - 1);
                float posY = Random.value * (size.Resolution - 1);

                float dirX = 0;
                float dirY = 0;
                float speed = setting.initialSpeed;
                float water = setting.initialWaterVolume;
                float sediment = 0;

                for (int lifetime = 0; lifetime < setting.maxDropletLifetime; lifetime++)
                {
                    int nodeX = (int)posX;
                    int nodeY = (int)posY;
                    int dropletIndex = nodeY * size.Resolution + nodeX;
                    // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
                    float cellOffsetX = posX - nodeX;
                    float cellOffsetY = posY - nodeY;

                    // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
                    HeightAndGradient heightAndGradient = CalculateHeightAndGradient(mHeightData, size.Resolution, posX, posY);

                    // Update the droplet's direction and position (move position 1 unit regardless of speed)
                    dirX = (dirX * setting.inertia - heightAndGradient.gradientX * (1 - setting.inertia));
                    dirY = (dirY * setting.inertia - heightAndGradient.gradientY * (1 - setting.inertia));
                    // Normalize direction
                    float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                    if (len != 0)
                    {
                        dirX /= len;
                        dirY /= len;
                    }
                    posX += dirX;
                    posY += dirY;

                    // Stop simulating droplet if it's not moving or has flowed over edge of map
                    if ((dirX == 0 && dirY == 0) || posX < 0 || posX >= size.Resolution - 1 || posY < 0 || posY >= size.Resolution - 1)
                    {
                        break;
                    }

                    // Find the droplet's new height and calculate the deltaHeight
                    float newHeight = CalculateHeightAndGradient(mHeightData, size.Resolution, posX, posY).height;
                    float deltaHeight = newHeight - heightAndGradient.height;

                    // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
                    float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * setting.sedimentCapacityFactor, setting.minSedimentCapacity);

                    // If carrying more sediment than capacity, or if flowing uphill:
                    if (sediment > sedimentCapacity || deltaHeight > 0)
                    {
                        // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                        float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * setting.depositSpeed;
                        sediment -= amountToDeposit;

                        // Add the sediment to the four nodes of the current cell using bilinear interpolation
                        // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                        mHeightData[dropletIndex] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                        mHeightData[dropletIndex + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                        mHeightData[dropletIndex + size.Resolution] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                        mHeightData[dropletIndex + size.Resolution + 1] += amountToDeposit * cellOffsetX * cellOffsetY;

                    }
                    else
                    {
                        // Erode a fraction of the droplet's current carry capacity.
                        // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                        float amountToErode = Mathf.Min((sedimentCapacity - sediment) * setting.erodeSpeed, -deltaHeight);

                        // Use erosion brush to erode from all nodes inside the droplet's erosion radius
                        for (int brushPointIndex = 0; brushPointIndex < mErosionBrushIndices[dropletIndex].Length; brushPointIndex++)
                        {
                            int nodeIndex = mErosionBrushIndices[dropletIndex][brushPointIndex];
                            float weighedErodeAmount = amountToErode * mErosionBrushWeights[dropletIndex][brushPointIndex];
                            float deltaSediment = (mHeightData[nodeIndex] < weighedErodeAmount) ? mHeightData[nodeIndex] : weighedErodeAmount;
                            mHeightData[nodeIndex] -= deltaSediment;
                            sediment += deltaSediment;
                        }
                    }

                    // Update droplet's speed and water content
                    float s = Mathf.Max(0, speed * speed + deltaHeight * setting.gravity);
                    speed = Mathf.Sqrt(s);
                    water *= (1 - setting.evaporateSpeed);
                }
            }
        }

        private void InitializeBrushIndices()
        {
            var setting = GetSetting() as Setting;
            var size = GetSize();

            mErosionBrushIndices = new int[size.Resolution * size.Resolution][];
            mErosionBrushWeights = new float[size.Resolution * size.Resolution][];

            int radius = setting.radius;
            int[] xOffsets = new int[radius* radius * 4];
            int[] yOffsets = new int[radius* radius * 4];
            float[] weights = new float[radius* radius * 4];
            float weightSum = 0;
            int addIndex = 0;

            for (int i = 0; i < mErosionBrushIndices.Length; i++)
            {
                int centreX = i % size.Resolution;
                int centreY = i / size.Resolution;

                if (centreY <= radius || centreY >= size.Resolution - radius || centreX <= radius + 1 || centreX >= size.Resolution - radius)
                {
                    weightSum = 0;
                    addIndex = 0;
                    for (int y = -radius; y <= radius; y++)
                    {
                        for (int x = -radius; x <= radius; x++)
                        {
                            float sqrDst = (float)(x * x + y * y);
                            if (sqrDst < radius * radius)
                            {
                                int coordX = centreX + x;
                                int coordY = centreY + y;

                                if (coordX >= 0 && coordX < size.Resolution && coordY >= 0 && coordY < size.Resolution)
                                {
                                    float weight = 1 - Mathf.Sqrt(sqrDst) / radius;
                                    weightSum += weight;
                                    weights[addIndex] = weight;
                                    xOffsets[addIndex] = x;
                                    yOffsets[addIndex] = y;
                                    addIndex++;
                                }
                            }
                        }
                    }
                }

                int numEntries = addIndex;
                mErosionBrushIndices[i] = new int[numEntries];
                mErosionBrushWeights[i] = new float[numEntries];

                for (int j = 0; j < numEntries; j++)
                {
                    mErosionBrushIndices[i][j] = (yOffsets[j] + centreY) * size.Resolution + xOffsets[j] + centreX;
                    mErosionBrushWeights[i][j] = weights[j] / weightSum;
                }
            }
        }

        class HeightAndGradient
        {
            public float height;
            public float gradientX;
            public float gradientY;
        };

        private HeightAndGradient CalculateHeightAndGradient(List<float> heightMap, int mapSize, float posX, float posY)
        {
            int coordX = (int)posX;
            int coordY = (int)posY;

            // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
            float x = posX - coordX;
            float y = posY - coordY;

            // Calculate heights of the four nodes of the droplet's cell
            int nodeIndexNW = coordY * mapSize + coordX;
            float heightNW = heightMap[nodeIndexNW];
            float heightNE = heightMap[nodeIndexNW + 1];
            float heightSW = heightMap[nodeIndexNW + mapSize];
            float heightSE = heightMap[nodeIndexNW + mapSize + 1];

            // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
            float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
            float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

            // Calculate height with bilinear interpolation of the heights of the nodes of the cell
            float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

            return new HeightAndGradient()
            {
                height = height,
                gradientX = gradientX,
                gradientY = gradientY,
            };
        }

        // Indices and weights of erosion brush precomputed for every node
        private int[][] mErosionBrushIndices;
        private float[][] mErosionBrushWeights;
    };
}