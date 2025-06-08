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

using System.Text;

namespace XDay.WorldAPI.City.Editor
{
    internal partial class CityEditor
    {
        /// <summary>
        /// 导出前验证数据是否有效
        /// </summary>
        /// <param name="errorMessage"></param>
        protected override void ValidateExportInternal(StringBuilder errorMessage)
        {
            EnsureConfigIDNotZero(errorMessage);

            EnsureConfigIDNoDuplication(errorMessage);
        }

        /// <summary>
        /// 保证ConfigID不为0
        /// </summary>
        /// <param name="errorMessage"></param>
        void EnsureConfigIDNotZero(StringBuilder errorMessage)
        {
            foreach (var grid in m_Grids)
            {
                foreach (var region in grid.RegionTemplates)
                {
                    if (region.ConfigID == 0)
                    {
                        errorMessage.AppendLine($"区域\"{region.Name}\"ID未设置.");
                    }

                    foreach (var area in region.AreaTemplates)
                    {
                        if (area.ConfigID == 0)
                        {
                            errorMessage.AppendLine($"地块\"{area.Name}\"ID未设置.");
                        }
                    }

                    foreach (var land in region.LandTemplates)
                    {
                        if (land.ConfigID == 0)
                        {
                            errorMessage.AppendLine($"绿地\"{land.Name}\"ID未设置.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 保证ConfigID没有重复
        /// </summary>
        /// <param name="errorMessage"></param>
        void EnsureConfigIDNoDuplication(StringBuilder errorMessage)
        {
            foreach (var grid in m_Grids)
            {
                for (var i = 0; i < grid.RegionTemplates.Count; ++i)
                {
                    var region = grid.RegionTemplates[i];
                    for (var j = i + 1; j < grid.RegionTemplates.Count; ++j)
                    {
                        var region1 = grid.RegionTemplates[j];
                        if (region != region1)
                        {
                            if (region.ConfigID == region1.ConfigID)
                            {
                                errorMessage.AppendLine($"区域\"{region.Name}\"和\"{region1.Name}\"ID相同,需要ID唯一.");
                            }
                        }
                    }

                    for (var j = 0; j < region.AreaTemplates.Count; ++j)
                    {
                        var area = region.AreaTemplates[j];
                        for (var k = j + 1; k < region.AreaTemplates.Count; ++k)
                        {
                            var area1 = region.AreaTemplates[k];
                            if (area != area1)
                            {
                                if (area.ConfigID == area1.ConfigID)
                                {
                                    errorMessage.AppendLine($"地块\"{region.Name}-{area.Name}\"和\"{region.Name}-{area1.Name}\"ID相同,需要ID唯一.");
                                }
                            }
                        }
                    }

                    for (var j = 0; j < region.LandTemplates.Count; ++j)
                    {
                        var land = region.LandTemplates[j];
                        for (var k = j + 1; k < region.LandTemplates.Count; ++k)
                        {
                            var land1 = region.LandTemplates[k];
                            if (land != land1)
                            {
                                if (land.ConfigID == land1.ConfigID)
                                {
                                    errorMessage.AppendLine($"绿地\"{region.Name}-{land.Name}\"和\"{region.Name}-{land1.Name}\"ID相同,需要ID唯一.");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
