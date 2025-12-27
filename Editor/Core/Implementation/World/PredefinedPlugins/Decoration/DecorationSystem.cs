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

namespace XDay.WorldAPI.Decoration.Editor
{
    [WorldPluginMetadata("装饰层", "decoration_editor_data", typeof(DecorationSystemCreateWindow), true)]
    public partial class DecorationSystem : EditorWorldPlugin,
        IObstacleSource, 
        IWalkableObjectSource,
        IWorldBakeable
    {
        public override GameObject Root => m_Renderer == null ? null : m_Renderer.Root;
        public override List<string> GameFileNames => new() { "decoration" };
        public override IPluginLODSystem LODSystem => m_PluginLODSystem;
        public int LODCount => m_PluginLODSystem.LODCount;
        public override WorldPluginUsage Usage => WorldPluginUsage.BothInEditorAndGame;
        public int CurrentLOD => m_PluginLODSystem.CurrentLOD;
        public int ActiveLOD => m_ActiveLOD;
        public override string Name
        {
            get=>m_Name;
            set
            {
                Root.name = value;
                m_Name = value;
            }
        }
        public override Bounds Bounds => m_Bounds;
        public override string TypeName => "EditorDecorationSystem";
        public override int FileIDOffset => WorldDefine.DECORATION_SYSTEM_FILE_ID_OFFSET;

        public DecorationSystem()
        {
        }

        public DecorationSystem(int id, int objectIndex, Vector2 runtimeGridSize, Bounds bounds, string name)
            : base(id, objectIndex)
        {
            m_GameGridSize = runtimeGridSize;
            m_PluginLODSystem = IPluginLODSystem.Create(1);
            m_Bounds = bounds;
            m_Name = name;
            m_ResourceDescriptorSystem = IEditorResourceDescriptorSystem.Create();
            m_ResourceGroupSystem = IResourceGroupSystem.Create(false);
        }

        protected override void InitInternal()
        {
            m_CreateMode = (ObjectCreateMode)EditorPrefs.GetInt(DecorationDefine.CREATE_MODE, (int)ObjectCreateMode.Single);
            m_RemoveRange = EditorPrefs.GetFloat(DecorationDefine.REMOVE_RANGE, 5);
            m_CoordinateGenerateSetting.CircleRadius = EditorPrefs.GetFloat(DecorationDefine.CIRCLE_RADIUS, 10);
            m_CoordinateGenerateSetting.RectWidth = EditorPrefs.GetFloat(DecorationDefine.RECT_WIDTH, 5);
            m_CoordinateGenerateSetting.RectHeight = EditorPrefs.GetFloat(DecorationDefine.RECT_HEIGHT, 5);
            m_CoordinateGenerateSetting.Count = EditorPrefs.GetInt(DecorationDefine.OBJECT_COUNT, 10);
            m_CoordinateGenerateSetting.Space = EditorPrefs.GetFloat(DecorationDefine.SPACE, 1);
            m_CoordinateGenerateSetting.Random = EditorPrefs.GetBool(DecorationDefine.RANDOM, false);
            m_CoordinateGenerateSetting.BorderSize = EditorPrefs.GetFloat(DecorationDefine.BORDER_SIZE, 0);
            m_CoordinateGenerateSetting.LineEquidistant = EditorPrefs.GetBool(DecorationDefine.LINE_EQUIDISTANT, false);

            UndoSystem.AddUndoRedoCallback(UndoRedo);
            UndoSystem.AddCreateObjectCallback(OnUndoCreateObject);

            m_ResourceDescriptorSystem.Init(World);

            foreach (var kv in m_Decorations)
            {
                kv.Value.Init(World);
            }

            foreach (var pattern in m_Patterns)
            {
                pattern.Init(World);
            }

            m_PluginLODSystem.Init(World.WorldLODSystem);

            m_Indicator = IMeshIndicator.Create(World);
            m_AreaUpdater = ICameraVisibleAreaUpdater.Create(World);
            m_Renderer = new DecorationSystemRenderer(World.Root.transform, World.GameObjectPool, this);
            m_ResourceGroupSystem.Init(null);

            ShowObjects();

            Selection.selectionChanged += OnSelectionChanged;

            SearchHooks();
        }

        protected override void UninitInternal()
        {
            Selection.selectionChanged -= OnSelectionChanged;

            UndoSystem.RemoveUndoRedoCallback(UndoRedo);
            UndoSystem.RemoveCreateObjectCallback(OnUndoCreateObject);

            m_Indicator.OnDestroy();
            m_ResourceDescriptorSystem.Uninit();
            m_Renderer.OnDestroy();
            foreach (var kv in m_Decorations)
            {
                kv.Value.Uninit();
            }
            foreach (var pattern in m_Patterns)
            {
                pattern.Uninit();
            }
        }

        private void OnUndoCreateObject(IWorldObject obj)
        {
            if (obj is Pattern pattern)
            {
                if (GetPatternIndex(pattern) == m_ActivePatternIndex)
                {
                    pattern.SetActive(true);
                }
            }
        }

        private void OnSelectionChanged()
        {
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            return World.QueryObject<IWorldObject>(objectID);
        }

        public override void DestroyObjectUndo(int objectID)
        {
            bool destroyed = DestroyDecoration(objectID);
            if (!destroyed)
            {
                DestroyPattern(objectID);
            }
        }

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            if (obj is DecorationObject decoration)
            {
                AddDecoration(decoration);
            }
            else if (obj is Pattern pattern)
            {
                m_Patterns.Add(pattern);
            }

            UpdateObjectLOD(obj.ID, m_ActiveLOD);
        }

