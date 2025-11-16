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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;

namespace XDay.ModelBuildPipeline.Editor
{
    internal partial class ModelBuildPipelineView
    {
        protected override void OnDrawGUI()
        {
            DrawLeft();

            DrawSplitter();

            DrawRight();

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
            {
                var stage = GetSelectedStageView();
                if (stage != null)
                {
                    DeleteOneStage(true);
                    e.Use();
                }
                else if (m_SelectionInfo.Connections.Count > 0)
                {
                    var con = m_SelectionInfo.Connections[0];
                    con.StageView.Stage.RemovePreviousStage(con.PrevStageView.Stage.ID);
                    e.Use();
                }
            }
        }

        private void DrawLeft()
        {
            m_ViewportArea = new Rect(0, 0, m_SplitterPosition * m_WindowContentWidth, m_WindowContentHeight);
            GUILayout.BeginArea(m_ViewportArea, EditorStyles.helpBox);

            DrawGrid();

            if (m_TextFieldStyle == null)
            {
                m_TextFieldStyle = new(GUI.skin.textField)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (m_LabelFieldStyle == null)
            {
                m_LabelFieldStyle = new(GUI.skin.label);
                m_LabelFieldStyle.normal.textColor = new Color32(255, 127, 39, 255);
                m_LabelFieldStyle.fontStyle = FontStyle.Bold;
                m_LabelFieldStyle.fontSize = Mathf.FloorToInt(m_LabelFieldStyle.fontSize * 1.2f);
                m_LabelFieldStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (m_CommentFieldStyle == null)
            {
                m_CommentFieldStyle = new(GUI.skin.textArea)
                {
                    alignment = TextAnchor.LowerLeft
                };
            }

            if (m_TextAreaStyle == null)
            {
                m_TextAreaStyle = new GUIStyle(EditorStyles.textArea);
                m_TextAreaStyle.normal.background = m_ButtonBackground;
                m_TextAreaStyle.active.background = m_ButtonBackground;
                m_TextAreaStyle.focused.background = m_ButtonBackground;
            }

            Handles.BeginGUI();

            var oldColor = Handles.color;
            Handles.color = Color.white;
            foreach (var stage in m_AllStages)
            {
                DrawStageConnection(stage);
            }

            DrawDragLine();

            Handles.color = oldColor;

            //draw stages
            foreach (var stage in m_AllStages)
            {
                DrawStage(stage);
            }

            DrawDescription();

            DrawDebug();

            Handles.EndGUI();

            GUILayout.EndArea();
        }

        private void DrawRight()
        {
            GUILayout.BeginArea(new Rect(m_SplitterPosition * m_WindowContentWidth + 5, 0, m_WindowContentWidth - m_SplitterPosition * m_WindowContentWidth - 5, m_WindowContentHeight), EditorStyles.helpBox);

            DrawPipelineSelection();

            GUI.enabled = !Application.isPlaying;
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("New"))
            {
                if (m_Pipeline != null)
                {
                    if (EditorUtility.DisplayDialog("注意", "当前有管线正在编辑,确定创建新的?", "确定", "取消"))
                    {
                        Save();
                        CreateNew("New");
                    }
                }
                else
                {
                    CreateNew("New");
                }
            }

            if (GUILayout.Button("Save"))
            {
                Save();
            }

            if (GUILayout.Button("Reset"))
            {
                Reset();
            }

            if (GUILayout.Button("重命名"))
            {
                Rename();
            }

            if (GUILayout.Button("选中", GUILayout.MaxWidth(40)))
            {
                if (m_Pipeline != null)
                { 
                    Selection.activeObject = m_Pipeline;
                    EditorGUIUtility.PingObject(m_Pipeline);
                }
            }

            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            DrawToolbar();

            GUILayout.EndArea();
        }

        private void Rename()
        {
            if (m_Pipeline != null)
            {
                var path = AssetDatabase.GetAssetPath(m_Pipeline);
                if (!string.IsNullOrEmpty(path))
                {
                    var oldName = Helper.GetPathName(path, false);
                    var parameters = new List<ParameterWindow.Parameter>()
                    {
                        new ParameterWindow.StringParameter("新名字", "", oldName),
                    };
                    ParameterWindow.Open("重命名", parameters, (p) =>
                    {
                        var ok = ParameterWindow.GetString(p[0], out var newName);
                        if (ok)
                        {
                            var error = AssetDatabase.RenameAsset(path, newName);
                            if (string.IsNullOrEmpty(error))
                            {
                                //也重命名ModelBuildMetadata里的引用
                                var metadatas = EditorHelper.QueryAssets<ModelBuildMetadata>();
                                foreach(var metadata in metadatas)
                                {
                                    if (metadata.ModelBuildPipelineName == oldName)
                                    {
                                        Debug.Log($"修改了{metadata.name}里的引用");
                                        metadata.ModelBuildPipelineName = newName;
                                        EditorUtility.SetDirty(metadata);
                                    }
                                }
                                AssetDatabase.SaveAssets();
                            }
                            else
                            {
                                Debug.LogError(error);
                            }
                        }
                        return ok;
                    });
                }
            }
        }

        private void DrawToolbar()
        {
            GUI.enabled = m_Pipeline != null;
            DrawStagePage();
            GUI.enabled = true;
        }

        private void DrawSplitter()
        {
            var splitterRect = new Rect(m_SplitterPosition * m_WindowContentWidth, 0, 5, m_WindowContentHeight);
            EditorGUI.DrawRect(splitterRect, Color.gray);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            // Dragging logic for the splitter
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                m_IsDragging = true;
            }
            if (m_IsDragging && Event.current.type == EventType.MouseDrag)
            {
                m_SplitterPosition += Event.current.delta.x / m_WindowContentWidth;
                Repaint();
            }
            if (Event.current.type == EventType.MouseUp) m_IsDragging = false;
        }

