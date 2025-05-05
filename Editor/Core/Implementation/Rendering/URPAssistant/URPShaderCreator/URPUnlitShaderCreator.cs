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
using UnityEditor;
using XDay.UtilityAPI.Editor;

namespace ShaderTool
{
    internal class URPUnlitShaderCreator : URPShaderCreatorBase
    {
        [MenuItem("XDay/Rendering/URP/Create Unlit Shader")]
        private static void CreateUnlitShader()
        {
            var parameters = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.PathParameter("Output Folder", "", "Assets"),
                new ParameterWindow.StringParameter("Name", "", "URPUnlit"),
            };
            URPUnlitShaderCreator creator = new();
            var parameter = new URPShaderTemplateParameter();
            var prop = new PropertyDefine();
            prop.Name = "_Splat";
            prop.DefaultValue = "";
            prop.IsMainTexture = true;
            prop.DisplayName = "Splat Map";
            prop.Type = ShaderParameterType.Texture2D;
            parameter.Properties.Add(prop);

            ParameterWindow.Open("Create URP Unlit Shader", parameters, (p) =>
            {
                var ok = ParameterWindow.GetPath(p[0], out var outDir);
                ok &= ParameterWindow.GetString(p[1], out var name);
                if (ok)
                {
                    parameter.Name = name;
                    creator.Create(parameter, $"{outDir}/{name}");
                }
                return ok;
            }, true, () => { DrawProperties(parameter); });
        }

        protected override string GetPrefix()
        {
            return "Unlit";
        }

        protected override string GetTemplateFilePath()
        {
            return "Assets/XDay/Editor/Core/Implementation/Rendering/URPAssistant/URPShaderCreator/URPUnlitTemplate";
        }
    }
}
