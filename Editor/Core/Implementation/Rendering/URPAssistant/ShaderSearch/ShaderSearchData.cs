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
using UnityEngine;

namespace ShaderTool
{
    public class Entry
    {
        public string Text;
        public string File;
        public string Detail;
        public int LineNumber;

        public override string ToString()
        {
            return $"{Text} in {File} at line {LineNumber}";
        }
    }

    [System.Serializable]
    public class SearchResult
    {
        public string Text;
        public bool Display = true;
        public List<Entry> Entries = new();
    }

    public class ShaderSearchData : ScriptableObject
    {
        public List<SearchResult> Results = new();
        public bool ShowDetail = false;
        public bool MatchWord = true;

        public void AddResult(SearchResult result)
        {
            for (var i = 0; i < Results.Count; i++)
            {
                if (Results[i].Text == result.Text)
                {
                    Results[i] = result;
                    return;
                }
            }

            Results.Add(result);
        }

        public void RemoveResult(string text)
        {
            for (var i = 0; i < Results.Count; i++)
            {
                if (Results[i].Text == text)
                {
                    Results.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