        protected override void OnMouseButtonPressed(int button, Vector2 mousePos)
        {
            if (m_Pipeline == null)
            {
                return;
            }
            if (button == 0)
            {
                if (!m_ViewportArea.Contains(mousePos))
                {
                    return;
                }
                m_ValidClick = true;
                m_DrawDragLine = true;

                TimeSpan timeSinceLastClick = DateTime.Now - lastClickTime;
                Vector2 deltaPosition = mousePos - lastClickPosition;
                // 判断是否满足双击条件
                if (timeSinceLastClick.TotalSeconds < DoubleClickTime &&
                    deltaPosition.magnitude < PositionTolerance)
                {
                    OnDoubleClick();
                    Event.current.Use(); // 标记事件已处理
                }
                else
                {
                    PickObject(mousePos);
                }

                // 记录当前点击信息
                lastClickTime = DateTime.Now;
                lastClickPosition = mousePos;
            }
            else if (button == 1)
            {
                PickObject(mousePos);

                GenericMenu menu = new();
                if (m_SelectionInfo.Stages.Count > 0)
                {
                    menu.AddItem(new GUIContent("删除"), false, () => { DeleteOneStage(true); });
                    menu.AddItem(new GUIContent("打开脚本"), false, OpenCSFile);
                }
                AddStageMenuItems(menu, mousePos);
                menu.AddItem(new GUIContent("重置视野"), false, ResetViewPosition);

                menu.ShowAsContext();

                SceneView.RepaintAll();
            }
        }

        protected override void OnMouseButtonReleased(int button, Vector2 mousePos)
        {
            if (m_Pipeline == null)
            {
                return;
            }

            if (button == 0)
            {
                m_ValidClick = false;
                m_DrawDragLine = false;
                Repaint();
                var selectionInfo = new SelectionInfo();
                PickObjectOnly(mousePos, selectionInfo, true);
                if (selectionInfo.Stages.Count > 0)
                {
                    if (m_SelectionInfo.Part == Part.Top &&
                        selectionInfo.Part == Part.Bottom)
                    {
                        var childStage = m_SelectionInfo.Stages[0].Stage;
                        var parentStage = selectionInfo.Stages[0].Stage;
                        if (childStage != parentStage)
                        {
                            childStage.AddPreviousStage(parentStage.ID);
                        }
                    }
                    else if (m_SelectionInfo.Part == Part.Bottom &&
                        selectionInfo.Part == Part.Top)
                    {
                        var parentStage = m_SelectionInfo.Stages[0].Stage;
                        var childStage = selectionInfo.Stages[0].Stage;
                        if (childStage != parentStage)
                        {
                            childStage.AddPreviousStage(parentStage.ID);
                        }
                    }
                }
            }
        }

