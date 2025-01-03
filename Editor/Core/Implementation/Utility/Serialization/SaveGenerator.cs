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
    internal class SaveGenerator : GeneratorBase
    {
        public void Generate()
        {
            List<SerializableClassInfo> classesInfo = GetSerializableClassInfo();

            foreach (var classInfo in classesInfo)
            {
                var functions = GenerateClassFunctions(classInfo);

                string fileContent = GenerateFull(classInfo, functions);

                var fileName = GetFileName(classInfo, true);

                File.WriteAllText(fileName, fileContent);

                SerializationHelper.FormatCode(fileName);
            }
        }

        private string GenerateClassFunctions(SerializableClassInfo classInfo)
        {
            string text =
@"public @OVERRIDE@ void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
{
    serializer.WriteInt32(m_Version, @CLASS_VERSION_NAME@);
    
    @BASE_SAVE@

    @WRITE_FIELDS@
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
                text = text.Replace("@BASE_SAVE@", "");
            }
            else
            {
                text = text.Replace("@BASE_SAVE@", "base.EditorSerialize(serializer, label, converter);");
            }

            var fields = GetFieldsText(classInfo.Fields);

            text = text.Replace("@WRITE_FIELDS@", fields);

            return text;
        }

        private string GetFieldsText(List<SerializableClassFieldInfo> fields)
        {
            StringBuilder builder = new();
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
serializer.Write{field.Type.Name}({field.Name}, ""{field.Label}"");
";
        }

        private string GeneratePODList(SerializableClassFieldInfo field)
        {
            return $@"
serializer.Write{field.Type.Name}List({field.Name}, ""{field.Label}"");
";
        }

        private string GeneratePODArray(SerializableClassFieldInfo field)
        {
            return $@"
serializer.Write{field.Type.Name}Array({field.Name}, ""{field.Label}"");
";
        }

        private string GenerateCompound(SerializableClassFieldInfo field)
        {
            return $@"
serializer.WriteStructure(""{field.Label}"", ()=>{{
{field.Name}.EditorSerialize(serializer, """", converter);
}});
";
        }

        private string GenerateCompoundList(SerializableClassFieldInfo field)
        {
            string text = $@"
serializer.WriteList({field.Name}, ""{field.Label}"", (obj, index)=>
{{
    serializer.WriteStructure(^FIELD_NAME^, ()=>{{
        obj.EditorSerialize(serializer, """", converter);
    }});
}});
";
            text = text.Replace("^FIELD_NAME^", $"$\"{field.Name} {{index}}\"");
            return text;
        }

        private string GenerateCompoundArray(SerializableClassFieldInfo field)
        {
            string text = $@"
serializer.WriteArray({field.Name}, ""{field.Label}"", (obj, index)=>
{{
    serializer.WriteStructure(^FIELD_NAME^, ()=>{{
        obj.EditorSerialize(serializer, """", converter);
    }});
}});
";
            text = text.Replace("^FIELD_NAME^", $"$\"{field.Name} {{index}}\"");
            return text;
        }
    }
}
