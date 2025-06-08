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

namespace XDay.ModelBuildPipeline.Editor
{
    internal partial class ModelBuildPipelineView
    {
        private void DrawStatus(StageView stageView)
        {
            var pos = stageView.WorldPosition;
            var size = stageView.Size;
            float iconMaxX = pos.x + size.x;
            float iconMinX = iconMaxX - m_StatusIconSize;
            float iconMinY = pos.y;
            float iconMaxY = iconMinY + m_StatusIconSize;

            var min = World2Window(new Vector2(iconMinX, iconMinY));
            var max = World2Window(new Vector2(iconMaxX, iconMaxY));

            var stage = stageView.Stage;

            if (stage.Status == StageStatus.Success)
            {
                DrawTexture(min, max, m_SuccessTexture);
            }
            else if (stage.Status == StageStatus.Fail)
            {
                DrawTexture(min, max, m_FailTexture);
            }
            else if (stage.Status == StageStatus.Running)
            {
                DrawTexture(min, max, m_RunningTexture);
            }
        }
    }
}