        protected override void OnMouseDrag(int button, Vector2 movement)
        {
            if (m_Pipeline == null)
            {
                return;
            }

            if (button == 0 && movement != Vector2.zero)
            {
                var e = Event.current;
                if (m_ValidClick)
                {
                    if (m_SelectionInfo.Stages.Count > 0)
                    {
                        if (m_SelectionInfo.Part == Part.Center)
                        {
                            foreach (var stage in m_SelectionInfo.Stages)
                            {
                                MoveStage(stage, movement);
                            }
                        }
                    }
                }
            }
        }

        private void DrawDragLine()
        {
            if (m_DrawDragLine)
            {
                var e = Event.current;
                if (m_SelectionInfo.Part == Part.Top)
                {
                    var start = GetSelectedStageView().GetTopCenter();
                    var end = e.mousePosition;
                    Handles.DrawAAPolyLine(5, World2Window(start), end);
                }
                else if (m_SelectionInfo.Part == Part.Bottom)
                {
                    var start = GetSelectedStageView().GetBottomCenter();
                    var end = e.mousePosition;
                    Handles.DrawAAPolyLine(5, World2Window(start), end);
                }
            }
        }

        private void DrawStageConnection(StageView stageView)
        {
            var color = Handles.color;
            var stage = stageView.Stage;
            foreach (var prevStageID in stage.PreviousStageIDs)
            {
                var prevStageView = GetStageView(prevStageID);
                var topCenter = stageView.GetTopCenter();
                var bottomCenter = prevStageView.GetBottomCenter();

                Handles.color = Color.gray;
                if (IsSelected(stageView, prevStageView))
                {
                    Handles.color = Color.cyan;
                }
                Handles.DrawAAPolyLine(m_LineWidth, World2Window(topCenter), World2Window(bottomCenter));
            }
        }

        private bool IsSelected(StageView stageView, StageView prevStageView)
        {
            foreach (var con in m_SelectionInfo.Connections)
            {
                if (con.StageView == stageView && con.PrevStageView == prevStageView)
                {
                    return true;
                }
            }
            return false;
        }

        private void MoveStage(StageView stageView, Vector2 movement)
        {
            ReleaseGrids(stageView);
            stageView.Move(movement);
            OccupyGrids(stageView);
        }

        private void DrawStage(StageView stageView)
        {
            //draw stageView
            DrawIcon(stageView);

            var stage = stageView.Stage;

            DrawTop(stageView);

            DrawBottom(stageView);

            stage.Name = DrawTextAt(stageView.Stage.Name, stageView.Size.x, stageView.WorldPosition + new Vector2(0, stageView.Size.y));

            if (stageView.Stage.ShowComment)
            {
                bool left = false;
                Vector2 position = stageView.WorldPosition + stageView.Size;
                stage.Comment = DrawCommentAt(stage.Comment, stageView.WorldPosition, stageView.Size, left);
            }
        }

        private string DrawCommentAt(string text, Vector2 worldPosition, Vector2 size, bool left)
        {
            var textSize = GUI.skin.textField.CalcSize(new GUIContent(text));
            if (textSize.x > m_MaxCommentAreaWidth)
            {
                textSize.x = m_MaxCommentAreaWidth;
            }

            float areaWidth = textSize.x + m_XPadding;
            float areaHeight = textSize.y + m_YPadding;

            worldPosition.y += size.y;
            if (left)
            {
                worldPosition.x -= areaWidth;
            }
            else
            {
                worldPosition.x += size.x;
            }
            var textPos = World2Window(worldPosition);

            Rect textRect = new(textPos, new Vector2(WorldLengthToWindowLength(areaWidth), WorldLengthToWindowLength(areaHeight)));
            return EditorGUI.TextArea(textRect, text);
        }

