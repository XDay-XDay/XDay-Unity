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
using UnityEditor;
using UnityEngine;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Shape.Editor
{
    [WorldPluginMetadata("形状层", "shape_editor_data", typeof(ShapeSystemCreateWindow), singleton:true)]
    internal partial class ShapeSystem : EditorWorldPlugin, IObstacleSource
    {
        public override GameObject Root => m_Renderer == null ? null : m_Renderer.Root;
        public override List<string> GameFileNames => new() { "shape" };
        public override IPluginLODSystem LODSystem => null;
        public override WorldPluginUsage Usage => WorldPluginUsage.BothInEditorAndGame;
        public override string Name
        {
            get => m_Name;
            set
            {
                Root.name = value;
                m_Name = value;
            }
        }
        public override Bounds Bounds => m_Bounds;
        public override string TypeName => "EditorShapeSystem";

        public ShapeSystem()
        {
        }

        public ShapeSystem(int id, int objectIndex, Bounds bounds, string name)
            : base(id, objectIndex)
        {
            m_Bounds = bounds;
            m_Name = name;
        }

        protected override void InitInternal()
        {
            Selection.selectionChanged += OnSelectionChanged;

            InitBuilder();

            foreach (var kv in m_Shapes)
            {
                kv.Value.Init(World);
            }

            m_Renderer = new ShapeSystemRenderer(World.Root.transform, this);

            ShowObjects();
        }

        protected override void UninitInternal()
        {
            m_Renderer.OnDestroy();
            foreach (var kv in m_Shapes)
            {
                kv.Value.Uninit();
            }

            Selection.selectionChanged -= OnSelectionChanged;
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            return World.QueryObject<IWorldObject>(objectID);
        }

        public override void DestroyObjectUndo(int objectID)
        {
            var obj = World.QueryObject<ShapeObject>(objectID);
            if (obj != null)
            {
                m_Renderer.Destroy(obj);
            }
            DestroyObject(objectID);
        }

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            AddObjectInternal(obj as ShapeObject);
        }

        public void SetEnabled(int objectID, bool enabled, bool forceSet)
        {
            var obj = World.QueryObject<ShapeObject>(objectID);
            if (obj != null)
            {
                if (forceSet ||
                    obj.SetEnabled(enabled))
                {
                    m_Renderer.ToggleVisibility(obj);
                }
            }
            else
            {
                Debug.LogError($"SetObjectEnabled {objectID} failed!");
            }
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (!base.SetAspect(objectID, name, aspect))
            {
                var obj = World.QueryObject<ShapeObject>(objectID);
                if (obj != null)
                {
                    var ok = obj.SetAspect(objectID, name, aspect);
                    if (ok)
                    {
                        m_Renderer.SetAspect(objectID, name);
                    }
                }
            }
            return true;
        }

        public override IAspect GetAspect(int objectID, string name)
        {
            var aspect = base.GetAspect(objectID, name);
            if (aspect != null)
            {
                return aspect;
            }

            var obj = World.QueryObject<ShapeObject>(objectID);
            if (obj != null)
            {
                return obj.GetAspect(objectID, name);
            }

            return null;
        }

        private void AddObjectInternal(ShapeObject shape)
        {
            m_Shapes.Add(shape.ID, shape);

            if (shape.GetVisibility() == WorldObjectVisibility.Undefined)
            {
                shape.SetVisibility(WorldObjectVisibility.Visible);
            }

            m_Renderer.Create(shape);
        }

        private bool DestroyObject(int objectID)
        {
            foreach (var shape in m_Shapes.Values)
            {
                if (shape.ID == objectID)
                {
                    shape.Uninit();
                    m_Shapes.Remove(shape.ID);
                    if (objectID == m_ActiveShapeID)
                    {
                        SetActiveShape(0);
                    }
                    return true;
                }
            }
            Debug.Assert(false, $"Destroy object {objectID} failed!");
            return false;
        }

        private ShapeObject CloneObject(int id, int objectIndex, ShapeObject obj)
        {
            if (obj == null)
            {
                Debug.Assert(false, $"Clone object failed: {id}");
                return null;
            }
            var bytes = UndoSystem.Serialize(obj);
            var newObj = UndoSystem.Deserialize(id, objectIndex, bytes, World.ID, typeof(ShapeObject).FullName, false) as ShapeObject;
            return newObj;
        }

        List<IObstacle> IObstacleSource.GetObstacles()
        {
            List<IObstacle> obstacles = new();
            foreach (var kv in m_Shapes)
            {
                obstacles.Add(kv.Value);
            }
            return obstacles;
        }

        private string m_Name;
        private ShapeSystemRenderer m_Renderer;
        private Bounds m_Bounds;
        private Dictionary<int, ShapeObject> m_Shapes = new();
        private const int m_Version = 1;
    }
}
