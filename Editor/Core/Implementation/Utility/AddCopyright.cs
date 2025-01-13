
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
using UnityEditor;

namespace XDay.UtilityAPI.Editor
{
    internal static class AddCopyright
    {
        [MenuItem("XDay/Other/Add Copyright")]
        static void Add()
        {
            Run("Assets/XDay");
        }

        private static void Run(string dir)
        {
            foreach (var path in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                if (path.EndsWith(".cs"))
                {
                    if (SkipFile(path))
                    {
                        continue;
                    }

                    var text = File.ReadAllText(path);
                    if (text.IndexOf("Copyright (c) 2024-2025 XDay") == -1)
                    {
                        text = m_CopyrightText + text;
                        File.WriteAllText(path, text);
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        private static bool SkipFile(string filePath)
        {
            return filePath.IndexOf("MiniJSON") >= 0 ||
                filePath.IndexOf("Triangle.Net") >= 0 ||
                filePath.IndexOf("CsprojModifier") >= 0 ||
                filePath.IndexOf("ThirdParty") >= 0 ||
                filePath.IndexOf("CounterModeCryptoTransform") >= 0 ||
                filePath.IndexOf("FastPriorityQueue") >= 0;
        }

        private const string m_CopyrightText =
@"/*
 * Copyright (c) 2024-2025 XDay
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * ""Software""), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

";
    }
}