        private void DrawDescription()
        {
            var stageView = GetSelectedStageView();
            if (stageView == null)
            {
                return;
            }
            var attribute = Helper.GetClassAttribute<StageDescriptionAttribute>(stageView.Stage.GetType());
            if (attribute != null && !string.IsNullOrEmpty(attribute.Description))
            {
                GUI.enabled = false;
                EditorGUI.TextArea(new Rect(0, m_WindowContentHeight - m_DescriptionHeight, m_DescriptionWidth, m_DescriptionHeight), attribute.Description, m_TextAreaStyle);
                GUI.enabled = true;
            }
        }

        private string DrawTextAt(string name, float worldWidth, Vector2 worldPosition)
        {
            var minZoom = m_Viewer.GetMinZoom();
            var maxZoom = m_Viewer.GetMaxZoom();
            float t = (m_Viewer.GetZoom() - minZoom) / (maxZoom - minZoom);
            m_TextFieldStyle.fontSize = (int)(Mathf.Lerp(m_MinFontSize, m_MaxFontSize, Helper.EaseInExpo(1 - t)));
            //文字长度大于stage size,需要把字体变小
            var textPos = World2Window(worldPosition);
            Rect textRect = new(textPos, new Vector2(WorldLengthToWindowLength(worldWidth), WorldLengthToWindowLength(m_NameHeight)));
            name = EditorGUI.TextField(textRect, name, m_TextFieldStyle);
            return name;
        }

        private void DrawLabelAt(string name, float worldWidth, Vector2 worldPosition)
        {
            var minZoom = m_Viewer.GetMinZoom();
            var maxZoom = m_Viewer.GetMaxZoom();
            float t = (m_Viewer.GetZoom() - minZoom) / (maxZoom - minZoom);
            m_TextFieldStyle.fontSize = (int)(Mathf.Lerp(m_MinFontSize, m_MaxFontSize, Helper.EaseInExpo(1 - t)));
            //文字长度大于stage size,需要把字体变小
            var textPos = World2Window(worldPosition);
            Rect textRect = new(textPos, new Vector2(WorldLengthToWindowLength(worldWidth), WorldLengthToWindowLength(m_NameHeight)));
            EditorGUI.LabelField(textRect, name, m_LabelFieldStyle);
        }

        private void DrawButtonAt(string name, float worldWidth, Vector2 worldPosition, System.Action<int> onClickButton, bool alignCenter, Color color)
        {
            var textSize = GUI.skin.button.CalcSize(new GUIContent(name));
            float offset = (worldWidth / m_Viewer.GetZoom() - textSize.x) / 2;
            if (offset < 0)
            {
                //文字长度大于stage size,需要把字体变小
            }
            var textPos = World2Window(worldPosition);
            if (alignCenter)
            {
                textPos.x += offset;
            }
            Rect textRect = new Rect(textPos, textSize);
            var style = GUI.skin.button;
            style.normal.background = m_ButtonBackground;
            var originalColor = style.normal.textColor;
            style.normal.textColor = color;
            if (GUI.Button(textRect, name, style))
            {
                if (onClickButton != null)
                {
                    onClickButton(Event.current.button);
                }
            }
            style.normal.textColor = originalColor;
            style.normal.background = null;
        }

