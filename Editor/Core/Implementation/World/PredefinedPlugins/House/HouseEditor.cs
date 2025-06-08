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
using UnityEditor;
using XDay.WorldAPI.Editor;
using XDay.UtilityAPI;
using XDay.WorldAPI.City.Editor;

namespace XDay.WorldAPI.House.Editor
{
    [WorldPluginMetadata("房间编辑器", "house_editor_data", typeof(HouseEditorCreateWindow), true)]
    internal partial class HouseEditor : EditorWorldPlugin, IScenePrefabSetter
    {
        public override string Name { get => m_Name; set { m_Name = value; m_Root.name = value; } }
        public override GameObject Root => m_Root;
        public GameObject HouseInstanceRoot => m_HouseInstanceRoot;
        public GameObject HouseTemplateRoot => m_HouseTemplateRoot;
        public override List<string> GameFileNames => new() { "house" };
        public override string TypeName => "HouseEditor";
        public int NextObjectID => --m_NextObjectID;
        public override bool AllowUndo => false;
        public bool Visible { get => m_ScenePrefab.Visible; set => m_ScenePrefab.Visible = value; }
        public GameObject Prefab { get => m_ScenePrefab.Prefab; set => m_ScenePrefab.Prefab = value; }
        public GameObject PrefabInstance => m_ScenePrefab.Instance;
        Vector3 IScenePrefabSetter.Position
        {
            get
            {
                if (PrefabInstance != null)
                {
                    return PrefabInstance.transform.position;
                }
                return Vector3.zero;
            }
            set
            {
                if (PrefabInstance != null)
                {
                    PrefabInstance.transform.position = value;
                }
            }
        }
        Quaternion IScenePrefabSetter.Rotation
        {
            get
            {
                if (PrefabInstance != null)
                {
                    return PrefabInstance.transform.rotation;
                }
                return Quaternion.identity;
            }
            set
            {
                if (PrefabInstance != null)
                {
                    PrefabInstance.transform.rotation = value;
                }
            }
        }

        public HouseEditor()
        {
        }

        public HouseEditor(int id, int objectIndex, string name)
            : base(id, objectIndex)
        {
            m_Name = name;
            m_ResourceDescriptorSystem = IEditorResourceDescriptorSystem.Create();
        }

        protected override void InitInternal()
        {
            SubscribeEvents();

            Selection.selectionChanged += OnSelectionChanged;

            m_ResourceDescriptorSystem.Init(World);

            m_Root = new GameObject(m_Name);
            m_Root.transform.SetParent(World.Root.transform);
            m_Root.AddComponent<NoKeyDeletion>();

            m_HouseTemplateRoot = CreateItemRoot("房间模板");
            m_HouseInstanceRoot = CreateItemRoot("房间");

            m_MeshIndicator = IMeshIndicator.Create(World);
            m_TileIndicator = IGizmoCubeIndicator.Create();

            foreach (var house in m_Houses)
            {
                house.Init(World);
            }

            foreach (var house in m_HouseInstances)
            {
                house.Init(World);
            }

            if (m_ActiveHouseInstanceID == 0 && m_HouseInstances.Count > 0)
            {
                SetActiveHouseInstance(m_HouseInstances[0].ID);
            }

            if (m_ActiveHouseID == 0 && m_Houses.Count > 0)
            {
                SetActiveHouse(m_Houses[0].ID);
            }

            m_ScenePrefab.Initialize(m_Root.transform, true);
        }

        protected override void UninitInternal()
        {
            m_ScenePrefab.OnDestroy();

            foreach (var house in m_HouseInstances)
            {
                house.Uninit();
            }
            m_HouseInstances.Clear();

            foreach (var house in m_Houses)
            {
                house.Uninit();
            }
            m_Houses.Clear();

            m_MeshIndicator.OnDestroy();

            m_ResourceDescriptorSystem.Uninit();

            Helper.DestroyUnityObject(m_Root);

            Selection.selectionChanged -= OnSelectionChanged;

            UnsubscribeEvents();
        }

        protected override void UpdateInternal(float dt)
        {
            foreach (var house in m_Houses)
            {
                house.Update(dt);
            }

            foreach (var house in m_HouseInstances)
            {
                house.Update(dt);
            }
        }

        protected override void SelectionChangeInternal(bool selected)
        {
            if (!selected)
            {
                Tools.hidden = false;
            }
            else
            {
                SetOperation(m_Operation);
            }
        }

        public void SetOperation(OperationType operation)
        {
            if (m_Operation != operation)
            {
                m_Operation = operation;
                RefreshOperation();
            }
        }

