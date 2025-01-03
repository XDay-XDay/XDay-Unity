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
using UnityEditor;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;

namespace XDay.CameraAPI.Editor
{
    internal class CameraSetupCreator
    {
        [MenuItem("XDay/Camera/Create Setup")]
        static void Open()
        {
            var parameters = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("Name", "", "CameraSetup.bytes"),
                new ParameterWindow.PathParameter("Output Folder", "", "Assets")
            };
            ParameterWindow.Open("Create Camera Setup", parameters, (p) =>
            {
                var ok = ParameterWindow.GetString(p[0], out var name);
                ok &= ParameterWindow.GetPath(p[1], out var path);
                if (ok)
                {
                    Create($"{path}/{name}");
                }
                return ok;
            });
        }

        public static void Create(string path)
        {
            string template =
@"
{
    ""Orbit"":
    {
        ""Pitch"":45,
        ""Yaw"":0,
        ""Range"":0,
        ""Min Altitude"":0,
        ""Max Altitude"":0,
        ""Restore Speed"":200,
        ""Zoom"": false,
        ""Free"": false
    },
    ""Altitude"":
    [
        {
            ""Name"":""min"",
            ""Altitude"":""5"",
            ""FOV"":""60""
        },
        {
            ""Name"":""max"",
            ""Altitude"":""500"",
            ""FOV"":""60""
        }
    ],
    ""Restore"":
    {
        ""Duration"":0.3,
        ""Distance"":2
    }
}
";
            var dir = Helper.GetFolderPath(path);
            Helper.CreateDirectory(dir);
            File.WriteAllText(path, template);
            AssetDatabase.Refresh();
        }
    }
}


//XDay