        public void SetEnabled(int objectID, bool enabled, int lod, bool forceSet)
        {
            var obj = World.QueryObject<DecorationObject>(objectID);
            if (obj != null)
            {
                if (forceSet ||
                    obj.SetEnabled(enabled))
                {
                    m_Renderer.ToggleVisibility(obj, lod);
                }
            }
            else
            {
                Debug.LogError($"SetObjectEnabled {objectID} failed!");
            }
        }

        public void ClearDirtyObjects()
        {
            m_DirtyObjectIDs.Clear();
        }

        public void NotifyObjectDirty(int objectID)
        {
            if (!m_DirtyObjectIDs.Contains(objectID))
            {
                m_DirtyObjectIDs.Add(objectID);
            }
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (!base.SetAspect(objectID, name, aspect))
            {
                if (name == DecorationDefine.LOD_NAME)
                {
                    ChangeActiveLOD(aspect.GetInt32(), false);
                    return true;
                }

                var obj = World.QueryObject<DecorationObject>(objectID);
                if (obj != null)
                {
                    var ok = obj.SetAspect(objectID, name, aspect);
                    if (ok)
                    {
                        m_Renderer.SetAspect(objectID, name);
                    }
                }
                else
                {
                    var pattern = World.QueryObject<Pattern>(objectID);
                    pattern?.SetAspect(objectID, name, aspect);
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

            if (name == DecorationDefine.LOD_NAME)
            {
                return IAspect.FromInt32(m_ActiveLOD);
            }

            var obj = World.QueryObject<DecorationObject>(objectID);
            if (obj != null)
            {
                return obj.GetAspect(objectID, name);
            }

            var pattern = World.QueryObject<Pattern>(objectID);
            if (pattern != null)
            {
                return pattern.GetAspect(objectID, name);
            }

            return null;
        }

        public void SetBounds(Bounds bounds)
        {
            m_Bounds = bounds;
        }
        
        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            SyncObjectTransforms();

            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_Version, "DecorationSystem.Version");

            serializer.WriteString(m_Name, "Name");
            serializer.WriteBounds(m_Bounds, "Bounds");
            serializer.WriteBoolean(m_EnableInstanceRendering, "Enable Instance Rendering");

            var allObjects = new List<DecorationObject>();
            foreach (var p in m_Decorations)
            {
                allObjects.Add(p.Value);
            }

            serializer.WriteList(allObjects, "Objects", (obj, index) =>
            {
                serializer.WriteSerializable(obj, $"Object {index}", converter, false);
            });

            serializer.WriteList(m_Patterns, "Patterns", (pattern, index) =>
            {
                serializer.WriteSerializable(pattern, $"Pattern {index}", converter, false);
            });

            serializer.WriteVector2(m_GameGridSize, "Game Grid Size");
            serializer.WriteSerializable(m_PluginLODSystem, "LOD System", converter, false);
            serializer.WriteSerializable(m_ResourceDescriptorSystem, "Resource Descriptor System", converter, false);
            serializer.WriteSerializable(m_ResourceGroupSystem, "Resource Group System", converter, false);

            EditorPrefs.SetInt(DecorationDefine.CREATE_MODE, (int)m_CreateMode);
            EditorPrefs.SetFloat(DecorationDefine.REMOVE_RANGE, m_RemoveRange);
            EditorPrefs.SetFloat(DecorationDefine.CIRCLE_RADIUS, m_CoordinateGenerateSetting.CircleRadius);
            EditorPrefs.SetFloat(DecorationDefine.RECT_WIDTH, m_CoordinateGenerateSetting.RectWidth);
            EditorPrefs.SetFloat(DecorationDefine.RECT_HEIGHT, m_CoordinateGenerateSetting.RectHeight);
            EditorPrefs.SetInt(DecorationDefine.OBJECT_COUNT, m_CoordinateGenerateSetting.Count);
            EditorPrefs.SetFloat(DecorationDefine.SPACE, m_CoordinateGenerateSetting.Space);
            EditorPrefs.SetBool(DecorationDefine.RANDOM, m_CoordinateGenerateSetting.Random);
            EditorPrefs.SetFloat(DecorationDefine.BORDER_SIZE, m_CoordinateGenerateSetting.BorderSize);
            EditorPrefs.SetBool(DecorationDefine.LINE_EQUIDISTANT, m_CoordinateGenerateSetting.LineEquidistant);
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            var version = deserializer.ReadInt32("DecorationSystem.Version");

            m_Name = deserializer.ReadString("Name");
            m_Bounds = deserializer.ReadBounds("Bounds");
            if (version >= 4)
            {
                m_EnableInstanceRendering = deserializer.ReadBoolean("Enable Instance Rendering");
            }

            var allObjects = deserializer.ReadList("Objects", (index) =>
            {
                return deserializer.ReadSerializable<DecorationObject>($"Object {index}", false);
            });
            foreach (var obj in allObjects)
            {
                m_Decorations.Add(obj.ID, obj);
            }

            if (version >= 3)
            {
                m_Patterns = deserializer.ReadList("Patterns", (index) =>
                {
                    return deserializer.ReadSerializable<Pattern>($"Pattern {index}", false);
                });
            }

            m_GameGridSize = deserializer.ReadVector2("Game Grid Size");
            m_PluginLODSystem = deserializer.ReadSerializable<IPluginLODSystem>("LOD System", false);
            m_ResourceDescriptorSystem = deserializer.ReadSerializable<EditorResourceDescriptorSystem>("Resource Descriptor System", false);

            m_ResourceGroupSystem = deserializer.ReadSerializable<IResourceGroupSystem>("Resource Group System", false);
        }