        private void DrawIcon(StageView stageView)
        {
            var pos = stageView.WorldPosition;
            var size = stageView.Size;
            Color color = m_IconBackgroundColor;
            if (m_SelectionInfo.Stages.Contains(stageView))
            {
                color = m_IconSelectColor;
            }
            
            float expandWidth = 0;
            float expandHeight = 0;
            float outlineMinX = pos.x - expandWidth;
            float outlineMinY = pos.y - expandHeight;
            float outlineMaxX = outlineMinX + size.x + expandWidth * 2;
            float outlineMaxY = outlineMinY + size.y + expandHeight * 2;
            DrawRect(World2Window(new Vector2(outlineMinX, outlineMinY)), World2Window(new Vector2(outlineMaxX, outlineMaxY)), color);

            float iconMinX = pos.x + 5;
            float iconMinY = pos.y + 5;
            float iconMaxX = iconMinX + size.x - 10;
            float iconMaxY = iconMinY + size.y - m_NameHeight - 10;

            var min = World2Window(new Vector2(iconMinX, iconMinY));
            var max = World2Window(new Vector2(iconMaxX, iconMaxY));

            DrawTexture(min, max, stageView.Icon);

            float width = stageView.Size.x;
            DrawOrder(stageView.WorldPosition + new Vector2(stageView.Size.x / 2 - width / 2, stageView.Size.y / 2), width, m_Pipeline.GetStageIndex(stageView.Stage));

            DrawStatus(stageView);
        }

        private void DrawOrder(Vector2 pos, float width, int index)
        {
            DrawLabelAt($"{index}", width, pos);
        }

        private void DrawTop(StageView stage)
        {
            Color color = m_TopConnectorColor;
            if (m_SelectionInfo.Stages.Contains(stage) && m_SelectionInfo.Part == Part.Top)
            {
                color = m_TopConnectorSelectColor;
            }
            stage.GetTopRect(out var min, out var max);
            DrawRect(World2Window(min), World2Window(max), color);
        }

        private void DrawBottom(StageView stage)
        {
            Color color = m_BottomConnectorColor;
            if (m_SelectionInfo.Stages.Contains(stage) && m_SelectionInfo.Part == Part.Bottom)
            {
                color = m_BottomConnectorSelectColor;
            }
            stage.GetBottomRect(out var min, out var max);
            DrawRect(World2Window(min), World2Window(max), color);
        }

        private void DrawHorizontalLine(float x, float y, float width, Color color)
        {
            EditorGUI.DrawRect(new Rect(x, y, width, 1), color);
        }

        private void PickObject(Vector2 mousePosInViewportSpace)
        {
            PickObjectOnly(mousePosInViewportSpace, m_SelectionInfo, true);
        }

        private void PickObjectOnly(Vector2 mousePosInViewportSpace, SelectionInfo info, bool singleSelection)
        {
            if (!m_ViewportArea.Contains(mousePosInViewportSpace))
            {
                return;
            }

            info.Stages.Clear();
            info.Connections.Clear();
            info.Part = Part.None;
            var mouseWorldPos = Window2World(mousePosInViewportSpace);

            //test stageView
            for (var i = m_AllStages.Count - 1; i >= 0; --i)
            {
                var stage = m_AllStages[i];
                var part = stage.HitTest(mouseWorldPos);
                if (part != Part.None)
                {
                    info.Stages.Add(stage);
                    info.Part = part;
                    if (singleSelection)
                    {
                        break;
                    }
                }
            }

            //test connections
            foreach (var stageView in m_AllStages)
            {
                foreach (var prevStageID in stageView.Stage.PreviousStageIDs)
                {
                    var prevStageView = GetStageView(prevStageID);
                    if (Hit(mouseWorldPos, stageView, prevStageView))
                    {
                        info.Connections.Add(new StageConnection() { StageView = stageView, PrevStageView = prevStageView });
                    }
                }
            }
        }

        public void ResetViewPosition()
        {
            m_Viewer.ResetPosition();
        }

        private void OpenCSFile()
        {
            var stageView = GetSelectedStageView();
            if (stageView == null)
            {
                return;
            }

            var path = EditorHelper.QueryScriptFilePath("ModelBuildPipeline", stageView.Stage.GetType().Name);
            EditorHelper.OpenCSFile(path, 1);
        }

        private void DeleteOneStage(bool prompt)
        {
            if (prompt && !EditorUtility.DisplayDialog("注意", "确定删除选中节点?", "确定", "取消"))
            {
                return;
            }

            var stage = GetSelectedStageView();
            if (stage != null)
            {
                m_Pipeline.RemoveStage(stage.Stage);
            }
        }

