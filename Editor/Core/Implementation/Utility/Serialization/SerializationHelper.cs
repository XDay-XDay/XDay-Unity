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

using XDay.UtilityAPI;
using System.IO;

namespace XDay.SerializationAPI.Editor
{
    internal static class SerializationHelper
    {
        public static string GetClassFilePath(string classFullName)
        {
            var workingDir = Directory.GetCurrentDirectory();
            string exeName = "Tools\\ProjectAnalyser\\bin\\Debug\\net8.0\\ProjectAnalyser.exe";
            string arguments = $"{"GetClassFilePath"} {"Assets"} {classFullName}";
            EditorHelper.RunProcess(exeName, arguments, "", out var output, out var error);
            output = output.Trim();
            return output;
        }

        public static void FormatCode(string fileName)
        {
            var workingDir = Directory.GetCurrentDirectory();
            string exeName = "Tools\\ProjectAnalyser\\bin\\Debug\\net8.0\\ProjectAnalyser.exe";
            string arguments = $"{"FormatCode"} {fileName}";
            EditorHelper.RunProcess(exeName, arguments, "", out var output, out var error);
        }
    }
}
