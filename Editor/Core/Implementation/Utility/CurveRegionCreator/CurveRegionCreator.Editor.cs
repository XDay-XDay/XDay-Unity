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

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor
{
    public partial class CurveRegionCreator
    {
        public void HandleEditControlPointFunction(Event e, Vector3 worldPos)
        {
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.alt == false)
            {
                if (e.type == EventType.MouseDown)
                {
                    Pick(worldPos);
                }
                MoveControlPoint(worldPos);
                   
                SceneView.RepaintAll();
            }

            //draw tangent line
            if (m_SelectedControlPointIndex >= 0)
            {
                var t = GetSelectedTerritory();
                var controlPoint = t.ControlPoints[m_SelectedControlPointIndex];
                Handles.DrawLine(controlPoint.Position, controlPoint.Tangent0);
                Handles.DrawLine(controlPoint.Position, controlPoint.Tangent1);
            }

            HandleUtility.AddDefaultControl(0);
        }

        private int TryPickControlPoint(Vector3 worldPos, Territory t)
        {
            float pickRadius2 = m_Input.Settings.VertexDisplayRadius;
            pickRadius2 *= pickRadius2;
            var controlPoints = t.ControlPoints;
            for (int i = 0; i < controlPoints.Count; ++i)
            {
                var d = worldPos - controlPoints[i].Position;
                if (d.sqrMagnitude <= pickRadius2)
                {
                    return i;
                }
            }
            return -1;
        }

        private void PickControlPoint(Vector3 worldPos, Territory t)
        {
            m_Mover.Reset();
            m_SelectedControlPointIndex = TryPickControlPoint(worldPos, t);
        }

        private void MoveControlPoint(Vector3 worldPos)
        {
            if (m_SelectedTerritoryID > 0 && m_SelectedControlPointIndex >= 0)
            {
                m_Mover.Update(worldPos);

                var delta = m_Mover.GetMovement();
                if (delta != Vector2.zero)
                {
                    var territory = GetSelectedTerritory();
                    GetControlPointsWithSameCoordinates(territory.ControlPoints[m_SelectedControlPointIndex].Position);
                    if (m_SelectedTangentPointIndex >= 0)
                    {
                        var type = TangentMoveType.RotateAndScale;
                        for (int i = 0; i < m_ControlPointsWithSameCoordinates.Count; ++i)
                        {
                            m_ControlPointsWithSameCoordinates[i].MoveTangent(m_SelectedTangentPointIndex, delta, type);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < m_ControlPointsWithSameCoordinates.Count; ++i)
                        {
                            var controlPoint = m_ControlPointsWithSameCoordinates[i];
                            controlPoint.MoveControlPoint(delta);
                            controlPoint.MoveTangent(0, delta, TangentMoveType.Free);
                            controlPoint.MoveTangent(1, delta, TangentMoveType.Free);
                        }
                    }
                    
                    Generate("", 0, false, false, m_Width, m_Height, 3, 3, 0, null);
                }
            }
        }

        //todo optimize this
        private void GetControlPointsWithSameCoordinates(Vector3 pos)
        {
            m_ControlPointsWithSameCoordinates.Clear();
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                var controlPoints = m_Territories[i].ControlPoints;
                for (int j = 0; j < controlPoints.Count; ++j)
                {
                    if (Helper.Approximately(controlPoints[j].Position, pos, 0.01f))
                    {
                        m_ControlPointsWithSameCoordinates.Add(controlPoints[j]);
                    }
                }
            }
        }

        private void Pick(Vector3 worldPos)
        {
            m_SelectedTangentPointIndex = -1;
            var selectedTerritory = GetSelectedTerritory();
            if (selectedTerritory != null && m_SelectedControlPointIndex >= 0)
            {
                int idx = TryPickControlPoint(worldPos, selectedTerritory);
                if (idx < 0)
                {
                    PickTangent(worldPos, selectedTerritory);
                }
                else
                {
                    m_SelectedTangentPointIndex = -1;
                }
            }

            if (m_SelectedTangentPointIndex < 0)
            {
                SetSelectedTerritory(null);
                for (int i = 0; i < m_Territories.Count; ++i)
                {
                    PickControlPoint(worldPos, m_Territories[i]);
                    int selectedControlPointIndex = m_SelectedControlPointIndex;
                    if (selectedControlPointIndex >= 0)
                    {
                        SetSelectedTerritory(m_Territories[i]);
                        m_SelectedControlPointIndex = selectedControlPointIndex;
                        m_Territories[i].ShowTangent(selectedControlPointIndex);
                        break;
                    }
                }
            }
        }

        private void PickTangent(Vector3 worldPos, Territory t)
        {
            m_Mover.Reset();
            m_SelectedTangentPointIndex = -1;
            float pickRadius2 = m_Input.Settings.VertexDisplayRadius;
            pickRadius2 *= pickRadius2;
            var controlPoint = t.ControlPoints[m_SelectedControlPointIndex];
            var d = worldPos - controlPoint.Tangent0;
            if (d.sqrMagnitude <= pickRadius2)
            {
                m_SelectedTangentPointIndex = 0;
            }
            else
            {
                d = worldPos - controlPoint.Tangent1;
                if (d.sqrMagnitude <= pickRadius2)
                {
                    m_SelectedTangentPointIndex = 1;
                }
            }
        }

        private void SetSelectedTerritory(Territory t)
        {
            var oldSpline = GetSelectedTerritory();
            if (oldSpline != null)
            {
                oldSpline.HideTangent();
            }

            if (t != null)
            {
                m_SelectedTerritoryID = t.RegionID;
            }
            else
            {
                m_SelectedTerritoryID = 0;
            }
            m_SelectedControlPointIndex = -1;
            m_SelectedTangentPointIndex = -1;
        }

        private Territory GetTerritory(int id)
        {
            if (id == 0)
            {
                return null;
            }

            for (int i = 0; i < m_Territories.Count; ++i)
            {
                if (m_Territories[i].RegionID == id)
                {
                    return m_Territories[i];
                }
            }
            return null;
        }

        private Territory GetSelectedTerritory()
        {
            return GetTerritory(m_SelectedTerritoryID);
        }

        private int m_SelectedTerritoryID = 0;
        private int m_SelectedControlPointIndex = -1;
        private int m_SelectedTangentPointIndex = -1;
        private IMover m_Mover = IMover.Create();
        private List<ControlPoint> m_ControlPointsWithSameCoordinates = new List<ControlPoint>();
    }
}

