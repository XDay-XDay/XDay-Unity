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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Decoration.Editor
{
    internal class Pattern : WorldObject
    {
        public override string TypeName => "DecorationSystem.Pattern";
        public string Name => m_Name;
        public int ItemCount => m_Items.Count;

        public Pattern()
        {
        }

        public Pattern(int id, int index, string name, List<DecorationObject> decorations)
            : base(id, index)
        {
            m_Name = name;
            m_Bounds = CalculateBounds(decorations);

            m_Items = new();
            foreach (var decoration in decorations)
            {
                var item = CreateItem(decoration);
                m_Items.Add(item);
            }
        }

        protected override void OnInit()
        {
            CreateRenderer();
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (base.SetAspect(objectID, name, aspect))
            {
                return true;
            }

            if (name == DecorationDefine.PATTERN_NAME)
            {
                m_Name = aspect.GetString();
                m_Renderer.name = m_Name;
                return true;
            }

            return false;
        }

        public override IAspect GetAspect(int objectID, string name)
        {
            var aspect = base.GetAspect(objectID, name);
            if (aspect != null)
            {
                return aspect;
            }

            if (name == DecorationDefine.PATTERN_NAME)
            {
                return IAspect.FromString(m_Name);
            }

            Debug.Assert(false, $"Unknown aspect {name}");
            return null;
        }

        public void SetActive(bool active)
        {
            m_Renderer.SetActive(active);
        }

        public void SetPosition(Vector3 position)
        {
            m_Renderer.transform.position = position;
        }

        public void Rotate(float angle)
        {
            m_Renderer.transform.Rotate(new Vector3(0, angle, 0));
        }

        public new void Scale(float delta)
        {
            float s = m_Renderer.transform.localScale.x + delta;
            if (s < 0.01f)
            {
                s = 0.01f;
            }
            m_Renderer.transform.localScale = Vector3.one * s;
        }

        public bool GetItemInfo(int index, out string prefabPath, out Vector3 worldPosition, out Vector3 worldScale, out Quaternion worldRotation)
        {
            prefabPath = null;
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;
            worldScale = Vector3.one;
            if (index >= 0 && index < m_Items.Count)
            {
                var item = m_Items[index];
                prefabPath = AssetDatabase.GUIDToAssetPath(item.AssetGUID);
                worldPosition = m_Renderer.transform.TransformPoint(item.LocalPosition);
                worldRotation = m_Renderer.transform.rotation * item.LocalRotation;
                worldScale = m_Renderer.transform.localScale.Mult(item.LocalScale);
                return true;
            }
            return false;
        }

        private void CreateRenderer()
        {
            m_Renderer = new GameObject(m_Name);
            m_Renderer.AddComponent<NoKeyDeletion>();
            m_Renderer.SetActive(false);
            Helper.HideGameObject(m_Renderer);
            foreach (var item in m_Items)
            {
                var obj = EditorHelper.LoadAssetByGUID<GameObject>(item.AssetGUID);
                if (obj != null)
                {
                    obj = Object.Instantiate(obj);
                    obj.transform.SetParent(m_Renderer.transform, false);
                    obj.transform.localPosition = item.LocalPosition;
                    obj.transform.localScale = item.LocalScale;
                    obj.transform.localRotation = item.LocalRotation;
                }
            }
        }

        protected override void OnUninit()
        {
            Helper.DestroyUnityObject(m_Renderer);
            m_Renderer = null;
        }

        private Bounds CalculateBounds(List<DecorationObject> decorations)
        {
            Bounds ret = new();
            for (var i = 0; i < decorations.Count; i++)
            {
                var decoration = decorations[i];
                var localBounds = decoration.QueryWorldBounds().ToBounds();
                if (i == 0)
                {
                    ret = localBounds;
                }
                else
                {
                    ret.Encapsulate(localBounds);
                }
            }
            return ret;
        }

        private Item CreateItem(DecorationObject decoration)
        {
            var item = new Item
            {
                AssetGUID = AssetDatabase.AssetPathToGUID(decoration.ResourceDescriptor.GetPath(0)),
                LocalPosition = decoration.Position - m_Bounds.center,
                LocalRotation = decoration.Rotation,
                LocalScale = decoration.Scale
            };
            Debug.Assert(!string.IsNullOrEmpty(item.AssetGUID));
            return item;
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_EditorVersion, "Pattern.Version");

            base.EditorSerialize(serializer, label, converter);

            serializer.WriteString(m_Name, "Name");
            serializer.WriteBounds(m_Bounds, "Bounds");
            serializer.WriteList(m_Items, "Items", (item, index) =>
            {
                serializer.WriteStructure($"Item {index}", () =>
                {
                    serializer.WriteString(m_Items[index].AssetGUID, "AssetGUID");
                    serializer.WriteVector3(m_Items[index].LocalPosition, "Position");
                    serializer.WriteVector3(m_Items[index].LocalScale, "Scale");
                    serializer.WriteQuaternion(m_Items[index].LocalRotation, "Rotation");
                });
            });
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("Pattern.Version");

            base.EditorDeserialize(deserializer, label);

            m_Name = deserializer.ReadString("Name");
            m_Bounds = deserializer.ReadBounds("Bounds");
            m_Items = deserializer.ReadList("Items", (index) =>
            {
                var item = new Item();
                deserializer.ReadStructure($"Item {index}", () =>
                {
                    item.AssetGUID = deserializer.ReadString("AssetGUID");
                    item.LocalPosition = deserializer.ReadVector3("Position");
                    item.LocalScale = deserializer.ReadVector3("Scale");
                    item.LocalRotation = deserializer.ReadQuaternion("Rotation");
                });
                return item;
            });
        }

        private string m_Name;
        private List<Item> m_Items;
        private Bounds m_Bounds;
        private GameObject m_Renderer;
        private const int m_EditorVersion = 1;

        public class Item
        {
            public string AssetGUID;
            public Vector3 LocalPosition;
            public Vector3 LocalScale;
            public Quaternion LocalRotation;
        }
    }
}
