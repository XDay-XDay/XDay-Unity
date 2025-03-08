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



using System.IO;
using System.Text;
using XDay.SerializationAPI.Editor;

namespace XDay.UtilityAPI.Editor
{
    internal partial class DisplayKeyEditor
    {
        private void GenerateIDs(string fileName)
        {
            var code =
@"
namespace XDay.DisplayKeyID 
{
    $CLASSES$
}
";
            StringBuilder builder = new StringBuilder();
            foreach (var group in m_DisplayKeyManager.Groups)
            {
                builder.AppendLine($"public static class {group.Name} {{");

                for (var i = 0; i < group.Keys.Count; ++i)
                {
                    var key = group.Keys[i];
                    if (!string.IsNullOrEmpty(key.Name))
                    {
                        builder.AppendLine($"public const int {key.Name.Replace(" ", "_")} = {key.ID};");
                    }
                }

                builder.AppendLine("}");
            }

            builder.AppendLine("");

            code = code.Replace("$CLASSES$", builder.ToString());

            File.WriteAllText(fileName, code);

            SerializationHelper.FormatCode(fileName);
        }
    }
}
