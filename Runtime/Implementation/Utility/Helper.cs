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

using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace XDay.UtilityAPI
{
    public static class Helper
    {
        public static Rect Rotate(this Rect rect, Quaternion rot)
        {
            var bounds = new Bounds();
            bounds.Encapsulate(rot * new Vector3(rect.xMin, 0, rect.yMin));
            bounds.Encapsulate(rot * new Vector3(rect.xMax, 0, rect.yMin));
            bounds.Encapsulate(rot * new Vector3(rect.xMin, 0, rect.yMax));
            bounds.Encapsulate(rot * new Vector3(rect.xMax, 0, rect.yMax));
            return bounds.ToRect();
        }

        public static void DestroyUnityObject(UnityEngine.Object obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying)
                {
                    if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj)))
                    {
                        UnityEngine.Object.DestroyImmediate(obj);
                    }
                    return;
                }
#endif
                UnityEngine.Object.Destroy(obj);
            }
        }

        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
            }
        }

        /// <summary>
        /// 移除重复的点,考虑浮点误差
        /// </summary>
        public static void RemoveDuplicatedFast(List<Vector3> list, float precision = 0.001f)
        {
            if (list == null || list.Count <= 1)
            {
                return;
            }

            var seen = new HashSet<ulong>();
            int writeIndex = 0;
            float invPrecision = 1f / precision;
            for (int i = 0; i < list.Count; i++)
            {
                Vector3 p = list[i];
                // 量化到整数格子（避免浮点哈希问题）
                long x = (long)Mathf.Round(p.x * invPrecision);
                long y = (long)Mathf.Round(p.y * invPrecision);
                long z = (long)Mathf.Round(p.z * invPrecision);

                // 合并为唯一 ulong（假设每个维度不超过 2^21）
                ulong hash = ((ulong)(x + 2097152) << 42) |
                             ((ulong)(y + 2097152) << 21) |
                             (ulong)(z + 2097152);

                if (seen.Add(hash))
                {
                    if (writeIndex != i)
                    {
                        list[writeIndex] = p;
                    }
                    writeIndex++;
                }
            }

            list.RemoveRange(writeIndex, list.Count - writeIndex);
        }

        public static List<Vector3> RemoveDuplicated(List<Vector3> points, float epsilon = 0.001f)
        {
            List<Vector3> result = new();
            foreach (var p in points)
            {
                if (IndexOf(result, p, epsilon) < 0)
                {
                    result.Add(p);
                }
            }

            if (result.Count != points.Count)
            {
                UnityEngine.Debug.Log($"removed {points.Count - result.Count} duplicated points");
            }

            return result;
        }

        public static string RemoveExtension(string path)
        {
            var idx = path.LastIndexOf('.');
            if (idx == -1)
            {
                return path;
            }
            return path[..idx];
        }

        public static string ToRelativePath(string absPath, string workingDir)
        {
            if (string.IsNullOrEmpty(absPath))
            {
                return null;
            }

            absPath = ToNixSlash(absPath);

            var workingDirAbsPath = ToNixSlash(Path.GetFullPath(workingDir));
            var index = absPath.IndexOf(workingDirAbsPath);
            if (index >= 0)
            {
                var startIndex = absPath[workingDirAbsPath.Length] == '/' ? workingDirAbsPath.Length + 1 : workingDirAbsPath.Length;
                return absPath[startIndex..];
            }
            return absPath;
        }

        public static string ToUnityPath(string path, string prefix = "Assets")
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            path = ToNixSlash(path);

            var index = path.IndexOf($"{prefix}/", System.StringComparison.Ordinal);
            if (index >= 0)
            {
                return path[index..];
            }
            if (path.Length > 0)
            {
                Debug.Assert(false, "Invalid unity path");
                return "";
            }
            return path;
        }

        public static string ToWinSlash(string path)
        {
            return path.Replace('/', '\\');
        }

        public static string ToNixSlash(string path)
        {
            return path.Replace('\\', '/');
        }

        public static void HideGameObject(GameObject obj, bool hide = true)
        {
#if UNITY_EDITOR
            if (obj != null)
            {
                if (!UnityEditor.EditorApplication.isPlaying)
                {
                    obj.hideFlags = hide ? HideFlags.HideInHierarchy : HideFlags.None;
                    UnityEditor.EditorApplication.DirtyHierarchyWindowSorting();
                }
            }
#endif
        }

        public static Vector2Int GetCenter(this HashSet<Vector2Int> coordinates)
        {
            var center = new Vector2Int(0, 0);
            foreach (var coord in coordinates)
            {
                center += coord;
            }

            if (coordinates.Count > 0)
            {
                center /= coordinates.Count;
            }

            return center;
        }

        public static Vector3 GetCenter(this List<Vector3> coordinates)
        {
            var center = Vector3.zero;
            foreach (var coord in coordinates)
            {
                center += coord;
            }

            if (coordinates.Count > 0)
            {
                center /= coordinates.Count;
            }

            return center;
        }

        public static Vector2Int GetCenter(this List<Vector2Int> coordinates)
        {
            var center = new Vector2Int(0, 0);
            for (var i = 0; i < coordinates.Count; ++i)
            {
                center += coordinates[i];
            }

            if (coordinates.Count > 0)
            {
                center /= coordinates.Count;
            }

            return center;
        }

        //xz plane,得到的center有可能在多边形外部
        //https://en.wikipedia.org/wiki/Centroid#:~:text=PE%2BPF)%2B3PG.%7D-,Of%20a%20polygon,-%5Bedit%5D
        public static Vector3 CalculatePolygonCenter(List<Vector3> polygonVertices)
        {
            int n = polygonVertices.Count;
            float x = 0;
            float z = 0;
            for (int i = 0; i < n; ++i)
            {
                var cur = polygonVertices[i];
                var next = polygonVertices[(i + 1) % n];
                x += (cur.x + next.x) * (cur.x * next.z - next.x * cur.z);
                z += (cur.z + next.z) * (cur.x * next.z - next.x * cur.z);
            }
            float area = CalculatePolygonSignedArea(polygonVertices);
            x /= (area * 6);
            z /= (area * 6);
            return new Vector3(x, 0, z);
        }

        public static float CalculatePolygonSignedArea(List<Vector3> polygonVertices)
        {
            float signedArea = 0;
            int n = polygonVertices.Count;
            for (int i = 0; i < n; ++i)
            {
                var cur = polygonVertices[i];
                var next = polygonVertices[(i + 1) % n];
                signedArea += (cur.x * next.z - next.x * cur.z);
            }
            return signedArea * 0.5f;
        }

        public static T AddOrGetComponent<T>(this GameObject obj) where T : Component
        {
            if (!obj.TryGetComponent<T>(out var comp))
            {
                comp = obj.AddComponent<T>();
            }
            return comp;
        }

        public static void DestroyComponent<T>(this GameObject obj) where T : Component
        {
            var comp = obj.GetComponent<T>();
            DestroyUnityObject(comp);
        }

        public static Vector3 FromFocusPointXZ(Camera camera, Vector3 focusPoint, float y)
        {
            focusPoint.y = 0;
            var cameraTransform = camera.transform;
            var viewDistance = FocalLengthFromAltitudeXZ(cameraTransform.eulerAngles.x, y);
            var cameraPos = focusPoint - cameraTransform.forward * viewDistance;
            return cameraPos;
        }

        public static Vector3 FromFocusPointXY(Camera camera, Vector3 focusPoint, float z)
        {
            focusPoint.z = 0;
            var cameraTransform = camera.transform;
            var viewDistance = FocalLengthFromAltitudeXY(cameraTransform.eulerAngles.x, z);
            var cameraPos = focusPoint - cameraTransform.forward * viewDistance;
            return cameraPos;
        }

        public static float FocalLengthFromAltitudeXZ(float rotationX, float y)
        {
            Debug.Assert(y > 0, "y must be > 0");
            return (float)((double)y / System.Math.Sin(rotationX * m_DegToRad));
        }

        public static float FocalLengthFromAltitudeXY(float rotationX, float z)
        {
            if (z <= 0)
            {
                Debug.Assert(false, $"无效的高度{z}");
            }

            if (rotationX == 0)
            {
                return z;
            }
            return (float)((double)z / System.Math.Sin((90 - rotationX) * m_DegToRad));
        }

        public static bool ContainsTextureProperty(this Shader shader, string propertyName)
        {
            int n = shader.GetPropertyCount();
            for (int i = 0; i < n; ++i)
            {
                if (shader.GetPropertyName(i) == propertyName &&
                    shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Contains(this Bounds a, Bounds b)
        {
            return
                GE(b.min.x, a.min.x) &&
                GE(b.min.y, a.min.y) &&
                GE(b.min.z, a.min.z) &&
                LE(b.min.x, a.max.x) &&
                LE(b.min.y, a.max.y) &&
                LE(b.min.z, a.max.z) &&
                GE(b.max.x, a.min.x) &&
                GE(b.max.y, a.min.y) &&
                GE(b.max.z, a.min.z) &&
                LE(b.max.x, a.max.x) &&
                LE(b.max.y, a.max.y) &&
                LE(b.max.z, a.max.z);
        }

        public static Vector3 ToVector3XY(this Vector2 v)
        {
            return new Vector3(v.x, v.y, 0);
        }

        public static Vector3 ToVector3XZ(this Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        public static Vector2 ToVector2(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public static Vector3 RayCastXZPlane(Vector3 origin, Vector3 direction)
        {
            return origin + direction * -origin.y / direction.y;
        }

        public static Vector3 RayCastXYPlane(Vector3 origin, Vector3 direction)
        {
            return origin - direction * origin.z / direction.z;
        }

        //bottom left is origin
        public static Vector3 RayCastWithXZPlane(Vector2 screenPos, Camera camera, float y = 0)
        {
            if (camera == null)
            {
                return Vector3.zero;
            }

            var ray = camera.ScreenPointToRay(screenPos);
            if (Mathf.Approximately(ray.direction.y, 0))
            {
                return Vector3.zero;
            }

            var t = (y - ray.origin.y) / ray.direction.y;
            return ray.GetPoint(t);
        }

        public static Vector3 RayCastWithPlane(Vector2 screenPos, Camera camera, Plane plane)
        {
            if (camera == null)
            {
                return Vector3.zero;
            }

            var ray = camera.ScreenPointToRay(screenPos);
            plane.Raycast(ray, out var t);
            return ray.GetPoint(t);
        }

        //top left is origin
        public static Vector3 GUIRayCastWithXZPlane(Vector3 screenPos, Camera camera, float y = 0)
        {
            screenPos = ScreenPointTLToBL(screenPos);
            return RayCastWithXZPlane(screenPos, camera, y);
        }

        public static Vector2 ScreenPointTLToBL(Vector2 pos)
        {
#if UNITY_EDITOR
            return UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(pos);
#else
            Debug.LogError("not implemented!");
            return Vector2.zero;
#endif
        }

        public static float EaseOutExp(float start, float end, float t)
        {
            var delta = end - start;
            return delta * (-Mathf.Pow(2, -10 * t) + 1) + start;
        }

        public static float EaseOutQuad(float start, float end, float t)
        {
            var delta = end - start;
            return -delta * t * (t - 2) + start;
        }

        public static Vector3 ClampPointInXZPlane(Vector3 p, Vector2 min, Vector2 max)
        {
            return new Vector3(Mathf.Clamp(p.x, min.x, max.x), p.y, Mathf.Clamp(p.z, min.y, max.y));
        }

        public static Vector3 ClampPointInXYPlane(Vector3 p, Vector2 min, Vector2 max)
        {
            return new Vector3(Mathf.Clamp(p.x, min.x, max.x), Mathf.Clamp(p.y, min.y, max.y), p.z);
        }

        public static float Sign(float value)
        {
            return value >= 0 ? 1 : -1;
        }

        public static bool PointInTriangle2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            var v = (d11 * d20 - d01 * d21) / denom;
            var w = (d00 * d21 - d01 * d20) / denom;
            var u = 1.0f - v - w;

            return (u >= 0) && (v >= 0) && (u + v < 1);
        }

        public static bool PointInTriangle3D(Vector3 p, Vector3 a, Vector3 b, Vector3 c, out Vector3 barycentricCoordinate)
        {
            Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            var v = (d11 * d20 - d01 * d21) / denom;
            var w = (d00 * d21 - d01 * d20) / denom;
            var u = 1.0f - v - w;
            barycentricCoordinate = new Vector3(u, v, w);
            return (u >= 0) && (v >= 0) && (u + v < 1);
        }

        //检查a是否在b的左边,顺时针,左手系
        public static bool IsLeftXZ(Vector3 a, Vector3 b)
        {
            return a.z * b.x - a.x * b.z >= 0;
        }

        /// <summary>
        /// 左手坐标系
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>0共线,-1左边,1右边</returns>
        public static int GetSide(Vector3 a, Vector3 b)
        {
            float ret = a.z * b.x - a.x * b.z;
            if (Approximately(ret, 0, 0.0001f))
            {
                return 0;
            }
            if (ret > 0)
            {
                return -1;
            }
            return 1;
        }

        //检查dir在dirLeft和dirRight的哪边
        public static int GetSide(Vector3 dir, Vector3 dirLeft, Vector3 dirRight)
        {
            bool leftCheck = IsLeftXZ(dirLeft, dir);
            if (leftCheck == false)
            {
                return -1;
            }
            bool rightCheck = IsLeftXZ(dir, dirRight);
            if (rightCheck == false)
            {
                return 1;
            }
            return 0;
        }

        public static Vector3 RotateY(Vector3 dir, float angle)
        {
            Quaternion q = Quaternion.Euler(0, angle, 0);
            return q * dir;
        }

        public static float GetAngleBetween(Vector3 dirA, Vector3 dirB)
        {
            float angle = Vector3.Angle(dirA, dirB);
            var p = Vector3.Cross(dirA, dirB);
            if (p.y > 0)
            {
                return angle;
            }

            return -angle;
        }

        public static float GetAngleBetween(Vector2 dir0, Vector2 dir1)
        {
            dir0.Normalize();
            dir1.Normalize();
            var dot = Vector2.Dot(dir0, dir1);
            if (dot >= 1)
            {
                return 0;
            }
            if (dot <= -1)
            {
                return 180;
            }
            return Mathf.Acos(dot) * Mathf.Rad2Deg;
        }

        public static bool GE(float a, float b, float esp = 0.0001f)
        {
            return GT(a, b, esp) || Approximately(a, b, esp);
        }

        public static bool LE(float a, float b, float esp = 0.0001f)
        {
            return LT(a, b, esp) || Approximately(a, b, esp);
        }

        public static bool LT(float a, float b, float esp = 0.0001f)
        {
            if (a <= b - esp)
            {
                return true;
            }
            return false;
        }

        public static bool GT(float a, float b, float esp = 0.0001f)
        {
            if (a >= b + esp)
            {
                return true;
            }
            return false;
        }

        public static bool Approximately(Vector3 a, Vector3 b, float esp = 0.0001f)
        {
            return Approximately(a.x, b.x, esp) &&
                   Approximately(a.y, b.y, esp) &&
                   Approximately(a.z, b.z, esp);
        }

        public static bool Approximately(Vector2 a, Vector2 b, float esp = 0.0001f)
        {
            return Approximately(a.x, b.x, esp) &&
                   Approximately(a.y, b.y, esp);
        }

        public static bool Approximately(float a, float b, float esp = 0.0001f)
        {
            if (Mathf.Abs(a - b) <= esp)
            {
                return true;
            }
            return false;
        }

        public static Rect MergeRect(Rect a, Rect b)
        {
            var min = Vector2.Min(a.min, b.min);
            var max = Vector2.Max(a.max, b.max);
            return new Rect(min, max - min);
        }

        public static Rect ExpandRect(Rect rect, Vector2 size)
        {
            var minX = rect.xMin - size.x * 0.5f;
            var minY = rect.yMin - size.y * 0.5f;
            var maxX = rect.xMax + size.x * 0.5f;
            var maxY = rect.yMax + size.y * 0.5f;

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static Rect ExpandRect(Rect rect, Rect expand)
        {
            var minX = rect.xMin + expand.xMin;
            var minY = rect.yMin + expand.yMin;
            var maxX = rect.xMax + expand.xMax;
            var maxY = rect.yMax + expand.yMax;

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static string GetFolderPath(string path)
        {
            int idx = path.LastIndexOf("/", System.StringComparison.Ordinal);
            if (idx != -1)
            {
                path = path.Substring(0, idx);
            }

            return path;
        }

        public static string GetPathName(string path, bool hasExtension)
        {
            int idx = path.LastIndexOf("/", System.StringComparison.Ordinal);
            if (idx != -1)
            {
                path = path.Substring(idx + 1);
            }

            if (hasExtension == false)
            {
                idx = path.IndexOf(".");
                if (idx != -1)
                {
                    path = path.Substring(0, idx);
                }
            }

            return path;
        }

        public static string GetPathExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }

            var index = path.IndexOf(".");
            if (index >= 0)
            {
                return path.Substring(index + 1);
            }

            return "";
        }

        public static Bounds QueryBounds(this GameObject gameObject, bool setIdentity = true)
        {
            var bounds = new Bounds();
            var transform = gameObject.transform;
            var oldPos = transform.position;
            var oldScale = transform.localScale;
            var oldRot = transform.rotation;
            if (setIdentity)
            {
                transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                transform.localScale = Vector3.one;
            }

            foreach (var renderer in gameObject.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                if (bounds.size != Vector3.zero)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                }
            }

            foreach (var renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true))
            {
                if (bounds.size != Vector3.zero)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                }
            }

            foreach (var collider in gameObject.GetComponentsInChildren<MeshCollider>(includeInactive: true))
            {
                if (collider.sharedMesh != null)
                {
                    var worldBounds = collider.sharedMesh.bounds.Transform(collider.transform);
                    if (bounds.size != Vector3.zero)
                    {
                        bounds.Encapsulate(worldBounds);
                    }
                    else
                    {
                        bounds = worldBounds;
                    }
                }
            }

            if (setIdentity)
            {
                transform.SetPositionAndRotation(oldPos, oldRot);
                transform.localScale = oldScale;
            }

            if (bounds.extents == Vector3.zero)
            {
                Debug.LogWarning("bounds is empty");
            }
            return bounds;
        }

        public static Rect QueryRectWithRotation(this GameObject gameObject, Quaternion rotation)
        {
            if (gameObject == null)
            {
                Debug.LogError("game object is null!");
                return new();
            }

            var transform = gameObject.transform;
            transform.GetPositionAndRotation(out var oldPos, out var oldRot);
            transform.SetPositionAndRotation(Vector3.zero, rotation);

            var rect = QueryRect(gameObject, setIdentity: false);
            transform.SetPositionAndRotation(oldPos, oldRot);
            return rect;
        }

        public static Rect QueryRectWithLocalScaleAndRotation(this GameObject gameObject, Quaternion rotation, Vector3 localScale)
        {
            if (gameObject == null)
            {
                Debug.LogError("game object is null!");
                return new();
            }

            var transform = gameObject.transform;
            transform.GetPositionAndRotation(out var oldPos, out var oldRot);
            var oldScale = transform.localScale;
            transform.SetPositionAndRotation(Vector3.zero, rotation);
            transform.localScale = localScale;

            var rect = QueryRect(gameObject, setIdentity: false);
            transform.SetPositionAndRotation(oldPos, oldRot);
            transform.localScale = oldScale;
            return rect;
        }

        public static Rect QueryRectWithLocalTransform(this GameObject gameObject, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (gameObject == null)
            {
                Debug.LogError("game object is null!");
                return new();
            }

            var transform = gameObject.transform;
            var oldPos = transform.localPosition;
            var oldRot = transform.localRotation;
            var oldScale = transform.localScale;
            transform.SetLocalPositionAndRotation(pos, rot);
            transform.localScale = scale;

            var rect = QueryRect(gameObject, setIdentity: false);
            transform.SetLocalPositionAndRotation(oldPos, oldRot);
            transform.localScale = oldScale;
            return rect;
        }

        public static Rect QueryRectWithTransform(this GameObject gameObject, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (gameObject == null)
            {
                Debug.LogError("game object is null!");
                return new();
            }

            var transform = gameObject.transform;
            var oldPos = transform.position;
            var oldRot = transform.rotation;
            var oldScale = transform.localScale;
            transform.SetPositionAndRotation(pos, rot);
            transform.localScale = scale;

            var rect = QueryRect(gameObject, setIdentity: false);
            transform.SetPositionAndRotation(oldPos, oldRot);
            transform.localScale = oldScale;
            return rect;
        }

        public static Rect QueryRect(this GameObject gameObject, bool setIdentity = true)
        {
            if (gameObject == null)
            {
                Debug.LogError("game object is null!");
                return new();
            }

            var bounds = gameObject.QueryBounds(setIdentity);
            var min = bounds.min;
            var max = bounds.max;
            return new Rect(min.x, min.z, bounds.size.x, bounds.size.z);
        }

        public static Bounds Transform(this Bounds bounds, Matrix4x4 mat)
        {
            var right = new Vector3(mat.m00, mat.m10, mat.m20);
            var up = new Vector3(mat.m01, mat.m11, mat.m21);
            var forward = new Vector3(mat.m02, mat.m12, mat.m22);
            var translation = new Vector3(mat.m03, mat.m13, mat.m23);

            var min = bounds.min;
            var max = bounds.max;
            var ra = right * min.x;
            var rb = right * max.x;
            var fa = forward * min.z;
            var fb = forward * max.z;
            var ua = up * min.y;
            var ub = up * max.y;

            var boundsMin = Vector3.Min(ra, rb) + Vector3.Min(ua, ub) + Vector3.Min(fa, fb) + translation;
            var boundsMax = Vector3.Max(ra, rb) + Vector3.Max(ua, ub) + Vector3.Max(fa, fb) + translation;

            var ret = new Bounds();
            ret.SetMinMax(boundsMin, boundsMax);
            return ret;
        }

        public static Bounds Transform(this Bounds bounds, Transform transform)
        {
            return bounds.Transform(transform.localToWorldMatrix);
        }

        public static Vector3 Mult(this Vector3 v, Vector3 other)
        {
            return new Vector3(v.x * other.x, v.y * other.y, v.z * other.z);
        }

        public static Vector3 Div(this Vector3 v, Vector3 other)
        {
            return new Vector3(v.x / other.x, v.y / other.y, v.z / other.z);
        }

        public static Rect ToRect(this Bounds b)
        {
            return new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
        }

        public static Bounds ToBounds(this Rect r)
        {
            return new Bounds(r.center.ToVector3XZ(), r.size.ToVector3XZ());
        }

        public static bool CheckOverlap(List<Vector3> coordinates, Vector3 pos, float space)
        {
            foreach (var coordinate in coordinates)
            {
                if ((pos - coordinate).sqrMagnitude <= space * space)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool SphereAABBIntersectSq(Vector3 center, float radiusSq, Vector3 min, Vector3 max)
        {
            return PointToAABBMinDistanceSq(center, min, max) <= radiusSq;
        }

        public static float PointToAABBMinDistanceSq(Vector3 point, Vector3 min, Vector3 max)
        {
            var distance = 0.0f;

            if (point.x < min.x)
            {
                var d = point.x - min.x;
                distance += d * d;
            }
            if (point.x > max.x)
            {
                var d = point.x - max.x;
                distance += d * d;
            }

            if (point.y < min.y)
            {
                var d = point.y - min.y;
                distance += d * d;
            }
            if (point.y > max.y)
            {
                var d = point.y - max.y;
                distance += d * d;
            }

            if (point.z < min.z)
            {
                var d = point.z - min.z;
                distance += d * d;
            }
            if (point.z > max.z)
            {
                var d = point.z - max.z;
                distance += d * d;
            }

            return distance;
        }

        public static bool RectOverlap(float minX1, float minY1, float maxX1, float maxY1, float minX2, float minY2, float maxX2, float maxY2)
        {
            if (minX1 > maxX2 ||
                minY1 > maxY2 ||
                minX2 > maxX1 ||
                minY2 > maxY1)
            {
                return false;
            }
            return true;
        }

        public static float GetSignedPolygonArea(Vector3[] polygon)
        {
            var n = polygon.Length;
            float area = 0;
            for (var i = 0; i < n; i++)
            {
                area += (polygon[(i + 1) % n].x - polygon[i].x) * (polygon[(i + 1) % n].z + polygon[i].z) / 2;
            }
            return area;
        }

        public static float GetPolygonArea(Vector3[] polygon)
        {
            return Mathf.Abs(GetSignedPolygonArea(polygon));
        }

        public static void Traverse(Transform root, bool startFromChildren, Action<Transform> callback)
        {
            if (root == null)
            {
                return;
            }

            Stack<Transform> stack = new();
            if (startFromChildren)
            {
                foreach (Transform child in root)
                {
                    stack.Push(child);
                }
            }
            else
            {
                stack.Push(root);
            }
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                callback(cur);

                foreach (Transform child in cur.transform)
                {
                    stack.Push(child);
                }
            }
        }

        public static bool IsPOT(int n)
        {
            if (n <= 0)
            {
                return false;
            }

            return (n & (n - 1)) == 0;
        }

        public static int NextPowerOf2(int value)
        {
            int power = 1;
            while (power < value)
            {
                power *= 2;
            }
            return power;
        }

        public static Color Lerp(this Color a, Color b, float t)
        {
            return new Color(Mathf.Lerp(a.r, b.r, t),
                             Mathf.Lerp(a.g, b.g, t),
                             Mathf.Lerp(a.b, b.b, t),
                             Mathf.Lerp(a.a, b.a, t));
        }

        public static void InterlockedSet(ref int target, int value)
        {
            int initial, computed;
            do
            {
                initial = target;
                computed = value;
            }
            while (Interlocked.CompareExchange(ref target, computed, initial) != initial);
        }

        public static void Int32ToBytes(int value, byte[] array)
        {
            array[0] = (byte)(value & 0xff);
            array[1] = (byte)((value >> 8) & 0xff);
            array[2] = (byte)((value >> 16) & 0xff);
            array[3] = (byte)((value >> 24) & 0xff);
        }

        public static unsafe int BytesToInt32(byte[] array, int offset)
        {
            return
                array[offset + 3] |
                (array[offset + 2] << 8) |
                (array[offset + 1] << 16) |
                (array[offset + 0] << 24);
        }

        public static GameObject FindChildInHierarchy(this GameObject obj, string hierarchyPath, bool includeRoot)
        {
            if (string.IsNullOrEmpty(hierarchyPath) || obj == null)
            {
                return null;
            }

            var paths = hierarchyPath.Split('/');
            return obj.FindChildInHierarchyInternal(0, paths, includeRoot);
        }

        public static bool IsRoot(Transform transform)
        {
            var parent = transform.parent;
            if (parent == null)
            {
                return true;
            }
            if (parent.name.EndsWith("(Environment)") ||
                parent.name.EndsWith("Prefab Mode in Context"))
            {
                return true;
            }
            return false;
        }

        public static string GetPathInHierarchy(this GameObject go, bool includeRoot)
        {
            if (go == null)
            {
                return "";
            }

            var root = go.GetRoot(includeRoot);
            if (root.name.EndsWith("(Environment)") ||
                root.name.EndsWith("Prefab Mode in Context"))
            {
                root = root.GetChild(0);
            }
            var builder = new StringBuilder();
            while (true)
            {
                builder.Insert(0, go.name);
                if (go.transform == root)
                {
                    break;
                }
                var parent = go.transform.parent;
                if (parent != null)
                {
                    builder.Insert(0, "/");
                    go = parent.gameObject;
                }
                else
                {
                    break;
                }
            }
            return builder.ToString();
        }

        public static Transform GetRoot(this GameObject go, bool includeRoot)
        {
            if (go == null)
            {
                return null;
            }

            if (includeRoot)
            {
                return go.transform.root;
            }

            var cur = go.transform;
            if (cur.parent == null)
            {
                return cur;
            }
            while (cur.parent.parent != null)
            {
                cur = cur.parent;
            }
            return cur;
        }

        public static void Resize<T>(List<T> list, int n)
        {
            if (list == null)
            {
                Debug.LogError("List is null");
            }
            else
            {
                list.Clear();
                list.Capacity = n;
            }
        }

        public static bool PointInCircle(Vector2 p, Vector2 center, float radius)
        {
            var distanceSqr = (p - center).SqrMagnitude();
            return distanceSqr <= radius * radius;
        }

        public static bool PointInPolygon(Vector3 testPoint, Vector3[] polygon)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].z < testPoint.z && polygon[j].z >= testPoint.z ||
                    polygon[j].z < testPoint.z && polygon[i].z >= testPoint.z)
                {
                    if (polygon[i].x + (testPoint.z - polygon[i].z) /
                       (polygon[j].z - polygon[i].z) *
                       (polygon[j].x - polygon[i].x) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        public static bool PointInPolygon(Vector3 testPoint, List<Vector3> polygon)
        {
            bool result = false;
            int j = polygon.Count - 1;
            for (int i = 0; i < polygon.Count; i++)
            {
                if (polygon[i].z < testPoint.z && polygon[j].z >= testPoint.z ||
                    polygon[j].z < testPoint.z && polygon[i].z >= testPoint.z)
                {
                    if (polygon[i].x + (testPoint.z - polygon[i].z) /
                       (polygon[j].z - polygon[i].z) *
                       (polygon[j].x - polygon[i].x) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        public static bool RectInPolygon(Vector3 min, Vector3 max, List<Vector3> polygon)
        {
            var lbIn = PointInPolygon(min, polygon);
            if (lbIn)
            {
                return true;
            }
            var rtIn = PointInPolygon(max, polygon);
            if (rtIn)
            {
                return true;
            }

            var ltIn = PointInPolygon(new Vector3(min.x, 0, max.z), polygon);
            if (ltIn)
            {
                return true;
            }

            var rbIn = PointInPolygon(new Vector3(max.x, 0, min.z), polygon);
            if (rbIn)
            {
                return true;
            }

            var centerIn = PointInPolygon((min + max) / 2, polygon);
            if (centerIn)
            {
                return true;
            }

            foreach (var pt in polygon)
            {
                if (pt.x >= min.x && pt.y >= min.y && pt.z >= min.z &&
                    pt.x <= max.x && pt.y <= max.y && pt.z <= max.z)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool PointInPolygon2D(Vector3 p, Vector3[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
            {
                return false;
            }

            int numVerts = polygon.Length;
            Vector3 p0 = polygon[numVerts - 1];
            bool bYFlag0 = (p0.z >= p.z);

            bool bInside = false;
            for (int j = 0; j < numVerts; ++j)
            {
                Vector3 p1 = polygon[j];
                bool bYFlag1 = (p1.z >= p.z);
                if (bYFlag0 != bYFlag1)
                {
                    if (((p1.z - p.z) * (p0.x - p1.x) >= (p1.x - p.x) * (p0.z - p1.z)) == bYFlag1)
                    {
                        bInside = !bInside;
                    }
                }

                // Move to the next pair of vertices, retaining info as possible.
                bYFlag0 = bYFlag1;
                p0 = p1;
            }

            return bInside;
        }

        public static bool PointInPolygon2D(Vector3 p, List<Vector3> polygon)
        {
            if (polygon == null || polygon.Count < 3)
            {
                return false;
            }

            int numVerts = polygon.Count;
            Vector3 p0 = polygon[numVerts - 1];
            bool bYFlag0 = (p0.z >= p.z);

            bool bInside = false;
            for (int j = 0; j < numVerts; ++j)
            {
                Vector3 p1 = polygon[j];
                bool bYFlag1 = (p1.z >= p.z);
                if (bYFlag0 != bYFlag1)
                {
                    if (((p1.z - p.z) * (p0.x - p1.x) >= (p1.x - p.x) * (p0.z - p1.z)) == bYFlag1)
                    {
                        bInside = !bInside;
                    }
                }

                // Move to the next pair of vertices, retaining info as possible.
                bYFlag0 = bYFlag1;
                p0 = p1;
            }

            return bInside;
        }

        public static bool PointInRect(Vector2 p, Vector2 rectMin, Vector2 rectMax)
        {
            if (p.x >= rectMin.x && p.x <= rectMax.x &&
                p.y >= rectMin.y && p.y <= rectMax.y)
            {
                return true;
            }
            return false;
        }

        private static GameObject FindChildInHierarchyInternal(this GameObject obj, int index, string[] paths, bool includeRoot)
        {
            var child = obj.transform.Find(paths[index]);
            if (child == null && obj.name == paths[index] && includeRoot)
            {
                child = obj.transform;
            }
            if (child == null)
            {
                return null;
            }
            if (index < paths.Length - 1)
            {
                return child.gameObject.FindChildInHierarchyInternal(index + 1, paths, includeRoot);
            }
            return child.gameObject;
        }

        public static bool PointSegmentDistance(Vector2 p, Vector2 a, Vector2 b,
            out float distanceSquared, out Vector2 cp)
        {
            if (a == b)
            {
                cp = a;
                distanceSquared = (p - cp).sqrMagnitude;
                return false;
            }

            Vector2 ab = b - a;
            Vector2 ap = p - a;

            float proj = Vector2.Dot(ap, ab);
            float abLenSq = ab.sqrMagnitude;
            float d = proj / abLenSq;

            bool interior = false;
            if (d <= 0)
            {
                cp = a;
            }
            else if (d >= 1.0f)
            {
                cp = b;
            }
            else
            {
                cp = a + ab * d;
                interior = true;
            }

            distanceSquared = (p - cp).sqrMagnitude;
            return interior;
        }

        public static Vector3 FindClosestPointToSegement(Vector3 p, Vector3 segmentStart, Vector3 segmentEnd, out float distance)
        {
            var dir = segmentEnd - segmentStart;
            var ps = p - segmentStart;
            var proj = Vector3.Dot(dir, ps);
            proj /= dir.sqrMagnitude;
            Vector3 pos;
            if (proj >= 1)
            {
                pos = segmentEnd;
            }
            else if (proj <= 0)
            {
                pos = segmentStart;
            }
            else
            {
                pos = Vector3.Lerp(segmentStart, segmentEnd, proj);
            }
            distance = Vector3.Distance(p, pos);
            return pos;
        }

        public static void PointToLineSegmentDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd, out float distance, out bool interior, out Vector3 pointProjectionOnLine)
        {
            var pointToStart = point - lineStart;
            var pointToEnd = point - lineEnd;
            pointToEnd.Normalize();

            var lineDirection = (lineEnd - lineStart).normalized;
            var dStart = Vector3.Dot(pointToStart, lineDirection);
            var dEnd = Vector3.Dot(pointToEnd, -lineDirection);

            pointProjectionOnLine = Vector3.Dot(pointToStart, lineDirection) * lineDirection;
            var perpendicularDirection = pointToStart - pointProjectionOnLine;

            pointToStart.Normalize();

            interior = dStart >= 0 && dEnd >= 0;
            distance = perpendicularDirection.magnitude;
        }

        public static int FindClosestEdgeOnProjection(Vector3 point, List<Vector3> polygon, out Vector3 closestPointOutPolygon)
        {
            closestPointOutPolygon = Vector3.zero;
            var closestEdgeIndex = -1;
            var minimumDistance = float.MaxValue;
            for (var edgeIndex = 0; edgeIndex < polygon.Count; ++edgeIndex)
            {
                //PointToLineSegmentDistance(point, polygon[edgeIndex], polygon[(edgeIndex + 1) % polygon.Count], out var distance, out var interior, out var pointProjectionOnLine);

                var pointProjectionOnLine = FindClosestPointToSegement(point, polygon[edgeIndex], polygon[(edgeIndex + 1) % polygon.Count], out var distance);

                if (distance < minimumDistance)
                {
                    closestPointOutPolygon = pointProjectionOnLine;
                    closestEdgeIndex = edgeIndex;
                    minimumDistance = distance;
                }
            }

            Debug.Assert(closestEdgeIndex >= 0);
            return (closestEdgeIndex + 1) % polygon.Count;
        }

        public static Vector3 ToVector3(this FixedVector3 v)
        {
            return new Vector3(v.X.FloatValue, v.Y.FloatValue, v.Z.FloatValue);
        }

        public static Vector2 ToVector2(this FixedVector2 v)
        {
            return new Vector2(v.X.FloatValue, v.Y.FloatValue);
        }

        public static FixedVector3 FromVector3(Vector3 v)
        {
            return new FixedVector3(v.x, v.y, v.z);
        }

        public static FixedVector2 FromVector2(Vector3 v)
        {
            return new FixedVector2(v.x, v.y);
        }

        public static long GetTimeNow()
        {
            return (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        }

        public static Type SearchTypeByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            name = $".{name}";
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var pos = type.FullName.IndexOf(name);
                    if (pos >= 0)
                    {
                        var end = pos + name.Length;
                        if (end >= type.FullName.Length ||
                            type.FullName[end] == '.')
                        {
                            return type;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取所有继承自指定基类的非抽象子类
        /// </summary>
        /// <param name="baseType">基类类型</param>
        /// <param name="includeAbstract">是否包含抽象类</param>
        public static List<Type> GetAllSubclasses(Type baseType, bool includeAbstract = false)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // 处理无法加载的类型（如缺少依赖）
                        return Array.Empty<Type>();
                    }
                })
                .Where(type =>
                    type.IsSubclassOf(baseType) && // 必须是直接或间接子类
                    (includeAbstract || !type.IsAbstract) // 根据参数过滤抽象类
                )
                .ToList();
        }

        public static List<Type> GetClassesImplementingInterface(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("提供的类型必须是一个接口。", nameof(interfaceType));

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<Type> ret = new();
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 处理部分类型加载失败的情况
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    // 忽略无法加载的程序集
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.IsClass && !type.IsAbstract && interfaceType.IsAssignableFrom(type))
                    {
                        ret.Add(type);
                    }
                }
            }
            return ret;
        }

        public static string GetObjectGUID(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(obj);
            return AssetDatabase.AssetPathToGUID(path);
#else
            return "";
#endif
        }

        public static Rect CalculateRect(IEnumerable<Vector3> vertices)
        {
            float minX = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxZ = float.MinValue;
            foreach (var local in vertices)
            {
                if (local.x < minX)
                {
                    minX = local.x;
                }

                if (local.z < minZ)
                {
                    minZ = local.z;
                }

                if (local.x > maxX)
                {
                    maxX = local.x;
                }

                if (local.z > maxZ)
                {
                    maxZ = local.z;
                }
            }

            var bounds = new Rect(minX, minZ, maxX - minX, maxZ - minZ);
            return bounds;
        }

        public static Bounds CalculateBounds(IEnumerable<Vector3> points)
        {
            if (!points.Any())
            {
                return new Bounds();
            }

            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var minZ = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;
            var maxZ = float.MinValue;
            foreach (var point in points)
            {
                if (point.x < minX)
                {
                    minX = point.x;
                }
                if (point.y < minY)
                {
                    minY = point.y;
                }
                if (point.z < minZ)
                {
                    minZ = point.z;
                }
                if (point.x > maxX)
                {
                    maxX = point.x;
                }
                if (point.y > maxY)
                {
                    maxY = point.y;
                }
                if (point.z > maxZ)
                {
                    maxZ = point.z;
                }
            }

            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
            return bounds;
        }

        public static void SetTextNoGc(this TextMeshProUGUI textComp, ZStringInterpolatedStringHandler handler)
        {
            textComp.SetCharArray(handler.Builder.Buffer, 0, handler.Builder.Length);
        }

        public static float EaseInExpo(float v)
        {
            return v == 0 ? 0 : Mathf.Pow(2, 10 * v - 10);
        }

        public static Color32 LerpColor(ref Color32 a, ref Color32 b, float t)
        {
            return new Color32(
                (byte)(a.r + (b.r - a.r) * t),
                (byte)(a.g + (b.g - a.g) * t),
                (byte)(a.b + (b.b - a.b) * t),
                (byte)(a.a + (b.a - a.a) * t));
        }

        public static bool GetIntersection(int minX1, int minY1, int maxX1, int maxY1,
            int minX2, int minY2, int maxX2, int maxY2,
            out int intersectionMinX, out int intersectionMinY, out int intersectionMaxX, out int intersectionMaxY)
        {
            intersectionMinX = 0;
            intersectionMinY = 0;
            intersectionMaxX = 0;
            intersectionMaxY = 0;

            intersectionMinX = Mathf.Max(minX1, minX2);
            intersectionMaxX = Mathf.Min(maxX1, maxX2);
            if (intersectionMinX > intersectionMaxX)
            {
                return false;
            }

            intersectionMinY = Mathf.Max(minY1, minY2);
            intersectionMaxY = Mathf.Min(maxY1, maxY2);
            if (intersectionMinY > intersectionMaxY)
            {
                return false;
            }
            return true;
        }

        // 检测线段与轴对齐矩形是否相交
        public static bool CheckLineRectIntersection(Vector2 a, Vector2 b, Rect rect, out Vector2 intersection)
        {
            intersection = Vector2.zero;
            // 1. 检查线段端点是否在矩形内
            if (rect.Contains(a) && rect.Contains(b)) return true;

            // 2. 检查线段是否与矩形四条边相交
            Vector2 min = rect.min;
            Vector2 max = rect.max;

            // 左边界 x = min.x
            if (LineIntersectsVertical(a, b, min.x, min.y, max.y, out intersection))
                return true;

            // 右边界 x = max.x
            if (LineIntersectsVertical(a, b, max.x, min.y, max.y, out intersection))
                return true;

            // 下边界 y = min.y
            if (LineIntersectsHorizontal(a, b, min.y, min.x, max.x, out intersection))
                return true;

            // 上边界 y = max.y
            if (LineIntersectsHorizontal(a, b, max.y, min.x, max.x, out intersection))
                return true;

            return false;
        }

        // 检测线段与垂直边（x = xEdge）的交点
        private static bool LineIntersectsVertical(Vector2 a, Vector2 b, float xEdge, float yMin, float yMax, out Vector2 intersection)
        {
            intersection = Vector2.zero;
            if (Mathf.Approximately(a.x - b.x, 0)) return false; // 线段垂直，与x边平行

            float t = (xEdge - a.x) / (b.x - a.x);
            if (t < 0 || t > 1) return false;

            float yIntersect = a.y + t * (b.y - a.y);
            intersection = new Vector2(xEdge, yIntersect);
            return yIntersect >= yMin && yIntersect <= yMax;
        }

        // 检测线段与水平边（y = yEdge）的交点
        private static bool LineIntersectsHorizontal(Vector2 a, Vector2 b, float yEdge, float xMin, float xMax, out Vector2 intersection)
        {
            intersection = Vector2.zero;
            if (Mathf.Approximately(a.y - b.y, 0)) return false; // 线段水平，与y边平行

            float t = (yEdge - a.y) / (b.y - a.y);
            if (t < 0 || t > 1) return false;

            float xIntersect = a.x + t * (b.x - a.x);
            intersection = new Vector2(xIntersect, yEdge);
            return xIntersect >= xMin && xIntersect <= xMax;
        }

        public static Vector3 CalculateBaryCentricCoord(float x0, float z0, float x1, float z1, float x2, float z2, float px, float pz)
        {
            float pax = px - x0;
            float paz = pz - z0;
            float bax = x1 - x0;
            float baz = z1 - z0;
            float cax = x2 - x0;
            float caz = z2 - z0;
            float delta = bax * caz - baz * cax;
            if (delta == 0)
            {
                return Vector3.zero;
            }

            float t1 = (pax * caz - paz * cax) / delta;
            float t2 = (bax * paz - baz * pax) / delta;

            return new Vector3(1 - t1 - t2, t1, t2);
        }

        public static bool GetRectIntersection(Rect a, Rect b, out Rect intersection)
        {
            if (a.Overlaps(b))
            {
                float minX = Mathf.Max(a.min.x, b.min.x);
                float minY = Mathf.Max(a.min.y, b.min.y);
                float maxX = Mathf.Min(a.max.x, b.max.x);
                float maxY = Mathf.Min(a.max.y, b.max.y);
                intersection = new Rect(minX, minY, maxX - minX, maxY - minY);
                return true;
            }

            intersection = new Rect();
            return false;
        }

        public static int FindOrAddVertex(List<Vector3> vertices, Vector3 v, float esp, out bool added)
        {
            for (int i = 0; i < vertices.Count; ++i)
            {
                var o = vertices[i];
                if (Approximately(o.x, v.x, esp) &&
                    Approximately(o.y, v.y, esp) &&
                    Approximately(o.z, v.z, esp))
                {
                    added = false;
                    return i;
                }
            }

            added = true;
            vertices.Add(v);
            return vertices.Count - 1;
        }

        public static Vector3 CalculateCenter(List<Vector3> vertices)
        {
            Vector3 center = Vector3.zero;
            foreach (var vert in vertices)
            {
                center += vert;
            }

            if (vertices.Count > 0)
            {
                center /= vertices.Count;
            }

            return center;
        }

        public static Vector3 CalculateCenterAndLocalVertices(List<Vector3> vertices, out List<Vector3> localVertices)
        {
            localVertices = new List<Vector3>(vertices.Count);

            Vector3 center = Vector3.zero;
            foreach (var vert in vertices)
            {
                center += vert;
            }

            if (vertices.Count > 0)
            {
                center /= vertices.Count;
            }

            for (var i = 0; i < vertices.Count; ++i)
            {
                localVertices.Add(vertices[i] - center);
            }

            return center;
        }

        public static bool IsClockwiseWinding(this List<Vector3> polygon)
        {
            if (polygon == null)
            {
                return false;
            }
            float total = 0;
            for (var i = 0; i < polygon.Count; ++i)
            {
                var cur = polygon[i];
                var next = polygon[(i + 1) % polygon.Count];
                total += (next.x - cur.x) * (next.z + cur.z);
            }
            return total > 0;
        }

        public static Rect GetBounds2D(this List<Vector2> vertices)
        {
            var minX = float.MaxValue;
            var minZ = float.MaxValue;
            var maxX = float.MinValue;
            var maxZ = float.MinValue;
            foreach (var vert in vertices)
            {
                if (vert.x > maxX)
                {
                    maxX = vert.x;
                }

                if (vert.y > maxZ)
                {
                    maxZ = vert.y;
                }

                if (vert.x < minX)
                {
                    minX = vert.x;
                }

                if (vert.y < minZ)
                {
                    minZ = vert.y;
                }
            }

            return new Rect(minX, minZ, maxX - minX, maxZ - minZ);
        }

        public static Rect GetBounds2D(this List<Vector3> vertices)
        {
            if (vertices == null || vertices.Count == 0)
            {
                return new Rect();
            }

            var minX = float.MaxValue;
            var minZ = float.MaxValue;
            var maxX = float.MinValue;
            var maxZ = float.MinValue;
            foreach (var vert in vertices)
            {
                if (vert.x > maxX)
                {
                    maxX = vert.x;
                }

                if (vert.z > maxZ)
                {
                    maxZ = vert.z;
                }

                if (vert.x < minX)
                {
                    minX = vert.x;
                }

                if (vert.z < minZ)
                {
                    minZ = vert.z;
                }
            }

            return new Rect(minX, minZ, maxX - minX, maxZ - minZ);
        }

        public static long Min(long a, long b)
        {
            return a < b ? a : b;
        }

        public static long Max(long a, long b)
        {
            return a > b ? a : b;
        }

        public static int Loop(int k, int n)
        {
            if (k >= 0)
            {
                return k % n;
            }
            return k + n;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);  // 生成 0 到 n（含）的随机数
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

        /// <summary>
        /// 获取类中所有带有指定 Attribute 的字段
        /// </summary>
        /// <param name="targetType">要搜索的类类型</param>
        /// <param name="includeInherited">是否包含继承的基类字段</param>
        /// <typeparam name="TAttribute">目标 Attribute 类型</typeparam>
        public static List<FieldInfo> GetFieldsWithAttribute<TAttribute>(
            Type targetType,
            bool includeInherited = false
        ) where TAttribute : Attribute
        {
            List<FieldInfo> result = new();
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            Type currentType = targetType;
            while (currentType != null && currentType != typeof(object))
            {
                // 获取当前类型的所有字段（根据 includeInherited 决定是否包含基类）
                FieldInfo[] fields = currentType.GetFields(bindingFlags | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    var attribute = field.GetCustomAttribute(typeof(TAttribute), false);
                    if (attribute != null)
                    {
                        result.Add(field);
                    }
                }

                // 是否继续遍历基类
                if (!includeInherited) break;
                currentType = currentType.BaseType;
            }

            return result;
        }

        public static List<Type> GetClassesWithAttribute<TAttribute>()
                    where TAttribute : Attribute
        {
            List<Type> allTypes = new();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var markedTypes = assembly.GetTypes()
                                      .Where(t => t.IsClass && t.GetCustomAttribute<TAttribute>() != null)
                                      .ToList();
                allTypes.AddRange(markedTypes);
            }

            return allTypes;
        }

        public static T GetClassAttribute<T>(Type type, bool inherit = false) where T : Attribute
        {
            return type.GetCustomAttribute<T>(inherit);
        }

        /// <summary>
        /// 例如创建Variable<int>
        /// </summary>
        /// <param name="baseType">例如typeof(Variable<>)</param>
        /// <param name="genericTypes">例如typeof(int)</param>
        public static Type CreateGenericType(Type classType, Type[] genericTypes)
        {
            return classType.MakeGenericType(genericTypes);
        }

        public static ushort[] ConvertToUInt16Array(int[] array)
        {
            ushort[] ret = new ushort[array.Length];
            for (int i = 0; i < array.Length; ++i)
            {
                ret[i] = (ushort)array[i];
            }
            return ret;
        }

        public static List<T> GetReverseList<T>(List<T> list)
        {
            var newList = new List<T>(list);
            int n = newList.Count / 2;
            for (int i = 0; i < n; ++i)
            {
                T temp = newList[i];
                int k = newList.Count - 1 - i;
                newList[i] = newList[k];
                newList[k] = temp;
            }
            return newList;
        }

        public static void ReverseList<T>(List<T> list)
        {
            int n = list.Count / 2;
            for (int i = 0; i < n; ++i)
            {
                T temp = list[i];
                int k = list.Count - 1 - i;
                list[i] = list[k];
                list[k] = temp;
            }
        }

        public static void ReverseArray<T>(T[] array)
        {
            int n = array.Length / 2;
            for (int i = 0; i < n; ++i)
            {
                T temp = array[i];
                int k = array.Length - 1 - i;
                array[i] = array[k];
                array[k] = temp;
            }
        }

        /// <summary>
        /// 获取当前gui游标坐标
        /// </summary>
        /// <returns></returns>
        public static Vector2 GetCurrentCursorPos()
        {
            Rect cursorRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.Width(0),
                GUILayout.Height(0)
            );
            return cursorRect.min;
        }

        public static bool IsGameObjectTransformIdentity(GameObject obj)
        {
            return obj.transform.position == Vector3.zero &&
                   obj.transform.lossyScale == Vector3.one &&
                   obj.transform.rotation == Quaternion.identity;
        }

        public static void RenameFolder(string sourceFolder, string targetFolder)
        {
            try
            {
                // 验证原文件夹是否存在
                if (!Directory.Exists(sourceFolder))
                    throw new DirectoryNotFoundException($"源文件夹不存在: {sourceFolder}");

                // 检查目标路径是否已存在
                if (Directory.Exists(targetFolder))
                    throw new IOException($"目标文件夹已存在: {targetFolder}");

                // 执行重命名（支持非空文件夹）
                Directory.Move(sourceFolder, targetFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"重命名失败: {ex.Message}");
                throw; // 抛出异常供上层处理
            }
        }

        public static Bounds CalculateBoxColliderWorldBounds(UnityEngine.BoxCollider collider)
        {
            Vector3 worldCenter = collider.transform.TransformPoint(collider.center);
            Vector3 worldSize = Vector3.Scale(collider.size, collider.transform.lossyScale);
            return new Bounds(worldCenter, worldSize);
        }

        public static T[][] Rotate180<T>(T[][] matrix)
        {
            int n = matrix.Length;
            for (int i = 0; i < n; i++)
            {
                Array.Reverse(matrix[i]); // 左右翻转
            }

            // 上下翻转
            for (int i = 0; i < n / 2; i++)
            {
                (matrix[n - 1 - i], matrix[i]) = (matrix[i], matrix[n - 1 - i]);
            }

            return matrix;
        }

        public static T[] ToArray<T>(T[][] v, int row, int col)
        {
            T[] ret = new T[row * col];
            int idx = 0;
            for (var i = 0; i < row; i++)
            {
                for (var j = 0; j < col; j++)
                {
                    ret[idx++] = v[i][j];
                }
            }
            return ret;
        }

        public static T[][] ToArray2D<T>(T[] v, int row, int col)
        {
            T[][] ret = new T[row][];
            for (var i = 0; i < row; i++)
            {
                ret[i] = new T[col];
            }

            int idx = 0;
            for (var i = 0; i < row; ++i)
            {
                for (var j = 0; j < col; ++j)
                {
                    ret[i][j] = v[idx++];
                }
            }

            return ret;
        }

        public static string ToString<T>(T[] arr)
        {
            StringBuilder builder = new();
            foreach (var s in arr)
            {
                builder.Append($"{s} ");
            }
            return builder.ToString();
        }

        public static bool SegmentSegmentIntersectionTest2D(
            Vector2 aStart,
            Vector2 aEnd,
            Vector2 bStart,
            Vector2 bEnd,
            out Vector2 intersectionPoint)
        {
            intersectionPoint = Vector2.zero;
            Vector2 da = aEnd - aStart;
            Vector2 db = bEnd - bStart;
            float delta = da.y * db.x - da.x * db.y;
            if (Mathf.Approximately(delta, 0))
            {
                return false;
            }

            Vector2 k = bStart - aStart;
            float ta = (k.y * db.x - k.x * db.y) / delta;
            float tb = (k.y * da.x - k.x * da.y) / delta;
            if (ta >= 0 && ta < 1.0f && tb >= 0 && tb < 1.0f)
            {
                intersectionPoint = aStart + ta * da;
                return true;
            }

            return false;
        }

        public static bool TriangleRayIntersectionTest3D(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 rayOrigin, Vector3 rayDirection, out Vector3 barycentricCoordinate, out Vector3 normal, out float distance)
        {
            barycentricCoordinate = Vector3.zero;
            distance = 0;

            var v10 = v1 - v0;
            var v20 = v2 - v0;
            normal = Vector3.Cross(v10, v20);
            normal.Normalize();

            float dn = Vector3.Dot(rayDirection, normal);
            if (Mathf.Approximately(dn, 0))
            {
                return false;
            }

            float d = -Vector3.Dot(v0, normal);
            distance = (Vector3.Dot(rayOrigin, normal) + d) / -dn;
            var intersectionPoint = rayOrigin + rayDirection * distance;
            return PointInTriangle3D(intersectionPoint, v0, v1, v2, out barycentricCoordinate);
        }

        //找出点与边的最近点
        //offset:交点与边的偏移
        public static bool PointToSegmentDistance(Vector2 point, Vector2 s, Vector2 e, float offset, out Vector2 nearestPoint, out float minDistance)
        {
            var segmentDir = (e - s).normalized;
            var pointToStart = point - s;
            var pointToEnd = point - e;

            minDistance = 0;
            nearestPoint = Vector2.zero;
            var d1 = Vector2.Dot(pointToStart.normalized, segmentDir);
            var d2 = Vector2.Dot(pointToEnd.normalized, -segmentDir);
            bool inBetween = (d1 >= 0 && d2 >= 0);
            if (!inBetween)
            {
                return false;
            }

            var proj = Vector2.Dot(pointToStart, segmentDir) * segmentDir;
            var perp = pointToStart - proj;
            var normal = new Vector2(-segmentDir.y, segmentDir.x);
            float point2StartDistance = pointToStart.sqrMagnitude;
            float perpDistance2 = perp.sqrMagnitude;
            float point2EndDistance = pointToEnd.sqrMagnitude;

            if (Helper.LE(perpDistance2, point2StartDistance) && Helper.LE(perpDistance2, point2EndDistance))
            {
                minDistance = Mathf.Sqrt(perpDistance2);
                nearestPoint = s + proj - normal * offset;
                return true;
            }

            if (Helper.LE(point2StartDistance, perpDistance2) && Helper.LE(point2StartDistance, point2EndDistance))
            {
                minDistance = Mathf.Sqrt(point2StartDistance);
                nearestPoint = s;
                return true;
            }

            if (Helper.LE(point2EndDistance, point2StartDistance) && Helper.LE(point2EndDistance, perpDistance2))
            {
                minDistance = Mathf.Sqrt(point2EndDistance);
                nearestPoint = e;
                return true;
            }

            minDistance = Mathf.Sqrt(perpDistance2);
            nearestPoint = s + proj - normal * offset;
            return true;
        }

        public static float ChangePrecision(float x)
        {
            return Mathf.FloorToInt(x * 1000) / 1000f;
        }

        public static Transform FindChild(Transform transform, string name)
        {
            Stack<Transform> stack = new();
            stack.Push(transform);
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                if (cur.name == name)
                {
                    return cur;
                }

                foreach (Transform child in cur.transform)
                {
                    stack.Push(child);
                }
            }
            return null;
        }

        public static Transform FindChildNoAlloc(Transform transform, string name)
        {
            var n = transform.childCount;
            for (var i = 0; i < n; ++i)
            {
                var child = transform.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
                else
                {
                    var ret = FindChildNoAlloc(child, name);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }
            return null;
        }

        public static string ToHexColor(Color32 color)
        {
            StringBuilder builder = new();
            builder.Append("#");
            builder.Append(color.r.ToString("X2"));
            builder.Append(color.g.ToString("X2"));
            builder.Append(color.b.ToString("X2"));
            builder.Append(color.a.ToString("X2"));
            return builder.ToString();
        }

        public static byte ExtractBits(byte value, int startBit, int endBit)
        {
#if UNITY_EDITOR
            // 参数校验
            if (startBit < 0 || startBit > 7)
                throw new ArgumentOutOfRangeException(nameof(startBit), "Must be between 0 and 7");
            if (endBit < 0 || endBit > 7)
                throw new ArgumentOutOfRangeException(nameof(endBit), "Must be between 0 and 7");
            if (startBit > endBit)
                throw new ArgumentException("Start bit cannot be greater than end bit");
#endif
            // 创建掩码 (从 startBit 到 endBit 的位设为1)
            int maskLength = endBit - startBit + 1;
            byte mask = (byte)((1 << maskLength) - 1);
            // 应用掩码并右移对齐到第0位
            byte result = (byte)((value >> startBit) & mask);
            return result;
        }

        public static List<string> SearchFiles(string folder, string extension)
        {
            List<string> ret = new();
            var allFiles = Directory.GetFiles(folder, $"*.*", SearchOption.AllDirectories);
            foreach (var filePath in allFiles)
            {
                if (filePath.ToLower().EndsWith(".dll"))
                {
                    var unityFilePath = ToUnityPath(filePath, folder);
                    if (!string.IsNullOrEmpty(unityFilePath))
                    {
                        ret.Add(unityFilePath);
                    }
                    else
                    {
                        Debug.LogError($"无效路径: {filePath}");
                    }
                }
            }
            return ret;
        }

        public static float GetAnimLength(Animator animator)
        {
            float maxLength = 0;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.length > maxLength)
                {
                    maxLength = clip.length;
                }
            }
            return Mathf.Max(0, maxLength);
        }

        public static float GetAnimLength(Animator animator, string clipName)
        {
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == clipName)
                {
                    return clip.length;
                }
            }
            Debug.LogError($"clip {clipName} not found!");
            return 0;
        }

        public static string FindFile(string fileName, string searchDirectory)
        {
            try
            {
                var files = Directory.GetFiles(searchDirectory, fileName, SearchOption.AllDirectories);
                return files.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"搜索出错: {ex.Message}");
                return null;
            }
        }

        public static Vector2Int NextPOTSize(int pixelCount)
        {
            var size = Mathf.CeilToInt(Mathf.Sqrt(pixelCount));
            if (Mathf.NextPowerOfTwo(size) == size)
            {
                return new Vector2Int(size, size);
            }

            var textureHeight = Mathf.NextPowerOfTwo(size);
            var textureWidth = textureHeight;
            while (textureHeight != 0)
            {
                if (pixelCount > textureWidth * textureHeight)
                {
                    return new Vector2Int(textureWidth, textureHeight * 2);
                }

                textureHeight /= 2;
            }

            Debug.Assert(false);
            return Vector2Int.zero;
        }

        //左手坐标系的left turn，用左手坐标系是因为unity是左手坐标系
        public static bool IsLeftTurnLH(Vector3 a, Vector3 b)
        {
            float d = Vector3.Cross(a, b).y;
            return d <= 0;
        }

        public static int Mod(int x, int y)
        {
            if (x < 0)
            {
                return x + y;
            }

            return x % y;
        }

        public static float Mod(float x, float y)
        {
            if (x < 0)
            {
                return x + y;
            }

            return x % y;
        }

        public static int UpScale(float v)
        {
            return (int)(v * m_ScaleFactor);
        }

        public static double DownScale(long v)
        {
            return v / m_ScaleFactor;
        }

        public static bool ListEqual(List<Vector3> a, List<Vector3> b, float esp)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; ++i)
            {
                if (!Approximately(a[i], b[i], esp))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ListEqual(List<Vector2> a, List<Vector2> b, float esp)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; ++i)
            {
                if (!Approximately(a[i], b[i], esp))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ListEqual(List<int> a, List<int> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; ++i)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ListEqual(List<List<int>> a, List<List<int>> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; ++i)
            {
                if (!ListEqual(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool SegmentSegmentIntersectionTest(Vector2 aStart, Vector2 aEnd, Vector2 bStart, Vector2 bEnd,
            out Vector2 intersectionPoint)
        {
            intersectionPoint = Vector2.zero;
            Vector2 da = aEnd - aStart;
            Vector2 db = bEnd - bStart;
            float delta = da.y * db.x - da.x * db.y;
            if (Mathf.Approximately(delta, 0))
            {
                return false;
            }

            Vector2 k = bStart - aStart;
            float ta = (k.y * db.x - k.x * db.y) / delta;
            float tb = (k.y * da.x - k.x * da.y) / delta;
            if (ta >= 0 && ta < 1.0f && tb >= 0 && tb < 1.0f)
            {
                intersectionPoint = aStart + ta * da;
                return true;
            }

            return false;
        }

        public static int IndexOf(List<Vector3> vertices, Vector3 value, float esp = 0.0001f)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                if (Approximately(vertices[i], value, esp))
                {
                    return i;
                }
            }
            return -1;
        }

        public static Vector3 ConvertToLocalVertices(List<Vector3> vertices, List<Vector3> localVertices)
        {
            localVertices.Clear();
            var center = GetCenter(vertices);
            foreach (var pos in vertices)
            {
                localVertices.Add(pos - center);
            }
            return center;
        }

        internal static bool CompareEqual(RectInt oldBounds, RectInt newBounds)
        {
#if ENABLE_TUANJIE
            return oldBounds.min == newBounds.min &&
                oldBounds.max == newBounds.max;
#else
            return oldBounds == newBounds;
#endif
        }

        public static string CalculateSHA256Hash(Stream stream)
        {
            var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(stream);
            // 将字节数组转换为十六进制字符串
            var text = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            sha256.Dispose();
            return text;
        }

        public static void Clamp(ref Vector3 vector, float maxValue)
        {
            var len = vector.sqrMagnitude;
            if (len > maxValue * maxValue)
            {
                len = Mathf.Sqrt(len);
                vector *= maxValue / len;
            }
        }

        public static void ClampXZ(ref Vector3 vector, float maxValue)
        {
            var y = vector.y;
            vector.y = 0;
            var len = vector.sqrMagnitude;
            if (len > maxValue * maxValue)
            {
                len = Mathf.Sqrt(len);
                vector *= maxValue / len;
            }
            vector.y = y;
        }

        public static void ClampY(ref Vector3 vector, float maxValue)
        {
            vector.y = Mathf.Min(vector.y, maxValue);
        }

        /// <summary>
        /// 计算透视相机在指定固定Z深度处的世界单位/像素比例
        /// </summary>
        public static Vector2 CalculateScaleAtFixedDepth(Camera camera, float zDistanceToCamera)
        {
            float fovVerticalRad = camera.fieldOfView * Mathf.Deg2Rad;
            float halfHeightAtDistance = zDistanceToCamera * Mathf.Tan(fovVerticalRad * 0.5f);
            float totalVerticalHeight = halfHeightAtDistance * 2.0f;
            float totalHorizontalWidth = totalVerticalHeight * camera.aspect;
            var worldToScreenXScale = totalHorizontalWidth / Screen.width;
            var worldToScreenYScale = totalVerticalHeight / Screen.height;
            return new Vector2(worldToScreenXScale, worldToScreenYScale);
        }

        private const double m_DegToRad = 0.0174532924;
        private static float m_ScaleFactor = 10000;
    }
}

//XDay