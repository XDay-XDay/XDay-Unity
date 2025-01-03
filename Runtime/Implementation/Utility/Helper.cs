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
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    UnityEngine.Object.DestroyImmediate(obj);
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
        }

        public static string RemoveExtension(string path)
        {
            var idx = path.LastIndexOf('.');
            if (idx == -1)
            {
                return path;
            }
            return path.Substring(0, idx);
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

        public static Vector3 FromFocusPoint(Camera camera, Vector3 focusPoint, float y)
        {
            focusPoint.y = 0;
            var cameraTransform = camera.transform;
            var viewDistance = FocalLengthFromAltitude(cameraTransform.eulerAngles.x, y);
            var cameraPos = focusPoint - cameraTransform.forward * viewDistance;
            return cameraPos;
        }

        public static float FocalLengthFromAltitude(float rotationX, float y)
        {
            return (float)((double)y / System.Math.Sin(rotationX * m_DegToRad));
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

        public static Type[] QueryTypes<T>(bool isAbstract)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes()).Where(type => typeof(T).IsAssignableFrom(type) && type.IsAbstract == isAbstract).ToArray();
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
                Debug.LogError("bounds is empty");
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
            var oldPos = transform.position;
            var oldRot = transform.rotation;
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
            var oldPos = transform.position;
            var oldRot = transform.rotation;
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

        public static Color Lerp(this Color a, Color b, float t)
        {
            return new Color(Mathf.Lerp(a.r, b.r, t),
                             Mathf.Lerp(a.g, b.g, t),
                             Mathf.Lerp(a.b, b.b, t),
                             Mathf.Lerp(a.a, b.a, t));
        }

        private const double m_DegToRad = 0.0174532924;
    }
}

//XDay