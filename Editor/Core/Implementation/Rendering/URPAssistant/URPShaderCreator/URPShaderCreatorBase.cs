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
using XDay.UtilityAPI;

namespace ShaderTool
{
    public enum ShaderParameterType
    {
        Texture2D,
        Vector,
        Color,
        Float,
        Range,
    }

    public class PropertyDefine
    {
        public string Name;
        public string DisplayName;
        public ShaderParameterType Type;
        public string DefaultValue;
        public bool IsMainTexture;
    }

    public class URPShaderTemplateParameter
    {
        public string Name = "Test";
        public List<PropertyDefine> Properties = new();
    }

    internal abstract class URPShaderCreatorBase
    {
        public void Create(URPShaderTemplateParameter parameter, string outputDir)
        {
            Helper.CreateDirectory(outputDir);
            CreateShaderFile(parameter, outputDir);
            CreateInputFile(parameter, outputDir);
            CreateForwardPassFile(parameter, outputDir);
            CreateGBufferPassFile(parameter, outputDir);
            CreateMetaPassFile(parameter, outputDir);
            CreateDepthNormalsPassFile(parameter, outputDir);
            AssetDatabase.Refresh();
        }

        private void CreateShaderFile(URPShaderTemplateParameter parameter, string outputDir)
        {
            var templateFileData = File.ReadAllText(GetTemplateFileFullPath($"{GetPrefix()}.shader.txt"));
            templateFileData = templateFileData.Replace("$NAME$", parameter.Name);
            templateFileData = templateFileData.Replace("$CUSTOM_PROPERTY$", GenerateCustomShaderDefine(parameter));
            File.WriteAllText($"{outputDir}/{parameter.Name}.shader", templateFileData);
        }

        private void CreateInputFile(URPShaderTemplateParameter parameter, string outputDir)
        {
            var templateFileData = File.ReadAllText(GetTemplateFileFullPath($"{GetPrefix()}Input.hlsl.txt"));
            templateFileData = templateFileData.Replace("$NAME$", parameter.Name);
            templateFileData = templateFileData.Replace("$TEXTURE_DECLARE$", GenerateTextureDeclare(parameter));
            templateFileData = templateFileData.Replace("$BUFFER_DATA_DECLARE$", GenerateBufferDataDeclare(parameter));
            templateFileData = templateFileData.Replace("$MAIN_TEXTURE$", GetMainTexturePropertyName(parameter));
            templateFileData = templateFileData.Replace("$DOTS_PROP_DECLARE$", GetDOTSPropDeclare(parameter));
            templateFileData = templateFileData.Replace("$DOTS_STATIC_PROP_DECLARE$", GetDOTSStaticPropDeclare(parameter));
            templateFileData = templateFileData.Replace("$DOTS_STATIC_PROP_ASSIGN$", GetDOTSStaticPropAssign(parameter));
            templateFileData = templateFileData.Replace("$DOTS_PROP_DEFINE$", GetDOTSPropDefine(parameter));
            File.WriteAllText($"{outputDir}/{parameter.Name}_{GetPrefix()}Input.hlsl", templateFileData);
        }

        private void CreateForwardPassFile(URPShaderTemplateParameter parameter, string outputDir)
        {
            var templateFileData = File.ReadAllText(GetTemplateFileFullPath($"{GetPrefix()}ForwardPass.hlsl.txt"));
            templateFileData = templateFileData.Replace("$NAME$", parameter.Name);
            templateFileData = templateFileData.Replace("$MAIN_TEXTURE$", GetMainTexturePropertyName(parameter));
            templateFileData = templateFileData.Replace("$COLOR_CALCULATE$", GetColorCalculateText(parameter));
            templateFileData = templateFileData.Replace("$CALCULATE_UV0$", CalculateUV0(parameter));
            templateFileData = templateFileData.Replace("$MAIN_TEXTURE$", GetMainTexturePropertyName(parameter));
            File.WriteAllText($"{outputDir}/{parameter.Name}_{GetPrefix()}ForwardPass.hlsl", templateFileData);
        }

        private void CreateGBufferPassFile(URPShaderTemplateParameter parameter, string outputDir)
        {
            var templateFileData = File.ReadAllText(GetTemplateFileFullPath($"{GetPrefix()}GBufferPass.hlsl.txt"));
            templateFileData = templateFileData.Replace("$NAME$", parameter.Name);
            templateFileData = templateFileData.Replace("$CALCULATE_UV0$", CalculateUV0(parameter));
            templateFileData = templateFileData.Replace("$MAIN_TEXTURE$", GetMainTexturePropertyName(parameter));
            File.WriteAllText($"{outputDir}/{parameter.Name}_{GetPrefix()}GBufferPass.hlsl", templateFileData);
        }

