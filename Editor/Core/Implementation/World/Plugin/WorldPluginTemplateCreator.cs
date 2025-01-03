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
        [MenuItem("XDay/World/Plugin/Create Editor Plugin Template")]
        static void Open()
        {
            var parameters = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("Class Name", "", "TemplateEditorPlugin"),
                new ParameterWindow.StringParameter("Display Name", "", "Template Editor Plugin"),
                new ParameterWindow.StringParameter("File Name", "", "template_editor_plugin"),
                new ParameterWindow.PathParameter("Output Folder", "", "Assets"),
                new ParameterWindow.BoolParameter("Grid Based", "", true),
                new ParameterWindow.BoolParameter("Is Singleton", "", true),
            };
            ParameterWindow.Open("Create Editor World Plugin", parameters, (p) =>
            {
                var ok = ParameterWindow.GetString(p[0], out var className);
                ok = ParameterWindow.GetString(p[1], out var displayName);
                ok &= ParameterWindow.GetString(p[2], out var fileName);
                ok &= ParameterWindow.GetPath(p[3], out var outputPath);
                ok &= ParameterWindow.GetBool(p[4], out var gridBased);
                ok &= ParameterWindow.GetBool(p[5], out var isSingleton);
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

                    Create(fullPath, gridBased, className, displayName, fileName, isSingleton);
                }
                return ok;
            });
        }

        public static void Create(
            string outputPath,
            bool gridBased,
            string className,
            string displayName,
            string fileName,
            bool isSingleton)
        {
            string template =
@"
#if UNITY_EDITOR

using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using XDay.SerializationAPI;
using XDay.UtilityAPI;
using XDay.WorldAPI;
using XDay.WorldAPI.Editor;

[WorldPluginMetadata(""@DISPLAY_NAME@"", ""@FILE_NAME@_editor_data"", typeof(@CLASS_NAME@CreateWindow), @IS_SINGLETON@)]
internal class @CLASS_NAME@ : EditorWorldPlugin
{
    public override GameObject Root => m_Root;
    public override string Name { get => Root.name; set => Root.name = value; }
    public override List<string> GameFileNames => new List<string>() { ""@FILE_NAME@_game_data""};
    public override WorldPluginUsage Usage => WorldPluginUsage.BothInEditorAndGame;
    public override string TypeName => ""@CLASS_NAME@"";

    public @CLASS_NAME@()
    {
    }

    public @CLASS_NAME@(int id, int index)
        : base(id, index)
    {
    }

    protected override void InitInternal()
    {
        m_Root = new GameObject(""@DISPLAY_NAME@"");
        m_Root.transform.SetParent(World.Root.transform, false);
    }

    protected override async UniTask InitAsyncInternal(CancellationToken token)
    {
        InitInternal();

        await UniTask.FromResult(true);
    }

    protected override void UninitInternal()
    {
        Object.DestroyImmediate(m_Root);
    }

    public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
    {
        serializer.WriteInt32(m_Version, $""@CLASS_NAME@.Version"");

        base.EditorSerialize(serializer, label, converter);

        //save custom data here
    }

    public override void EditorDeserialize(IDeserializer deserializer, string label)
    {
        deserializer.ReadInt32(""@CLASS_NAME@.Version"");

        base.EditorDeserialize(deserializer, label);

        //load custom data here
    }

    public override void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
    {
        serializer.WriteInt32(m_RuntimeVersion, ""@CLASS_NAME@.Version"");

        base.GameSerialize(serializer, label, converter);

        //export custom data here
    }

    public override IAspect GetAspect(int objectID, string name)
    {
        var aspect = base.GetAspect(objectID, name);
        if (aspect != null)
        {
            return aspect;
        }

        //get custom aspect here

        return null;
    }

    public override bool SetAspect(int objectID, string name, IAspect aspect)
    {
        if (base.SetAspect(objectID, name, aspect))
        {
            return true;
        }

        //set custom aspect here

        return false;
    }

    protected override void SceneGUIInternal() { }
    protected override void SceneGUISelectedInternal() { }
    protected override void SceneViewControlInternal(Rect sceneViewRect) { }
    protected override void InspectorGUIInternal() { }
    protected override void SelectionChangeInternal(bool selected) { }
    protected override void SetActiveUndoInternal() { }
    protected override void ValidateExportInternal(StringBuilder errorMessage) { }
    protected override void GenerateGameDataInternal(IObjectIDConverter converter) 
    {
    }

    private GameObject m_Root;
    private const int m_Version = 1;
    private const int m_RuntimeVersion = 1;
}


#endif
";

            string genericCreateWindowText =
@"
    class @CLASS_NAME@CreateWindow : GenericWorldPluginCreateWindow
    {
        protected override string DisplayName => ""@DISPLAY_NAME@"";

        protected override void CreateInternal()
        {
            var plugin = new @CLASS_NAME@(World.AllocateObjectID(), World.PluginCount);
            UndoSystem.CreateObject(plugin, World.ID, 0, ""Create @DISPLAY_NAME@"", 0);
        }

        protected override string ValidateInternal()
        {
            return """";
        }
    }
";

            string gridBasedCreateWindowText =
@"
    class @CLASS_NAME@CreateWindow : GridBasedWorldPluginCreateWindow
    {
        protected override bool SetGridCount => true;
        protected override string DisplayName => ""@DISPLAY_NAME@"";

        protected override void CreateInternal()
        {
            var plugin = new @CLASS_NAME@(World.AllocateObjectID(), World.PluginCount);
            UndoSystem.CreateObject(plugin, World.ID, 0, ""Create @DISPLAY_NAME@"", 0);
        }

        protected override string ValidateInternal()
        {
            return base.ValidateInternal();
        }
    }
";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(template);
            if (gridBased)
            {
                builder.AppendLine(gridBasedCreateWindowText);
            }
            else
            {
                builder.AppendLine(genericCreateWindowText);
            }

            builder.
                Replace("@CLASS_NAME@", className).
                Replace("@DISPLAY_NAME@", displayName).
                Replace("@FILE_NAME@", fileName).
                Replace("@IS_SINGLETON@", isSingleton ? "true" : "false");

            File.WriteAllText(outputPath, builder.ToString());

            AssetDatabase.Refresh();
        }
    }
}


//XDay