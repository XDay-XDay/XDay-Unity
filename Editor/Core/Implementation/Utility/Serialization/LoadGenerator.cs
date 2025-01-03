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

namespace XDay.SerializationAPI.Editor
{
    internal class LoadGenerator : GeneratorBase
    {
        public void Generate()
        {
            List<SerializableClassInfo> classesInfo = GetSerializableClassInfo();

            foreach (var classInfo in classesInfo)
            {
                var functions = GenerateClassFunctions(classInfo);

                string fileContent = GenerateFull(classInfo, functions);

                var fileName = GetFileName(classInfo, false);

                File.WriteAllText(fileName, fileContent);

                SerializationHelper.FormatCode(fileName);
            }
        }

        private string GenerateClassFunctions(SerializableClassInfo classInfo)
        {
            string text =
@"public @OVERRIDE@ void EditorDeserialize(IDeserializer deserializer, string label)
{
    int classVersion = deserializer.ReadInt32(@CLASS_VERSION_NAME@);
    
    @BASE_LOAD@

    @READ_FIELDS@

    @POST_LOAD@
}";
            if (!classInfo.HasParentClass)
            {
                text = text.Replace("@OVERRIDE@", "");
            }
            else
            {
                text = text.Replace("@OVERRIDE@", "override");
            }

            text = text.Replace("@CLASS_VERSION_NAME@", $"\"{classInfo.ClassLabel}.Version\"");

            if (!classInfo.HasParentClass)
            {
                text = text.Replace("@BASE_LOAD@", "");
            }
            else
            {
                text = text.Replace("@BASE_LOAD@", "base.EditorDeserialize(deserializer, label);");
            }

            var fields = GetFieldsText(classInfo.Fields);

            text = text.Replace("@READ_FIELDS@", fields);

            text = text.Replace("@POST_LOAD@", GetPostLoadText(classInfo));

            return text;
        }

        private string GetPostLoadText(SerializableClassInfo classInfo)
        {
            if (classInfo.HasPostLoad)
            {
                return "PostLoad();";
            }
            return "";
        }

        private string GetFieldsText(List<SerializableClassFieldInfo> fields)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var field in fields)
            {
                if (field.FieldType == FieldType.POD)
                {
                    if (field.ContainerType == ContainerType.List)
                    {
                        builder.AppendLine(GeneratePODList(field));
                    }
                    else if (field.ContainerType == ContainerType.Array)
                    {
                        builder.AppendLine(GeneratePODArray(field));
                    }
                    else
                    {
                        builder.AppendLine(GeneratePOD(field));
                    }
                }
                else
                {
                    if (field.ContainerType == ContainerType.List)
                    {
                        builder.AppendLine(GenerateCompoundList(field));
                    }
                    else if (field.ContainerType == ContainerType.Array)
                    {
                        builder.AppendLine(GenerateCompoundArray(field));
                    }
                    else
                    {
                        builder.AppendLine(GenerateCompound(field));
                    }
                }
            }

            return builder.ToString();
        }

        private string GeneratePOD(SerializableClassFieldInfo field)
        {
            return $@"
if (classVersion >= {field.ExistedMinVersion})
{{
    {field.Name} = deserializer.Read{field.Type.Name}(""{field.Label}"");
}}
else 
{{
    {field.Name} = default;
}}
";
        }

        private string GeneratePODList(SerializableClassFieldInfo field)
        {
            return $@"
if (classVersion >= {field.ExistedMinVersion})
{{
    {field.Name} = deserializer.Read{field.Type.Name}List(""{field.Label}"");
}}
else
{{
    {field.Name} = new();
}}
";
        }

        private string GeneratePODArray(SerializableClassFieldInfo field)
        {
            return $@"
if (classVersion >= {field.ExistedMinVersion})
{{
    {field.Name} = deserializer.Read{field.Type.Name}Array(""{field.Label}"");
}}
else
{{
    {field.Name} = null;
}}
";
        }

        private string GenerateCompound(SerializableClassFieldInfo field)
        {
            return $@"
{field.Name} = new();
if (classVersion >= {field.ExistedMinVersion})
{{
    deserializer.ReadStructure(""{field.Label}"", ()=>{{
        {field.Name}.EditorDeserialize(deserializer, """");
    }});
}}
";
        }

        private string GenerateCompoundList(SerializableClassFieldInfo field)
        {
            string text = $@"
if (classVersion >= {field.ExistedMinVersion})
{{
    {field.Name} = deserializer.ReadList(""{field.Label}"", (index)=>
        {{
        var obj = new {field.Type}();
        deserializer.ReadStructure(^FIELD_NAME^, ()=>{{
            obj.EditorDeserialize(deserializer, """");
        }});
        return obj;
    }});
}}
else
{{
    {field.Name} = new();
}}
";
            text = text.Replace("^FIELD_NAME^", $"$\"{field.Name} {{index}}\"");
            return text;
        }

        private string GenerateCompoundArray(SerializableClassFieldInfo field)
        {
            string text = $@"
if (classVersion >= {field.ExistedMinVersion})
{{
    {field.Name} = deserializer.ReadArray(""{field.Label}"", (index)=>
        {{
        var obj = new {field.Type}();
        deserializer.ReadStructure(^FIELD_NAME^, ()=>{{
            obj.EditorDeserialize(deserializer, """");
        }});
        return obj;
    }});
}}
else
{{
    {field.Name} = null;
}}
";
            text = text.Replace("^FIELD_NAME^", $"$\"{field.Name} {{index}}\"");
            return text;
        }
    }
}