        public void SetEditMode(EditMode mode)
        {
            if (m_EditMode != mode)
            {
                m_EditMode = mode;
                Selection.activeGameObject = null;
                RefreshEditMode();
            }
        }

        private void RefreshOperation()
        {
            if (m_Operation == OperationType.Select)
            {
                Tools.hidden = false;
                return;
            }

            foreach (var house in m_Houses)
            {
                house.HideLayers();
            }

            switch (m_Operation)
            {
                case OperationType.SetWalkable:
                    foreach (var house in m_Houses)
                    {
                        house.SetLayerVisibility<HouseWalkableLayer>(house.IsWalkableLayerActive);
                    }
                    break;
                default:
                    break;
            }
        }

        private void RefreshEditMode()
        {
            foreach (var house in m_Houses)
            {
                house.SetActive(m_EditMode == EditMode.House);
            }
            foreach (var houseInstance in m_HouseInstances)
            {
                houseInstance.SetActive(m_EditMode == EditMode.HouseInstance);
            }
        }

        public House CreateHouse(string name, string assetPath, float gridSize)
        {
            var descriptor = m_ResourceDescriptorSystem.CreateDescriptorIfNotExists(assetPath, World);
            var house = new House(World.AllocateObjectID(), m_Houses.Count, name, gridSize, descriptor);
            return house;
        }

        public HouseInstance CreateHouseInstance(House house, string name)
        {
            var houseInstance = new HouseInstance(World.AllocateObjectID(), m_HouseInstances.Count, name, house.GridSize, house.ResourceDescriptor, house.ID);
            return houseInstance;
        }

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            if (obj is HouseInstance houseInstance)
            {
                m_HouseInstances.Add(houseInstance);
                m_HouseInstances.Sort((a, b) =>
                {
                    return a.ObjectIndex.CompareTo(b.ObjectIndex);
                });
            }
            else if (obj is House house)
            {
                m_Houses.Add(house);
                m_Houses.Sort((a, b) =>
                {
                    return a.ObjectIndex.CompareTo(b.ObjectIndex);
                });
            }
        }

        public override void DestroyObjectUndo(int objectID)
        {
            var obj = QueryObjectUndo(objectID);
            if (obj == null)
            {
                Debug.LogError($"destroy object {objectID} failed!");
                return;
            }
            if (obj is HouseInstance houseInstance)
            {
                houseInstance.Uninit();
                m_HouseInstances.Remove(houseInstance);
                if (objectID == m_ActiveHouseInstanceID)
                {
                    SetActiveHouseInstance(0);
                }
            }
            else if (obj is House house)
            {
                house.Uninit();
                m_Houses.Remove(house);
                if (objectID == m_ActiveHouseID)
                {
                    SetActiveHouse(0);
                }
            }
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            return World.QueryObject<IWorldObject>(objectID);
        }

        private void OnSelectionChanged()
        {
            EditorApplication.delayCall += () =>
            {
                var gameObject = Selection.activeGameObject;
                if (gameObject == null)
                {
                    return;
                }

                var house = GetHouseFromGameObject(gameObject);
                if (house != null)
                {
                    SetActiveHouse(house.ID);
                }
                else
                {
                    var houseInstance = GetHouseInstanceFromGameObject(gameObject);
                    if (houseInstance != null)
                    {
                        SetActiveHouseInstance(houseInstance.ID);
                    }
                }
            };
        }

        private House GetHouseFromGameObject(GameObject gameObject)
        {
            foreach (var house in m_Houses)
            {
                if (house.Root == gameObject)
                {
                    return house;
                }
            }
            return null;
        }

        private HouseInstance GetHouseInstanceFromGameObject(GameObject gameObject)
        {
            foreach (var houseInstance in m_HouseInstances)
            {
                if (houseInstance.Root == gameObject)
                {
                    return houseInstance;
                }
            }
            return null;
        }

        private House GetHouse(int houseID)
        {
            return QueryObjectUndo(houseID) as House;
        }

        private HouseInstance GetHouseInstance(int houseID)
        {
            return QueryObjectUndo(houseID) as HouseInstance;
        }

        private House GetActiveHouse()
        {
            return GetHouse(m_ActiveHouseID);
        }

        private HouseInstance GetActiveHouseInstance()
        {
            return GetHouseInstance(m_ActiveHouseInstanceID);
        }

        private void SetActiveHouse(int houseID)
        {
            if (houseID != m_ActiveHouseID)
            {
                SetEditMode(EditMode.House);
                m_ActiveHouseID = houseID;
                var house = GetActiveHouse();
                if (house != null)
                {
                    var gameObject = house.Root;
                    if (gameObject != null)
                    {
                        Selection.activeGameObject = gameObject;
                    }
                }

                EditorWindow.GetWindow<WorldEditorEntrance>().Repaint();
            }
        }

