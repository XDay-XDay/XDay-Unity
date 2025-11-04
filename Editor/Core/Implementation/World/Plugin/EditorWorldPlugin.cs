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
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    public abstract class EditorWorldPlugin : WorldPlugin, IWorldObjectContainer
    {
        public override bool IsActive => Root == null ? false : Root.activeSelf;
        public virtual WorldPluginUsage Usage { get; } = WorldPluginUsage.BothInEditorAndGame;
        public abstract GameObject Root { get; }
        public abstract int FileIDOffset { get; }

        public EditorWorldPlugin()
        {
        }

        public EditorWorldPlugin(int id, int index) 
            : base(id, index)
        {
        }

        protected override void PostInitInternal()
        {
            if (Root.GetComponent<NoKeyDeletion>() == null)
            {
                Root.AddComponent<NoKeyDeletion>();
            }
            Root.SetActive(m_IsActive);
        }

        public void SceneViewControl(Rect sceneViewRect)
        {
            if (Inited)
            {
                SceneViewControlInternal(sceneViewRect);
            }
        }

        public virtual List<UIControl> GetSceneViewControls()
        {
            return null;
        }

        public void SceneGUI()
        {
            if (Inited)
            {
                SceneGUIInternal();
            }
        }

        public void SceneGUISelected()
        {
            if (Inited)
            {
                SceneGUISelectedInternal();
            }
        }

        public void InspectorGUI()
        {
            if (Inited)
            {
                InspectorGUIInternal();
            }
        }

        public void SetActiveUndo(bool active)
        {
            if (IsActive != active)
            {
                UndoSystem.SetAspect(this, WorldDefine.ASPECT_ENABLE, IAspect.FromBoolean(active), "Set Plugin Active", ID, UndoActionJoinMode.NextJoin);

                SetActiveUndoInternal();
            }
        }

        public void SetSelected(bool selected)
        {
            if (Inited)
            {
                SelectionChangeInternal(selected);
            }
        }

        public virtual GenericMenu ContextMenu(out bool showMenu)
        {
            showMenu = true;
            return null;
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "WorldPlugin.Version");

            if (Root != null)
            {
                m_IsActive = IsActive;
            }
            serializer.WriteBoolean(m_IsActive, "Is Active");

            base.EditorSerialize(serializer, label, converter);
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("WorldPlugin.Version");
            m_IsActive = deserializer.ReadBoolean("Is Active", true);

            base.EditorDeserialize(deserializer, label);
        }

        public override void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "WorldPlugin.Version");

            base.GameSerialize(serializer, label, converter);
        }

        public override IAspect GetAspect(int objectID, string name)
        {
            var aspect = base.GetAspect(objectID, name);
            if (aspect != null)
            {
                return aspect;
            }

            if (name == WorldDefine.ASPECT_NAME)
            {
                return IAspect.FromString(Name);
            }

            if (name == WorldDefine.ASPECT_ENABLE)
            {
                return IAspect.FromBoolean(IsActive);
            }

            return null;
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (base.SetAspect(objectID, name, aspect))
            {
                return true;
            }

            if (name == WorldDefine.ASPECT_NAME)
            {
                Name = aspect.GetString();
                return true;
            }

            if (name == WorldDefine.ASPECT_ENABLE)
            {
                Root.SetActive(aspect.GetBoolean());
                return true;
            }

            return false;
        }

        internal virtual ISerializer CreateEditorDataSerializer()
        {
            var world = World as World;

            var metadata = GetMetadata();

            var path = $"{world.Setup.EditorFolder}/{metadata.EditorFileName}_{FileName}.bytes";
            return ISerializer.CreateFile(ISerializer.CreateBinary(), path);
        }

        public void GenerateGameData(IObjectIDConverter converter)
        {
            GenerateGameDataInternal(converter);
        }

        public void ValidateBeforeSerializeGameData(StringBuilder errorMessage)
        {
            ValidateExportInternal(errorMessage);
        }

        public abstract void AddObjectUndo(IWorldObject obj, int lod, int objectIndex);
        public abstract void DestroyObjectUndo(int objectID);
        public abstract IWorldObject QueryObjectUndo(int objectID);

        protected string GetGameFilePath(string name)
        {
            return $"{(World as World).GameFolder}/{name}@{FileName}.bytes";
        }

        private WorldPluginMetadataAttribute GetMetadata()
        {
            foreach (var attribute in GetType().GetCustomAttributes(false))
            {
                if (attribute.GetType() == typeof(WorldPluginMetadataAttribute))
                {
                    var metadata = attribute as WorldPluginMetadataAttribute;
                    return metadata;
                }
            }

            Debug.LogError($"{GetType().Name} no WorldPluginMetadataAttribute found!");
            return null;
        }

        protected virtual void SceneGUIInternal() { }
        protected virtual void SceneGUISelectedInternal() { }
        protected virtual void SceneViewControlInternal(Rect sceneViewRect) { }
        protected virtual void InspectorGUIInternal() { }
        protected virtual void SelectionChangeInternal(bool selected) { }
        protected virtual void SetActiveUndoInternal() { }
        protected virtual void ValidateExportInternal(StringBuilder errorMessage) { }
        protected virtual void GenerateGameDataInternal(IObjectIDConverter converter) { }

        private bool m_IsActive = true;
        private const int m_Version = 1;
        private const int m_RuntimeVersion = 1;
    }
}

//XDay