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
            }
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
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

        public static string ToUnityPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            path = ToNixSlash(path);

            var index = path.IndexOf("Assets/", System.StringComparison.Ordinal);
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
            Debug.Assert(y > 0);
            return (float)((double)y / System.Math.Sin(rotationX * m_DegToRad));
        }

        public static float FocalLengthFromAltitudeXY(float rotationX, float z)
        {
            if (z <= 0)
            {
                Debug.Assert(false);
            }
            
            if (rotationX == 0)
            {
                return z;
            }
            return (float)((double)z / System.Math.Sin((90 - rotationX) * m_DegToRad));
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

        public static bool Approximately(float a, float b, float esp = 0.0001f)
        {
            if (Mathf.Abs(a - b) <= esp)
            {
                return true;
            }
            return false;
        }

        public static Rect ExpandRect(Rect rect, Vector2 size)
        {
            var minX = rect.xMin - size.x * 0.5f;
            var minY = rect.yMin - size.y * 0.5f;
            var maxX = rect.xMax + size.x * 0.5f;
            var maxY = rect.yMax + size.y * 0.5f;

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
            var rtIn = PointInPolygon(max, polygon);
            var ltIn = PointInPolygon(new Vector3(min.x, 0, max.z), polygon);
            var rbIn = PointInPolygon(new Vector3(max.x, 0, min.z), polygon);
            var centerIn = PointInPolygon((min + max) / 2, polygon);
            return lbIn || rtIn || centerIn || ltIn || rbIn;
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

        public static Vector3 FindClosestPointToSegement(Vector3 p, Vector3 segmentStart, Vector3 segmentEnd)
        {
            var dir = segmentEnd - segmentStart;
            var ps = p - segmentStart;
            var proj = Vector3.Dot(dir, ps);
            proj /= dir.sqrMagnitude;
            if (proj >= 1)
            {
                return segmentEnd;
            }
            if (proj <= 0)
            {
                return segmentStart;
            }
            return Vector3.Lerp(segmentStart, segmentEnd, proj);
        }

        public static void PointToLineSegmentDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd, out float distance, out bool interior)
        {
            var pointToStart = point - lineStart;
            var pointToEnd = point - lineEnd;
            pointToEnd.Normalize();

            var lineDirection = (lineEnd - lineStart).normalized;
            var dStart = Vector3.Dot(pointToStart, lineDirection);
            var dEnd = Vector3.Dot(pointToEnd, -lineDirection);

            var pointProjectionOnLine = Vector3.Dot(pointToStart, lineDirection) * lineDirection;
            var perpendicularDirection = pointToStart - pointProjectionOnLine;

            pointToStart.Normalize();

            interior = dStart >= 0 && dEnd >= 0;
            distance = perpendicularDirection.magnitude;
        }

        public static int FindClosestEdgeOnProjection(Vector3 point, List<Vector3> polygon)
        {
            var closestEdgeIndex = -1;
            var minimumDistance = float.MaxValue;
            for (var edgeIndex = 0; edgeIndex < polygon.Count; ++edgeIndex)
            {
                PointToLineSegmentDistance(point, polygon[edgeIndex], polygon[(edgeIndex + 1) % polygon.Count], out var distance, out var interior);
                if (interior && distance < minimumDistance)
                {
                    closestEdgeIndex = edgeIndex;
                    minimumDistance = distance;
                }
            }

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

        public static string GetObjectGUID(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(obj);
            return AssetDatabase.AssetPathToGUID(path);
#else
            return "";
#endif
        }

        public static Bounds CalculateBounds(List<Vector3> points)
        {
            if (points.Count == 0)
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

        private const double m_DegToRad = 0.0174532924;
    }
}

//XDay