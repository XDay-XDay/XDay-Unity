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
using XDay.UtilityAPI.Editor;

namespace XDay.CameraAPI.Editor
{
    internal class WorldPluginTemplateCreator
    {
        [MenuItem("XDay/World/Plugin/Create Plugin Template")]
        static void Open()
        {
            var parameters = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("Class Name", "", "TemplatePlugin"),
                new ParameterWindow.StringParameter("File Name", "", "template_plugin"),
                new ParameterWindow.PathParameter("Output Folder", "", "Assets"),
            };
            ParameterWindow.Open("Create Editor World Plugin", parameters, (p) =>
            {
                var ok = ParameterWindow.GetString(p[0], out var className);
                ok &= ParameterWindow.GetString(p[1], out var fileName);
                ok &= ParameterWindow.GetPath(p[2], out var outputPath);
                if (ok)
                {
                    var fullPath = $"{outputPath}/{className}.cs";
                    if (File.Exists(fullPath))
                    {
                        if (!EditorUtility.DisplayDialog("Warning", $"{fullPath} already exists, overridden?", "Yes", "No"))
                        {
                            return false;
                        }
                    }

                    Create(fullPath, className, fileName);
                }
                return ok;
            });
        }

        public static void Create(
            string outputPath,
            string className,
            string fileName)
        {
            string template =
@"

using System.Collections.Generic;
using UnityEngine.Scripting;
using XDay.WorldAPI;


[Preserve]
internal partial class @CLASS_NAME@ : WorldPlugin
{
    public override List<string> GameFileNames => new() { ""@FILE_NAME@_game_data""};
    public override string Name { set => throw new System.NotImplementedException(); get => throw new System.NotImplementedException(); }
    public override string TypeName => ""@CLASS_NAME@"";

    public @CLASS_NAME@()
    {
    }

    protected override void InitInternal()
    {
        //Init plugin here
    }

    protected override void UninitInternal()
    {
        //Uninit plugin here   
    }

    protected override void InitRendererInternal()
    {
        //Init renderer here
    }

    protected override void UninitRendererInternal()
    {
        //Uninit renderer here
    }

    protected override void UpdateInternal()
    {
        //Update here
    }

    protected override void LoadGameDataInternal(string pluginName, IWorld world)
    {
        var deserializer = world.QueryGameDataDeserializer(world.ID, $""decoration@{pluginName}"");
    }
}
";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(template);

            builder.
                Replace("@CLASS_NAME@", className).
                Replace("@FILE_NAME@", fileName);

            File.WriteAllText(outputPath, builder.ToString());

            AssetDatabase.Refresh();
        }
    }
}

//XDay