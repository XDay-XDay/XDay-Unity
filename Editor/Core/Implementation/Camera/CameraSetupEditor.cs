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

using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.WorldAPI;

namespace XDay.CameraAPI.Editor
{
    internal class CameraSetupEditor
    {
        public void Load(string cameraSetupFilePath)
        {
            m_CameraSetupFilePath = cameraSetupFilePath;
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(cameraSetupFilePath);
            Debug.Assert(text != null, $"Invalid camera setup file path {cameraSetupFilePath}");
            m_Setup = new CameraSetup(Helper.GetPathName(cameraSetupFilePath, false));
            m_Setup.Load(text.text);
        }

        public void Save()
        {
            m_Setup.AltitudeManager.Sort();
            m_Setup.Save(m_CameraSetupFilePath);
        }

        public void SceneGUI()
        {
            var old = Handles.color;
            Handles.color = Color.magenta;
            var min = m_Setup.FocusPointBounds.min.ToVector3XZ();
            var max = m_Setup.FocusPointBounds.max.ToVector3XZ();
            Handles.DrawWireCube((min + max) / 2f, max - min);
            Handles.color = old;
        }

        public void InspectorGUI()
        {
            if (m_Setup == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            m_Show = EditorGUILayout.Foldout(m_Show, "相机设置");
            EditorGUILayout.Space();
            if (GUILayout.Button("保存", GUILayout.MaxWidth(40)))
            {
                Save();
            }
            EditorGUILayout.EndHorizontal();
            if (m_Show)
            {
                var old = EditorStyles.label.normal.textColor;
                EditorGUI.indentLevel++;
                m_Setup.Direction = (CameraDirection)EditorGUILayout.EnumPopup("朝向", m_Setup.Direction);
                m_Setup.FixedFOV = EditorGUILayout.FloatField("固定FOV", m_Setup.FixedFOV);
                m_Setup.UseNarrowView = EditorGUILayout.Toggle("使用窄视野", m_Setup.UseNarrowView);
                m_Setup.ChangeFOV = EditorGUILayout.Toggle("修改FOV", m_Setup.ChangeFOV);

                EditorStyles.label.normal.textColor = Color.green;
                m_Setup.DefaultAltitude = EditorGUILayout.FloatField(new GUIContent("默认高度", "0则忽略,由游戏内部逻辑设置成某高度"), m_Setup.DefaultAltitude);
                m_Setup.FocusPointBounds = EditorGUILayout.RectField(new GUIContent("相机可视范围", "相机视野能看到的范围, 0则忽略范围限制, 地编里可看到紫色的线包围的范围"), m_Setup.FocusPointBounds);
                m_Setup.Orbit.Pitch = EditorGUILayout.FloatField("垂直旋转角度", m_Setup.Orbit.Pitch);
                EditorStyles.label.normal.textColor = old;

                m_Setup.Orbit.Yaw = EditorGUILayout.FloatField("水平旋转角度", m_Setup.Orbit.Yaw);

                m_Setup.Restore.Distance = EditorGUILayout.FloatField("水平回弹距离", m_Setup.Restore.Distance);
                m_Setup.Restore.Duration = EditorGUILayout.FloatField("水平回弹时间", m_Setup.Restore.Duration);

                DrawAltitudeSetup();

                DrawRotationSetup();

                EditorGUI.indentLevel--;
            }
        }

        private void DrawAltitudeSetup()
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowAltitude = EditorGUILayout.Foldout(m_ShowAltitude, "高度设置");

            EditorGUILayout.Space();

            if (GUILayout.Button(new GUIContent("计算最大高度", "根据max的fov和相机可视范围,旋转角度来计算max最多能设置到多高"), GUILayout.MaxWidth(90)))
            {
                CalculateMaxAltitude();
            }

            if (GUILayout.Button(new GUIContent("排序", "按高度排序"), GUILayout.MaxWidth(40)))
            {
                m_Setup.AltitudeManager.Sort();
            }

            if (GUILayout.Button("增加", GUILayout.MaxWidth(40)))
            {
                var setup = new CameraSetup.AltitudeSetup
                {
                    Name = "New",
                    FOV = m_Setup.MaxAltitudeFOV,
                    Altitude = m_Setup.MaxAltitude
                };
                m_Setup.AltitudeManager.AddSetup(setup);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            m_InputAltitude = EditorGUILayout.FloatField("高度", m_InputAltitude);
            if (GUILayout.Button("计算FOV", GUILayout.MaxWidth(100)))
            {
                m_Setup.AltitudeManager.FOVAtAltitude(m_InputAltitude, out var fov);
                Debug.LogError($"高度{m_InputAltitude}对应的FOV:{fov}");
            }
            EditorGUILayout.EndHorizontal();

            if (m_ShowAltitude)
            {
                EditorGUI.indentLevel++;
                var altitudeManager = m_Setup.AltitudeManager;
                var deleted = false;
                for (var i = 0; i < altitudeManager.AltitudeSetups.Count; ++i)
                {
                    var setup = altitudeManager.AltitudeSetups[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 20;
                    EditorGUILayout.LabelField($"{i}", GUILayout.MaxWidth(40));
                    var enabled = setup.Name != "min" && setup.Name != "max"; ;
                    GUI.enabled = enabled;
                    setup.Name = EditorGUILayout.TextField(GUIContent.none, setup.Name, GUILayout.MaxWidth(200));
                    EditorGUIUtility.labelWidth = 80;
                    GUI.enabled = true;
                    setup.Altitude = EditorGUILayout.FloatField("高度", setup.Altitude, GUILayout.MaxWidth(200));
                    EditorGUIUtility.labelWidth = 80;
                    setup.FOV = EditorGUILayout.FloatField("FOV", setup.FOV, GUILayout.MaxWidth(200));
                    EditorGUIUtility.labelWidth = 0;
                    EditorGUILayout.Space();
                    GUI.enabled = enabled;
                    if (GUILayout.Button("删除", GUILayout.MaxWidth(40)))
                    {
                        deleted = true;
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();

                    if (deleted)
                    {
                        altitudeManager.AltitudeSetups.RemoveAt(i);
                        break;
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawRotationSetup()
        {
            m_ShowRotation = EditorGUILayout.Foldout(m_ShowRotation, "旋转设置");
            if (m_ShowRotation)
            {
                EditorGUI.indentLevel++;

                m_Setup.Orbit.EnableTouchOrbit = EditorGUILayout.Toggle("开启二指缩放", m_Setup.Orbit.EnableTouchOrbit);
                m_Setup.Orbit.EnableUnrestrictedOrbit = EditorGUILayout.Toggle("无限制缩放", m_Setup.Orbit.EnableUnrestrictedOrbit);
                GUI.enabled = !m_Setup.Orbit.EnableUnrestrictedOrbit;
                m_Setup.Orbit.Range = EditorGUILayout.FloatField("旋转范围(0-360°)", m_Setup.Orbit.Range);
                GUI.enabled = true;
                m_Setup.Orbit.MinAltitude = EditorGUILayout.FloatField("最小可旋转高度", m_Setup.Orbit.MinAltitude);
                m_Setup.Orbit.MaxAltitude = EditorGUILayout.FloatField("最大可旋转高度", m_Setup.Orbit.MaxAltitude);

                EditorGUI.indentLevel--;
            }
        }

        private void CalculateMaxAltitude()
        {
            var userSetSize = m_Setup.FocusPointBounds.size;
            if (userSetSize.x <= 0 || userSetSize.y <= 0)
            {
                EditorUtility.DisplayDialog("出错了", "没有设置相机可视范围,无法计算最大高度", "确定");
                return;
            }

            var xy = m_Setup.Direction == CameraDirection.XY;
            var calculator = new SLGCameraVisibleAreaCalculator(xy);
            var obj = new GameObject();
            var camera = obj.AddComponent<Camera>();
            var fov = m_Setup.MaxAltitudeFOV;
            var xRot = m_Setup.Orbit.Pitch;
            var yRot = m_Setup.Orbit.Yaw;
            var maxHeight = m_Setup.MaxAltitude;

            var maxSize = CalculateVisibleSize(camera, fov, xRot, yRot, xy, maxHeight, calculator);

            if (maxSize.x <= userSetSize.x && maxSize.y <= userSetSize.y)
            {
                return;
            }

            var threshold = 10f;
            var minHeight = m_Setup.MinAltitude;
            var maxTryCount = 100;
            var n = 0;
            //最高可视范围超过了玩家设置的范围,必须限制最高相机高度
            while (n < maxTryCount)
            {
                var middleHeight = (minHeight + maxHeight) / 2;
                var size = CalculateVisibleSize(camera, fov, xRot, yRot, xy, middleHeight, calculator);
                var dx = size.x - userSetSize.x;
                var dy = size.y - userSetSize.y;
                if (Mathf.Abs(dx) <= threshold &&
                    Mathf.Abs(dy) <= threshold)
                {
                    m_Setup.MaxAltitude = middleHeight;
                    break;
                }
                if (dx < 0 && dy < 0)
                {
                    maxHeight = middleHeight;
                }
                else if (dx > 0 && dy > 0)
                {
                    minHeight = middleHeight;
                }
                else
                {
                    Debug.Assert(false);
                }
                ++n;
            }

            if (n == maxTryCount)
            {
                Debug.LogError("没有找到正确的高度!");
            }

            Helper.DestroyUnityObject(obj);
        }

        private Vector2 CalculateVisibleSize(Camera camera, float fov, float xRot, float yRot, bool xy, float cameraHeight, SLGCameraVisibleAreaCalculator calculator)
        {
            var cameraTransform = camera.transform;
            var oldPos = cameraTransform.position;
            var oldRot = cameraTransform.rotation;
            var oldFOV = camera.fieldOfView;
            if (fov > 0)
            {
                camera.fieldOfView = fov;
            }
            if (xy)
            {
                cameraTransform.position = new Vector3(0, 0, -cameraHeight);
            }
            else
            {
                cameraTransform.position = new Vector3(0, cameraHeight, 0);
            }
            cameraTransform.rotation = Quaternion.Euler(xRot, yRot, 0);
            var area = calculator.GetVisibleAreas(camera);
            cameraTransform.position = oldPos;
            cameraTransform.rotation = oldRot;
            camera.fieldOfView = oldFOV;
            return area.size;
        }

        private bool m_Show = true;
        private bool m_ShowAltitude = true;
        private CameraSetup m_Setup;
        private string m_CameraSetupFilePath;
        private float m_InputAltitude;
        private bool m_ShowRotation = false;
    }
}