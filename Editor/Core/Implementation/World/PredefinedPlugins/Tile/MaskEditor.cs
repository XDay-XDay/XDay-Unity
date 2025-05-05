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

using System.Buffers;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile.Editor
{
    internal class MaskEditor
    {
        public delegate int GetGridDataID(int x, int y);
        public delegate void SetGridDataID(int x, int y, int id);
        public delegate Color32 GetGridColor(int id);

        public void Init(string name, int horizontalPixelCount, int verticalPixelCount, float width, float height, Vector3 startPosition, Transform parent, GetGridDataID getID, SetGridDataID setID, GetGridColor getColor, ArrayPool<Color32> arrayPool)
        {
            if (mPlaneObject == null && horizontalPixelCount > 0)
            {
                mHorizontalPixelCount = horizontalPixelCount;
                mVerticalPixelCount = verticalPixelCount;
                mPixelWidth = width / mHorizontalPixelCount;
                mPixelHeight = height / mVerticalPixelCount;
                mGetID = getID;
                mSetID = setID;
                mGetColor = getColor;
                mStartPosition = startPosition;
                mColorArrayPool = arrayPool;
                //create region texture and materials
                mGridTexture = new Texture2D(horizontalPixelCount, verticalPixelCount, TextureFormat.RGBA32, false, false);
                Color32[] gridColors = new Color32[horizontalPixelCount * verticalPixelCount];
                for (int i = 0; i < verticalPixelCount; ++i)
                {
                    for (int j = 0; j < horizontalPixelCount; ++j)
                    {
                        int id = getID(j, i);
                        Color32 color = getColor(id);
                        gridColors[i * horizontalPixelCount + j] = color;
                    }
                }

                mGridTexture.filterMode = FilterMode.Point;
                mGridTexture.SetPixels32(gridColors);
                mGridTexture.Apply();

                //create plane
                mPlaneObject = new GameObject(name);
                mPlaneObject.transform.position = startPosition;
                mPlaneObject.SetActive(true);
                Helper.HideGameObject(mPlaneObject);
                mPlaneObject.transform.parent = parent;
                var meshRenderer = mPlaneObject.AddComponent<MeshRenderer>();
                var meshFilter = mPlaneObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = CreateMesh(width, height);
                meshRenderer.sharedMaterial = CreateMaterial(UnityEngine.Rendering.CompareFunction.Always);
                meshRenderer.sharedMaterial.SetTexture("_MainTex", mGridTexture);
            }
        }

        public void OnDestroy()
        {
            Object.DestroyImmediate(mMesh);
            Object.DestroyImmediate(mMaterial);
            Object.DestroyImmediate(mGridTexture);
            Helper.DestroyUnityObject(mPlaneObject);
        }

        public void SetPixel(Vector3 pos, int brushSize, int id)
        {
            Vector2Int coord = FromWorldPositionToCoordinate(pos);
            int startX = coord.x - brushSize / 2;
            int startY = coord.y - brushSize / 2;
            int endX = startX + brushSize - 1;
            int endY = startY + brushSize - 1;

            if (endX < 0 || endY < 0 || startX >= mHorizontalPixelCount || startY >= mVerticalPixelCount)
            {
                return;
            }

            startX = Mathf.Clamp(startX, 0, mHorizontalPixelCount - 1);
            startY = Mathf.Clamp(startY, 0, mVerticalPixelCount - 1);
            endX = Mathf.Clamp(endX, 0, mHorizontalPixelCount - 1);
            endY = Mathf.Clamp(endY, 0, mVerticalPixelCount - 1);

            int width = endX - startX + 1;
            int height = endY - startY + 1;
            var pixels = mColorArrayPool.Rent(width * height);
            int idx = 0;

            Color32 color = mGetColor(id);
            for (int i = startY; i <= endY; ++i)
            {
                for (int j = startX; j <= endX; ++j)
                {
                    mSetID(j, i, id);
                    pixels[idx] = color;
                    ++idx;
                }
            }
            OnSetPixels(startX, startY, width, height, pixels);
            mColorArrayPool.Return(pixels);
        }

        public Vector2Int FromWorldPositionToCoordinate(Vector3 worldPos)
        {
            var localPos = worldPos - mStartPosition;
            return new Vector2Int(Mathf.FloorToInt(localPos.x / mPixelWidth), Mathf.FloorToInt(localPos.z / mPixelHeight));
        }

        public void Show(bool visible)
        {
            mPlaneObject.SetActive(visible);
        }

        public void RefreshTexture()
        {
            Color32[] colors = new Color32[mVerticalPixelCount * mHorizontalPixelCount];
            Color32 black = new Color32(0, 0, 0, 0);
            int idx = 0;
            for (int i = 0; i < mVerticalPixelCount; ++i)
            {
                for (int j = 0; j < mHorizontalPixelCount; ++j)
                {
                    var id = mGetID(j, i);
                    colors[idx] = mGetColor(id);
                    ++idx;
                }
            }
            OnSetPixels(0, 0, mHorizontalPixelCount, mVerticalPixelCount, colors);
        }

        void OnSetPixels(int x, int y, int width, int height, Color32[] pixels)
        {
            mGridTexture.SetPixels32(x, y, width, height, pixels);
            mGridTexture.Apply();
        }

        Mesh CreateMesh(float mapWidth, float mapHeight)
        {
            if (mMesh == null)
            {
                mMesh = new Mesh();
                mMesh.vertices = new Vector3[]{
                    new Vector3(0, 0, 0),
                    new Vector3(0, 0, mapHeight),
                    new Vector3(mapWidth, 0, mapHeight),
                    new Vector3(mapWidth, 0, 0),
                };
                mMesh.uv = new Vector2[] {
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(1, 0),
                };
                mMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            }
            return mMesh;
        }

        Material CreateMaterial(UnityEngine.Rendering.CompareFunction zTest)
        {
            if (mMaterial == null)
            {
                mMaterial = new Material(Shader.Find("SLGMaker/DiffuseTransparent"));
                mMaterial.SetFloat("_ZTest", (float)zTest);
            }
            return mMaterial;
        }

        int mHorizontalPixelCount;
        int mVerticalPixelCount;
        float mPixelWidth;
        float mPixelHeight;
        Vector3 mStartPosition;
        GameObject mPlaneObject;
        Mesh mMesh;
        Material mMaterial;
        Texture2D mGridTexture;
        GetGridDataID mGetID;
        SetGridDataID mSetID;
        GetGridColor mGetColor;
        ArrayPool<Color32> mColorArrayPool;
    }
}
