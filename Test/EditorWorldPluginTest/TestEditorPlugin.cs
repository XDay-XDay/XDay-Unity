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

namespace XDay.Test.EditorWorldPluginTest
{
    [WorldPluginMetadata("Test Editor Plugin", "test_editor_plugin_editor_data", typeof(TestEditorPluginCreateWindow), true)]
    internal class TestEditorPlugin : EditorWorldPlugin
    {
        public override GameObject Root => m_Root;

        public override string Name { get => Root.name; set => Root.name = value; }

        public override List<string> GameFileNames => new List<string>() { "test_editor_plugin_runtime_data"};

        public override WorldPluginUsage Usage => WorldPluginUsage.BothInEditorAndGame;

        public override string TypeName => "TestEditorPlugin";

        public TestEditorPlugin()
        {
        }

        public TestEditorPlugin(int id, int index)
            : base(id, index)
        {
        }

        protected override void InitInternal()
        {
            m_Root = new GameObject("Test Editor Plugin");
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
            serializer.WriteInt32(m_Version, $"TestEditorPlugin.Version");

            base.EditorSerialize(serializer, label, converter);

            //save custom data here
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("TestEditorPlugin.Version");

            base.EditorDeserialize(deserializer, label);

            //load custom data here
        }

        public override void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "TestEditorPlugin.Version");

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

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            throw new System.NotImplementedException();
        }

        public override void DestroyObjectUndo(int objectID)
        {
            throw new System.NotImplementedException();
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            throw new System.NotImplementedException();
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
            Debug.LogError("TestEditorPlugin: GenerateGameDataInternal");
        }

        private GameObject m_Root;
        private const int m_Version = 1;
        private const int m_RuntimeVersion = 1;
    }

    class TestEditorPluginCreateWindow : GenericWorldPluginCreateWindow
    {
        protected override string DisplayName => "Test Editor Plugin";

        protected override void CreateInternal()
        {
            var plugin = new TestEditorPlugin(World.AllocateObjectID(), World.PluginCount);
            UndoSystem.CreateObject(plugin, World.ID, "Create Test Editor Plugin");
        }

        protected override string ValidateInternal()
        {
            return "";
        }
    }

    class TestGridEditorPluginCreateWindow : GridBasedWorldPluginCreateWindow
    {
        protected override bool SetGridCount => true;
        protected override string DisplayName => "Test Editor Plugin";

        protected override void CreateInternal()
        {
            var plugin = new TestEditorPlugin(World.AllocateObjectID(), World.PluginCount);
            UndoSystem.CreateObject(plugin, World.ID, "Create Test Editor Plugin");
        }

        protected override string ValidateInternal()
        {
            return base.ValidateInternal();
        }
    }
}

#endif