        private void CreateMetaPassFile(URPShaderTemplateParameter parameter, string outputDir)
        {
            var templateFileData = File.ReadAllText(GetTemplateFileFullPath($"{GetPrefix()}MetaPass.hlsl.txt"));
            templateFileData = templateFileData.Replace("$NAME$", parameter.Name);
            templateFileData = templateFileData.Replace("$CALCULATE_UV0$", CalculateUV0(parameter));
            templateFileData = templateFileData.Replace("$MAIN_TEXTURE$", GetMainTexturePropertyName(parameter));
            File.WriteAllText($"{outputDir}/{parameter.Name}_{GetPrefix()}MetaPass.hlsl", templateFileData);
        }

        private void CreateDepthNormalsPassFile(URPShaderTemplateParameter parameter, string outputDir)
        {
            var templateFileData = File.ReadAllText(GetTemplateFileFullPath($"{GetPrefix()}DepthNormalsPass.hlsl.txt"));
            templateFileData = templateFileData.Replace("$NAME$", parameter.Name);
            templateFileData = templateFileData.Replace("$CALCULATE_UV0$", CalculateUV0(parameter));
            templateFileData = templateFileData.Replace("$MAIN_TEXTURE$", GetMainTexturePropertyName(parameter));
            File.WriteAllText($"{outputDir}/{parameter.Name}_{GetPrefix()}DepthNormalsPass.hlsl", templateFileData);
        }

        private string GenerateCustomShaderDefine(URPShaderTemplateParameter parameter)
        {
            var builder = new StringBuilder();
            builder.AppendLine(m_Tips);
            foreach (var prop in parameter.Properties)
            {
                if (prop.Type == ShaderParameterType.Texture2D)
                {
                    if (prop.IsMainTexture)
                    {
                        builder.Append("[MainTexture]");
                    }

                    builder.AppendLine($"{prop.Name}(\"{prop.DisplayName}\", 2D) = \"white\"{{}}");
                }
                else if (prop.Type == ShaderParameterType.Color)
                {
                    builder.AppendLine($"{prop.Name}(\"{prop.DisplayName}\", Color) = (1,1,1,1)");
                }
                else if (prop.Type == ShaderParameterType.Vector)
                {
                    builder.AppendLine($"{prop.Name}(\"{prop.DisplayName}\", Vector) = (1,1,1,1)");
                }
                else if (prop.Type == ShaderParameterType.Range)
                {
                    builder.AppendLine($"{prop.Name}(\"{prop.DisplayName}\", Range(0,1)) = 0.5");
                }
                else if (prop.Type == ShaderParameterType.Float)
                {
                    builder.AppendLine($"{prop.Name}(\"{prop.DisplayName}\", Float) = 0.5");
                }
                else
                {
                    Debug.Assert(false, $"Unknown property: {prop.Name}");
                }
            }

            return builder.ToString();
        }

        private string GenerateTextureDeclare(URPShaderTemplateParameter parameter)
        {
            var builder = new StringBuilder();
            builder.AppendLine(m_Tips);
            foreach (var property in parameter.Properties)
            {
                if (property.Type == ShaderParameterType.Texture2D)
                {
                    if (property.Name == "_BaseMap")
                    {
                        //_BaseMap已经被其他文件定义了,不用再定义
                        continue;
                    }
                    builder.AppendLine($"TEXTURE2D({property.Name}); SAMPLER(sampler{property.Name});");
                }
            }
            return builder.ToString();
        }

        private string GenerateBufferDataDeclare(URPShaderTemplateParameter parameter)
        {
            var builder = new StringBuilder();
            builder.AppendLine(m_Tips);
            foreach (var property in parameter.Properties)
            {
                if (property.Type == ShaderParameterType.Texture2D)
                {
                    builder.AppendLine($"float4 {property.Name}_ST;");
                    builder.AppendLine($"float4 {property.Name}_TexelSize;");
                }
                else if (property.Type == ShaderParameterType.Vector ||
                    property.Type == ShaderParameterType.Color)
                {
                    builder.AppendLine($"float4 {property.Name};");
                }
                else if (property.Type == ShaderParameterType.Float ||
                    property.Type == ShaderParameterType.Range)
                {
                    builder.AppendLine($"float {property.Name};");
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            return builder.ToString();
        }

        private string GetDOTSPropDeclare(URPShaderTemplateParameter parameter)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(m_Tips);
            foreach (var prop in parameter.Properties)
            {
                if (prop.Type != ShaderParameterType.Texture2D)
                {
                    builder.AppendLine($"UNITY_DOTS_INSTANCED_PROP({m_TypeNames[(int)prop.Type]}, {prop.Name})");
                }
            }

            return builder.ToString();
        }

        private string GetDOTSStaticPropDeclare(URPShaderTemplateParameter parameter)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(m_Tips);
            foreach (var prop in parameter.Properties)
            {
                if (prop.Type != ShaderParameterType.Texture2D)
                {
                    builder.AppendLine($"static {m_TypeNames[(int)prop.Type]} unity_DOTS_Sampled{prop.Name};");
                }
            }

            return builder.ToString();
        }