        public void CreateDecoration(string prefabPath, Vector3 worldPosition, Quaternion worldRotation, Vector3 worldScale, DecorationObjectFlags flags = DecorationObjectFlags.None)
        {
            var descriptor = m_ResourceDescriptorSystem.CreateDescriptorIfNotExists(prefabPath, World);
            var obj = new DecorationObject(World.AllocateObjectID(), m_Decorations.Count, false, LODLayerMask.AllLOD, worldPosition, worldRotation, worldScale, descriptor, flags);

            UndoSystem.CreateObject(obj, World.ID, DecorationDefine.ADD_DECORATION_NAME, ID, CurrentLOD);
        }

        public void DestroyImportedObjects()
        {
            List<DecorationObject> decorations = new();
            foreach (var kv in m_Decorations)
            {
                if (kv.Value.IsImportedFromConfig)
                {
                    decorations.Add(kv.Value);
                }
            }

            foreach (var obj in decorations)
            {
                UndoSystem.DestroyObject(obj, DecorationDefine.REMOVE_DECORATION_NAME, ID);
            }
        }

        protected override void UpdateInternal(float dt)
        {
            if ((m_ActivePatternIndex < 0 || m_ActivePatternIndex >= m_Patterns.Count) &&
                m_Patterns.Count > 0)
            {
                SetActivePattern(0);
            }
        }