        private void OccupyGrids(StageView stage)
        {
            var size = stage.Size;
            var minCoord = WorldPositionToGridCoordinateCeil(stage.WorldPosition - size * 0.5f);
            var maxCoord = WorldPositionToGridCoordinateCeil(stage.WorldPosition + size * 0.5f);
            OccupyGrids(minCoord, maxCoord);
        }

        private void ReleaseGrids(StageView stage)
        {
            var size = stage.Size;
            var minCoord = WorldPositionToGridCoordinateCeil(stage.WorldPosition - size * 0.5f);
            var maxCoord = WorldPositionToGridCoordinateCeil(stage.WorldPosition + size * 0.5f);
            ReleaseGrids(minCoord, maxCoord);
        }

        private void AddStageMenuItem(Type type, string group, GenericMenu menu, Vector2 mousePos)
        {
            var description = Helper.GetClassAttribute<StageDescriptionAttribute>(type);
            string typeName = type.Name;
            if (description != null)
            {
                typeName = description.Title;
            }
            menu.AddItem(new GUIContent($"{group}/{typeName}"), false, () =>
            {
                m_CreateWindowPosition = mousePos;
                m_Pipeline.CreateStage(type);
            });
        }

        private void DrawDebug()
        {
            if (m_Pipeline != null)
            {
                EditorGUILayout.LabelField($"Stage数量{m_Pipeline.Stages.Count}");
                EditorGUILayout.LabelField($"Zoom {m_Viewer.GetZoom()}");
            }
        }

        private void DrawStagePage()
        {
            var stageView = GetSelectedStageView();
            if (stageView == null)
            {
                return;
            }
            var stage = stageView.Stage;
            GUI.enabled = false;
            EditorGUILayout.IntField("ID", stage.ID);
            GUI.enabled = true;

            GUI.enabled = !Application.isPlaying;
            stage.Name = EditorGUILayout.TextField("Name", stage.Name);
            stage.Comment = EditorGUILayout.TextArea(stage.Comment, m_CommentFieldStyle);

            EditorHelper.HorizontalLine();

            DrawStageSetting(stage);
            
            GUI.enabled = true;
        }

        private void DrawStageSetting(ModelBuildPipelineStage stage)
        {
            if (string.IsNullOrEmpty(m_DebugModelFolder))
            {
                EditorGUILayout.LabelField("设置了模型调试目录才能显示参数");
                return;
            }

            var settingFilePath = $"{m_DebugModelFolder}/{ModelBuildPipeline.SETTING_FOLDER_NAME}/{stage.SettingType.Name}.asset";
            var setting = AssetDatabase.LoadAssetAtPath(settingFilePath, stage.SettingType) as ModelBuildPipelineStageSetting;
            DrawProperties(setting);
        }

        private void OnDoubleClick()
        {
            if (m_SelectionInfo.Stages.Count > 0)
            {
                var stageView = m_SelectionInfo.Stages[0];
                stageView.Stage.ShowComment = !stageView.Stage.ShowComment;
            }
        }

        private void DrawPipelineSelection()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新") || m_Pipelines == null)
            {
                m_Pipelines = EditorHelper.QueryAssets<ModelBuildPipeline>();
            }
            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint)
            {
                if (m_Pipeline == null && m_Pipelines.Count > 0)
                {
                    SetPipeline(m_Pipelines[0]);
                }
            }

            GetPipelineNames(m_Pipelines);

            EditorGUIUtility.labelWidth = 100;
            var oldIndex = m_Pipelines.IndexOf(m_Pipeline);
            var index = EditorGUILayout.Popup("Pipelines", oldIndex, m_PipelineNames);
            if (index != oldIndex)
            {
                SetPipeline(m_Pipelines[index]);
            }
            EditorGUIUtility.labelWidth = 0;

            EditorGUIUtility.labelWidth = 100;

            GUI.enabled = m_Pipeline != null;
            if (m_Pipeline != null)
            {
                m_Pipeline.SortOrder = EditorGUILayout.IntField("Sort Order", m_Pipeline.SortOrder);
            }
            else
            {
                EditorGUILayout.IntField("Sort Order", 0);
            }
            GUI.enabled = true;

