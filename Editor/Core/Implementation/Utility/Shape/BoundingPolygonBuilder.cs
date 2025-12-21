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
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace XDay.UtilityAPI.Editor
{
    /// <summary>
    /// https://www.codeproject.com/Articles/1201438/The-Concave-Hull-of-a-Set-of-Points
    /// </summary>
    public class BoundingPolygonBuilder
    {
        /// <summary>
        /// 构建最小包围框
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="heightThreshold">y坐标低于该值的顶点才考虑</param>
        /// <returns></returns>
        public List<Vector3> Build(GameObject gameObject, float heightThreshold = 2.0f, int iterateCount = 25)
        {
            if (gameObject == null)
            {
                return new();
            }
            var points = GetPoints(gameObject, heightThreshold);
            return Build(points, iterateCount);
        }

        public List<Vector3> Build(List<Vector3> points, int iterateCount = 25)
        {
            m_OutputFilePath = Helper.FindFile("concave.exe", "Packages");
            if (string.IsNullOrEmpty(m_OutputFilePath))
            {
                m_OutputFilePath = Helper.FindFile("concave.exe", "Assets");
            }

            if (string.IsNullOrEmpty(m_OutputFilePath))
            {
                Debug.LogError("concave.exe not found!");
                return new List<Vector3>();
            }

            m_OutputFilePath = m_OutputFilePath.Replace('\\', '/');
            m_OutputFilePath = Helper.GetFolderPath(m_OutputFilePath);

            CreateInputFile(points, m_OutputFilePath);
            EditorHelper.RunProcess($"{m_OutputFilePath}/concave.exe", $"input.txt -out output.txt -k {iterateCount}", m_OutputFilePath, out _, out _);
            var polygon = ParseOutput();
            FileUtil.DeleteFileOrDirectory($"{m_OutputFilePath}/input.txt");
            FileUtil.DeleteFileOrDirectory($"{m_OutputFilePath}/output.txt");
            return polygon;
        }

        private void CreateInputFile(List<Vector3> points, string outputPath)
        {
            StringBuilder builder = new();
            for (int i = 0; i < points.Count; i++)
            {
                builder.Append(points[i].x);
                builder.Append(",");
                builder.Append(points[i].z);
                builder.Append("\n");
            }
            File.WriteAllText($"{outputPath}/input.txt", builder.ToString());
        }

        private List<Vector3> ParseOutput()
        {
            List<Vector3> ret = new();
            var outputFilePath = $"{m_OutputFilePath}/output.txt";
            var text = File.ReadAllText(outputFilePath);
            var lines = text.Split("\n");
            foreach (var line in lines)
            {
                var coords = line.Split(" ");
                if (coords.Length > 1)
                {
                    double.TryParse(coords[0], out var x);
                    double.TryParse(coords[2], out var y);
                    var pos = new Vector3((float)x, 0, (float)y);
                    ret.Add(pos);
                }
            }
            return ret;
        }

        private List<Vector3> GetPoints(GameObject gameObject, float heightThreshold)
        {
            List<Vector3> points = new();
            var filters = gameObject.GetComponentsInChildren<MeshFilter>(true);
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh;
                if (mesh != null)
                {
                    Vector3[] worldVertices = new Vector3[mesh.vertexCount];
                    filter.transform.TransformPoints(mesh.vertices, worldVertices);
                    foreach (var vert in worldVertices)
                    {
                        if (vert.y <= heightThreshold)
                        {
                            points.Add(vert);
                        }
                    }
                }
            }
            return points;
        }

        private string m_OutputFilePath;
    }
}
