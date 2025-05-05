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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using XDay.UtilityAPI;

namespace ShaderTool
{
    public class ShaderSearch
    {
        public SearchResult Search(List<string> directories, string content, bool matchWord)
        {
            if (content == null || directories == null)
            {
                return null;
            }

            SearchResult result  = new SearchResult();
            result.Text = content;

            List<Task<List<Entry>>> tasks = new();
            foreach (var dir in directories)
            {
                var enumerator = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
                foreach (var file in enumerator)
                {
                    var task = Task.Run(() => {
                        return SearchContent(file, content, matchWord);
                    });
                    tasks.Add(task);
                }
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var task in tasks)
            {
                var entries = task.Result;
                if (entries != null)
                {
                    result.Entries.AddRange(entries);
                }
            }

            return result;
        }

        public void OpenFile(string fullPath)
        {
            if (fullPath.StartsWith("\""))
            {
                fullPath = fullPath.Remove(0, 1);
            }

            if (fullPath.EndsWith("\""))
            {
                fullPath = fullPath.Remove(fullPath.Length - 1, 1);
            }

            var tokens = fullPath.Split("/");
            var packagePath = tokens[0] + "/" + tokens[1];
            var absFilePath = EditorHelper.ConvertPackageToPhysicalPath(packagePath);
            var relativePath = fullPath.Substring(packagePath.Length);
            Open($"{absFilePath}{relativePath}", 1);
        }

        public void Open(string filePath, int lineNumber)
        {
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, lineNumber);
        }

        List<Entry> SearchContent(string file, string content, bool matchWord)
        {
            int lineNo = 0;
            List<Entry> ret = new();
            int lineLength = 3;
            Queue<string> lineHistory = new();

            var iter = File.ReadLines(file).GetEnumerator();
            while (iter.MoveNext())
            {
                var line = iter.Current;
                var trim = line.Trim();
                if (trim.Length == 0)
                {
                    continue;
                }
                if (lineHistory.Count >= lineLength)
                {
                    lineHistory.Dequeue();
                }
                lineHistory.Enqueue(line);

                if (matchWord)
                {
                    string pattern = $@"\b{Regex.Escape(content)}\b";
                    MatchCollection matches = Regex.Matches(line, pattern);

                    foreach (var match in matches)
                    {
                        var entry = new Entry();
                        entry.LineNumber = lineNo;
                        entry.Text = line;
                        entry.File = file;
                        entry.Detail = CreateDetail(lineHistory.ToArray());
                        ret.Add(entry);
                    }
                }
                else
                {
                    int start = 0;
                    while (true)
                    {
                        var pos = line.IndexOf(content, start);
                        if (pos < 0)
                        {
                            break;
                        }

                        var entry = new Entry();
                        entry.LineNumber = lineNo;
                        entry.Text = line;
                        entry.File = file;
                        entry.Detail = CreateDetail(lineHistory.ToArray());
                        ret.Add(entry);

                        start = pos + content.Length;
                    }
                }

                ++lineNo;
            }

            iter.Dispose();

            return ret;
        }

        string CreateDetail(string[] prev)
        {
            StringBuilder builder = new();
            for (var i = 0; i < prev.Length; ++i)
            {
                if (i < prev.Length - 1)
                {
                    builder.AppendLine(prev[i]);
                }
                else
                {
                    builder.Append(prev[i]);
                }
            }

            return builder.ToString();
        }
    }
}