        private void SetActiveHouseInstance(int houseInstanceID)
        {
            if (houseInstanceID != m_ActiveHouseInstanceID)
            {
                SetEditMode(EditMode.HouseInstance);
                m_ActiveHouseInstanceID = houseInstanceID;
                var houseInstance = GetActiveHouseInstance();
                if (houseInstance != null)
                {
                    var gameObject = houseInstance.Root;
                    if (gameObject != null)
                    {
                        Selection.activeGameObject = gameObject;
                    }
                }

                EditorWindow.GetWindow<WorldEditorEntrance>().Repaint();
            }
        }

        public HouseAgentTemplate GetAgentTemplate(int id)
        {
            if (id == 0)
            {
                return null;
            }

            foreach (var agent in m_AgentTemplates)
            {
                if (agent.ID == id)
                {
                    return agent;
                }
            }

            return null;
        }

        private void SubscribeEvents()
        {
            WorldEditor.EventSystem.Register(this, (DestroyAgentEvent e) => {
                var grid = GetHouse(e.HouseID);
                if (grid != null)
                {
                    grid.AddToDestroyQueue(grid.GetAgentInstance(e.AgentID));
                }
            });

            WorldEditor.EventSystem.Register(this, (DestroyHouseEvent e) => {
            });

            WorldEditor.EventSystem.Register(this, (DestroyHouseInstanceEvent e) => {
            });
        }

        private void UnsubscribeEvents()
        {
            WorldEditor.EventSystem.Unregister(this);
        }

        private GameObject CreateItemRoot(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(m_Root.transform, worldPositionStays: false);
            root.AddComponent<NoKeyDeletion>();
            return root;
        }

        /// <summary>
        /// 根据屏幕坐标找到对应的房间
        /// </summary>
        /// <param name=""></param>
        /// <param name="targetHouse"></param>
        public Vector3 HouseRaycastHit(Vector2 screenPos, out HouseInstance targetHouse)
        {
            var camera = SceneView.lastActiveSceneView.camera;
            targetHouse = null;
            foreach (var house in m_HouseInstances)
            {
                var worldPos = Helper.GUIRayCastWithXZPlane(screenPos, camera, house.WorldHeight);
                var coord = house.PositionToCoordinate(worldPos);
                if (house.IsValidCoordinate(coord.x, coord.y))
                {
                    targetHouse = house;
                    return worldPos;
                }
            }

            return Vector3.zero;
        }

        public bool FindPath(Vector3 curPosition, Vector3 targetPosition, HouseInstance targetHouse, List<Vector3> path)
        {
            if (targetHouse == null)
            {
                Debug.LogError("寻路失败,无效的目标房间!");
                return false;
            }

            path.Clear();

            m_HouseGraph = new(m_HouseInstances);

            var curHouse = LocateHouseInstance(curPosition);
            List<List<Vector3>> pathInRooms = new();
            bool found = m_HouseGraph.FindPath(curPosition, targetPosition, curHouse, targetHouse, pathInRooms);
            if (found)
            {
                foreach (var pathInRoom in pathInRooms)
                {
                    path.AddRange(pathInRoom);
                }
            }
            return found;
        }

        /// <summary>
        /// 判断点在哪个房间内
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public HouseInstance LocateHouseInstance(Vector3 position)
        {
            foreach (var house in m_HouseInstances)
            {
                if (house.ContainsPoint(position))
                {
                    return house;
                }
            }
            return null;
        }

        [SerializeField]
        private string m_Name;
        [SerializeField]
        private List<House> m_Houses = new();
        [SerializeField]
        private IEditorResourceDescriptorSystem m_ResourceDescriptorSystem;
        [SerializeField]
        private List<HouseAgentTemplate> m_AgentTemplates = new();
        [SerializeField]
        private List<HouseInstance> m_HouseInstances = new();
        [SerializeField]
        private bool m_ShowInteractivePointSettings = true;
        private bool m_ShowInteractivePointInstanceSettings = true;
        private int m_NextObjectID;
        private GameObject m_Root;
        private GameObject m_HouseTemplateRoot;
        private GameObject m_HouseInstanceRoot;
        private IMeshIndicator m_MeshIndicator;
        private IGizmoCubeIndicator m_TileIndicator;
        private int m_ActiveHouseID = 0;
        private int m_ActiveHouseInstanceID = 0;
        private ScenePrefab m_ScenePrefab = new();
        private HouseGraph m_HouseGraph;
    }
}