        private void UpdateObjectLOD(int objectID, int lod)
        {
            m_Renderer.UpdateObjectLOD(objectID, lod);
        }

        private void AddDecoration(DecorationObject decoration)
        {
            m_Decorations.Add(decoration.ID, decoration);

            if (decoration.GetVisibility() == WorldObjectVisibility.Undefined)
            {
                decoration.SetVisibility(WorldObjectVisibility.Visible);
            }

            if (decoration.IsActive)
            {
                m_Renderer.Create(decoration, CurrentLOD);
            }
        }

        private int QueryObjectCountInLOD(int lod)
        {
            var count = 0;
            foreach (var obj in m_Decorations.Values)
            {
                if (obj.ExistsInLOD(lod))
                {
                    ++count;
                }
            }
            return count;
        }

        private List<DecorationObject> QueryObjectsInRectangle(Vector3 center, float width, float height)
        {
            var objects = new List<DecorationObject>();
            foreach (var kv in m_Decorations)
            {
                var freeObject = kv.Value;
                var positionDelta = center - freeObject.Position;
                if (Mathf.Abs(positionDelta.x) <= width * 0.5f &&
                    Mathf.Abs(positionDelta.z) <= height * 0.5f)
                {
                    objects.Add(freeObject);
                }
            }
            return objects;
        }

        private bool DestroyDecoration(int objectID)
        {
            var obj = World.QueryObject<DecorationObject>(objectID);
            if (obj != null &&
                obj.IsActive)
            {
                m_Renderer.Destroy(obj, CurrentLOD, true);
            }
            var ok = m_Decorations.TryGetValue(objectID, out var decoration);
            if (decoration != null)
            {
                decoration.Uninit();
                m_Decorations.Remove(objectID);
            }
            return ok;
        }

        private bool DestroyPattern(int objectID)
        {
            for (var i = 0; i < m_Patterns.Count; ++i)
            {
                if (m_Patterns[i].ID == objectID)
                {
                    m_Patterns[i].Uninit();
                    m_Patterns.RemoveAt(i);

                    return true;
                }
            }
            return false;
        }

        private DecorationObject CloneObject(int id, int objectIndex, DecorationObject obj)
        {
            if (obj == null)
            {
                Debug.Assert(false, $"Clone object failed: {id}");
                return null;
            }
            //复制的物体要去掉导入属性
            var old = obj.IsImportedFromConfig;
            obj.IsImportedFromConfig = false;
            var bytes = UndoSystem.Serialize(obj);
            var newObj = UndoSystem.Deserialize(id, objectIndex, bytes, World.ID, typeof(DecorationObject).FullName, false) as DecorationObject;
            obj.IsImportedFromConfig = old;
            return newObj;
        }

        private List<int> GetDirtyObjectIDs()
        {
            return m_DirtyObjectIDs;
        }

        private enum ObjectCreateMode
        {
            Single = 0,
            Multiple,
        }

        private string m_Name;
        private ObjectCreateMode m_CreateMode = ObjectCreateMode.Single;
        private DecorationSystemRenderer m_Renderer;
        private IMeshIndicator m_Indicator;
        private Vector2 m_GameGridSize;
        private IPluginLODSystem m_PluginLODSystem;
        private Bounds m_Bounds;
        private ICameraVisibleAreaUpdater m_AreaUpdater;
        private CoordinateGenerateSetting m_CoordinateGenerateSetting = new();
        private Dictionary<int, DecorationObject> m_Decorations = new();
        private List<Pattern> m_Patterns = new();
        private int m_ActiveLOD = -1;
        private int m_ActivePatternIndex = -1;
        private IEditorResourceDescriptorSystem m_ResourceDescriptorSystem;
        private float m_RemoveRange = 5;
        private List<int> m_DirtyObjectIDs = new();
        /// <summary>
        /// 导出数据范围
        /// </summary>
        private Rect m_ExportRange;
        private const int m_Version = 4;
    }
}
