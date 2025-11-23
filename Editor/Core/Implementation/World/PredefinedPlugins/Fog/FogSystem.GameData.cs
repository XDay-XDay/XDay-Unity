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
using UnityEditor;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.FOW.Editor
{
    public partial class FogSystem
    {
        protected override void GenerateGameDataInternal(IObjectIDConverter converter)
        {
            ISerializer serializer = ISerializer.CreateBinary();
            serializer.WriteInt32(m_RuntimeVersion, "Fog.Version");

            serializer.WriteString(m_Name, "Name");

            var fogLayer = GetLayer(LayerType.UserDefined) as Layer;
            SaveLayer(serializer, fogLayer);

            serializer.Uninit();

            EditorHelper.WriteFile(serializer.Data, GetGameFilePath("fog"));
        }

        private void SaveLayer(ISerializer writer, Layer fogLayer)
        {
            var horizontalGridCount = fogLayer.HorizontalGridCount;
            var verticalGridCount = fogLayer.VerticalGridCount;
            var fogData = new byte[horizontalGridCount * verticalGridCount];
            for (var i = 0; i < verticalGridCount; ++i)
            {
                for (var j = 0; j < horizontalGridCount; ++j)
                {
                    var idx = i * horizontalGridCount + j;
                    if (fogLayer.Data[idx] != 0)
                    {
                        fogData[idx] = 1;
                    }
                }
            }

            writer.WriteInt32(fogLayer.HorizontalGridCount, "");
            writer.WriteInt32(fogLayer.VerticalGridCount, "");
            writer.WriteSingle(fogLayer.GridWidth, "");
            writer.WriteSingle(fogLayer.GridHeight, "");
            writer.WriteVector2(fogLayer.Origin, "");
            writer.WriteByteArray(fogData, "");

            var prefabPath = AssetDatabase.GUIDToAssetPath(fogLayer.FogPrefabGUID);
            var configPath = AssetDatabase.GUIDToAssetPath(fogLayer.FogConfigGUID);
            var blurShaderPath = AssetDatabase.GUIDToAssetPath(fogLayer.BlurShaderGUID);
            writer.WriteString(prefabPath, "Fog Prefab");
            writer.WriteString(configPath, "Fog Config");
            writer.WriteString(blurShaderPath, "Fog Blur Shader");
        }

        private const int m_RuntimeVersion = 1;
    }
}