        private string GetDOTSPropDefine(URPShaderTemplateParameter parameter)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(m_Tips);
            foreach (var prop in parameter.Properties)
            {
                if (prop.Type != ShaderParameterType.Texture2D)
                {
                    builder.AppendLine($"#define {prop.Name} unity_DOTS_Sampled{m_TypeNames[(int)prop.Type]}");
                }
            }

            return builder.ToString();
        }

        private string GetDOTSStaticPropAssign(URPShaderTemplateParameter parameter)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(m_Tips);
            foreach (var prop in parameter.Properties)
            {
                if (prop.Type != ShaderParameterType.Texture2D)
                {
                    builder.AppendLine($"unity_DOTS_Sampled{prop.Name} = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT({m_TypeNames[(int)prop.Type]}, {prop.Name});");
                }
            }

            return builder.ToString();
        }

        private string GetMainTexturePropertyName(URPShaderTemplateParameter parameter)
        {
            List<string> texturePropertyNames = new();
            foreach (var prop in parameter.Properties)
            {
                if (prop.Type == ShaderParameterType.Texture2D)
                {
                    texturePropertyNames.Add(prop.Name);
                    if (prop.IsMainTexture)
                    {
                        return prop.Name;
                    }
                }
            }

            if (texturePropertyNames.Count == 0)
            {
                Debug.LogError("No texture");
                return "";
            }

            return texturePropertyNames[0];
        }

        private string GetColorCalculateText(URPShaderTemplateParameter parameter)
        {
            string texturePropertyName = GetMainTexturePropertyName(parameter);
            return $"half4 col = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS({texturePropertyName}, sampler{texturePropertyName}));";
        }

        private string GetTemplateFileFullPath(string fileName)
        {
            return $"{GetTemplateFilePath()}/{fileName}";
        }

        private string CalculateUV0(URPShaderTemplateParameter parameter)
        {
            var mainTextureName = GetMainTexturePropertyName(parameter);
            var builder = new StringBuilder();
            builder.AppendLine(m_Tips);
            builder.AppendLine($"output.uv = TRANSFORM_TEX(input.uv, {mainTextureName});");
            return builder.ToString();
        }

        protected static void DrawProperties(URPShaderTemplateParameter parameter)
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowProperties = EditorGUILayout.Foldout(m_ShowProperties, "Parameters");
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("+", "Add Property"), GUILayout.MaxWidth(20)))
            {
                var contextMenu = new GenericMenu();
                contextMenu.AddItem(new GUIContent("Texture2D"), false, () => { OnClickAddParameter(ShaderParameterType.Texture2D, parameter); });
                contextMenu.AddItem(new GUIContent("Vector"), false, () => { OnClickAddParameter(ShaderParameterType.Vector, parameter); });
                contextMenu.AddItem(new GUIContent("Float"), false, () => { OnClickAddParameter(ShaderParameterType.Float, parameter); });
                contextMenu.AddItem(new GUIContent("Range"), false, () => { OnClickAddParameter(ShaderParameterType.Range, parameter); });
                contextMenu.AddItem(new GUIContent("Color"), false, () => { OnClickAddParameter(ShaderParameterType.Color, parameter); });
                contextMenu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            if (m_ShowProperties)
            {
                EditorGUI.indentLevel++;
                foreach (var param in parameter.Properties)
                {
                    bool removed = false;
                    EditorGUILayout.BeginVertical("GroupBox");
                    EditorGUILayout.BeginHorizontal();
                    param.Name = EditorGUILayout.TextField("Name", param.Name);
                    EditorGUILayout.Space();
                    if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
                    {
                        if (EditorUtility.DisplayDialog("Warning", "Continue?", "Yes", "No"))
                        {
                            removed = true;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    param.DisplayName = EditorGUILayout.TextField("Display Name", param.DisplayName);
                    param.Type = (ShaderParameterType)EditorGUILayout.EnumPopup("Type", param.Type);
                    if (param.Type == ShaderParameterType.Texture2D)
                    {
                        param.IsMainTexture = EditorGUILayout.Toggle("Main Texture", param.IsMainTexture);
                    }
                    param.DefaultValue = EditorGUILayout.TextField("Value", param.DefaultValue);
                    EditorGUILayout.EndVertical();

                    if (removed)
                    {
                        parameter.Properties.Remove(param);
                        break;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private static void OnClickAddParameter(ShaderParameterType type, URPShaderTemplateParameter parameter)
        {
            var prop = new PropertyDefine
            {
                Name = $"_{type}",
                DefaultValue = "",
                IsMainTexture = false,
                DisplayName = type.ToString(),
                Type = type
            };
            parameter.Properties.Add(prop);
        }

        protected abstract string GetTemplateFilePath();
        protected abstract string GetPrefix();

        private string m_Tips = "//----------------------urp auto generated----------------------";
        private readonly string[] m_TypeNames = new string[]
            {
                "",
                "float4",
                "float4",
                "float",
                "float",
            };
        private static bool m_ShowProperties = true;
    }
}