            if (m_Pipeline != null)
            {
                m_Pipeline.Setting = EditorGUILayout.ObjectField("设置", m_Pipeline.Setting, typeof(ModelBuildPipelineSetting), false) as ModelBuildPipelineSetting;
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.ObjectField("全局设置", null, typeof(ModelBuildPipelineSetting), false);
                GUI.enabled = true;
            }
            m_DebugModelFolder = EditorHelper.ObjectField<DefaultAsset>("调试模型文件夹", m_DebugModelFolder);
            EditorGUIUtility.labelWidth = 0;

            if (GUI.changed && m_Pipeline != null)
            {
                EditorUtility.SetDirty(m_Pipeline);
            }
        }

        private void GetPipelineNames(List<ModelBuildPipeline> pipelines)
        {
            if (m_PipelineNames == null || m_PipelineNames.Length != pipelines.Count)
            {
                m_PipelineNames = new string[pipelines.Count];
            }

            for (var i = 0; i < m_PipelineNames.Length; i++)
            {
                m_PipelineNames[i] = $"{i}-{pipelines[i].name}";
            }
        }

        private void AddStageMenuItems(GenericMenu menu, Vector2 mousePos)
        {
            var allStageTypes = Helper.GetAllSubclasses(typeof(ModelBuildPipelineStage));

            foreach (var type in allStageTypes)
            {
                var attributes = type.GetCustomAttributes(false);
                string groupName = "Stage";
                foreach (var attribute in attributes)
                {
                    if (attribute is StageGroupAttribute)
                    {
                        var group = (StageGroupAttribute)attribute;
                        groupName = group.GroupName;
                        break;
                    }
                }
                AddStageMenuItem(type, groupName, menu, mousePos);
            }
        }

        private StageView GetSelectedStageView()
        {
            if (m_SelectionInfo.Stages.Count == 0)
            {
                return null;
            }

            return m_SelectionInfo.Stages[0];
        }

        private bool Hit(Vector2 mouseWorldPos, StageView stageView, StageView prevStageView)
        {
            var start = stageView.GetTopCenter();
            var end = prevStageView.GetBottomCenter();
            Helper.PointToLineSegmentDistance(mouseWorldPos, start, end, out var distance, out bool interior, out _);
            if (interior && distance <= m_LineWidth * 2)
            {
                return true;
            }
            return false;
        }

        private const int m_MinFontSize = 5;
        private const int m_MaxFontSize = 25;
        private const float m_NameHeight = 20;
        private const float m_XPadding = 40;
        private const float m_YPadding = 40;
        private const float m_MaxCommentAreaWidth = 200;
        private const float m_StatusIconSize = 30;
        private const float m_DescriptionWidth = 250;
        private const float m_DescriptionHeight = 70;
        private Vector2 m_CreateWindowPosition;
        private Color m_IconBackgroundColor = new(0.3f, 0.3f, 0.3f, 1);
        private Color m_IconSelectColor = Color.gray;
        private Color m_TopConnectorColor = Color.gray;
        private Color m_BottomConnectorColor = Color.gray;
        private Color m_TopConnectorSelectColor = Color.cyan;
        private Color m_BottomConnectorSelectColor = Color.cyan;
        private Texture2D m_ButtonBackground;
        private GUIStyle m_TextFieldStyle;
        private GUIStyle m_LabelFieldStyle;
        private GUIStyle m_CommentFieldStyle;
        private GUIStyle m_TextAreaStyle;
        private float m_SplitterPosition = 0.65f;
        private bool m_IsDragging;
        private Rect m_ViewportArea;
        private DateTime lastClickTime;
        private Vector2 lastClickPosition;
        private bool m_ValidClick = false;
        private bool m_DrawDragLine = false;
        private float m_LineWidth = 6;
        private string[] m_PipelineNames;
        private List<ModelBuildPipeline> m_Pipelines;
        //用于调试Model文件夹
        private string m_DebugModelFolder;
        private const float DoubleClickTime = 0.3f; // 双击时间阈值（秒）
        private const float PositionTolerance = 5f; // 位置容差（像素）
    }
}