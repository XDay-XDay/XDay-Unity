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



using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using XDay.Node.Editor;
using XDay.UtilityAPI;
using XDay.WorldAPI;

namespace XDay.Terrain.Editor
{
    internal class TerrainGenEditor : EditorWindow, IXNodeCoordinateConverter, IXNodeEditorEventListener
    {
        //[MenuItem("XDay/地图/地形生成")]
        //private static void Open()
        //{
        //    GetWindow<TerrainGenEditor>().Show();
        //}

        private void OnGUI()
        {
            // First Panel
            GUILayout.BeginArea(new Rect(0, 0, m_SplitterPosition * position.width, position.height), EditorStyles.helpBox);

            if (GUILayout.Button("Create"))
            {
                Create();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                Load();
            }

            if (GUILayout.Button("Save"))
            {
                Save();
            }

            if (GUILayout.Button("Reset"))
            {
                Clear();
            }
            EditorGUILayout.EndHorizontal();

            mNodeEditor?.Draw();

            GUILayout.EndArea();

            // Splitter
            var splitterRect = new Rect(m_SplitterPosition * position.width, 0, 5, position.height);
            EditorGUI.DrawRect(splitterRect, Color.gray);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            // Dragging logic for the splitter
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                m_IsDragging = true;
            }
            if (m_IsDragging && Event.current.type == EventType.MouseDrag)
            {
                m_SplitterPosition += Event.current.delta.x / position.width;
                Repaint();
            }
            if (Event.current.type == EventType.MouseUp) m_IsDragging = false;

            // Second Panel
            GUILayout.BeginArea(new Rect(m_SplitterPosition * position.width + 5, 0, position.width - m_SplitterPosition * position.width - 5, position.height), EditorStyles.helpBox);
            DrawNodeSetting();
            GUILayout.EndArea();
        }

        public void OnNodeWillBeDeleted(XNode node)
        {
            if (node == mSelectedNode)
            {
                mSelectedNode = null;
            }
        }

        public void OnNodeDeleted(XNode node)
        {
            var terrainNode = node as TerrainModifierNode;
            var modifier = terrainNode.GetModifier();
            modifier.OnDestroy();
        }

        public void OnNodeOutputDisconnected(XNode node, int outputIndex)
        {
            var modifier = GetModifier(node);
            modifier.DisconnectOutput(outputIndex);
        }

        public void OnNodeInputDisconnected(XNode node, int inputIndex)
        {
            var modifier = GetModifier(node);
            modifier.DisconnectInput(inputIndex);
        }

        public void OnNodeOutputConnected(XNode from, int outputIndex, XNode to, int inputIndex)
        {
            var fromModifier = GetModifier(from);
            var toModifier = GetModifier(to);
            fromModifier.OutputTo(outputIndex, toModifier, inputIndex);
        }

        public void OnNodeSelected(XNode node)
        {
            mSelectedNode = node as TerrainModifierNode;
        }

        public void OnNodeDoubleClicked(XNode node)
        {
            var modifier = GetModifier(node);
            mGenerator.Generate(modifier);
        }

        public Vector2 ScreenToContentPosition(Vector2 screenPos)
        {
            //return ToLocalPosition(screenPos);
            return screenPos;
        }

        public Vector2 ContentToScreenPosition(Vector2 localPos)
        {
            //return ToGlobalPosition(localPos);
            return localPos;
        }

        public Vector2 GetContentSize()
        {
            return position.size;
        }

        private void Connect(TerrainModifierNode node)
        {
            if (node != null)
            {
                var modifier = node.GetModifier();

                var outputs = modifier.GetOutputs();
                for (var i = 0; i < outputs.Length; ++i)
                {
                    foreach (var connection in outputs[i].Connections)
                    {
                        var outputNode = GetNode(connection.ModifierID);
                        mNodeEditor.ConnectNode(node, i, outputNode, connection.Index, false);
                        Connect(outputNode);
                    }
                }
            }
        }

        private TerrainModifierNode GetNode(int modifierID)
        {
            if (modifierID == 0)
            {
                return null;
            }

            foreach (var node in mNodeEditor.GetNodes())
            {
                if (GetModifier(node).GetID() == modifierID)
                {
                    return node as TerrainModifierNode;
                }
            }

            Debug.Assert(false);
            return null;
        }

        private TerrainModifier GetModifier(XNode node)
        {
            var terrainNode = node as TerrainModifierNode;
            return terrainNode.GetModifier();
        }

        private void DrawNodeSetting()
        {
            if (mSelectedNode != null)
            {
                var modifier = GetModifier(mSelectedNode);
                modifier.DrawInspector();
            }
        }

        private void CreateMenu()
        {
            mNodeEditor.AddMenuItem("Execute", () =>
        {
            var modifier = GetModifier(mSelectedNode);
            mGenerator.Generate(modifier);
        }, () => { return mSelectedNode != null; });

            mNodeEditor.AddMenuItem("Modifier/Noise", () =>
        {
            CreateModifier<Noise>();
        }, () => { return true; });

            mNodeEditor.AddMenuItem("Modifier/Erosion", () =>
        {
            CreateModifier<Erosion>();
        }, () => { return true; });

            mNodeEditor.AddMenuItem("Modifier/Weight Map", () =>
        {
            CreateModifier<WeightMap>();
        }, () => { return true; });

            mNodeEditor.AddMenuItem("Modifier/Fault", () =>
        {
            CreateModifier<Fault>();
        }, () => { return true; });

            mNodeEditor.AddMenuItem("Modifier/Height Map", () =>
        {
            CreateModifier<HeightMap>();
        }, () => { return true; });

            mNodeEditor.AddMenuItem("Modifier/Combine", () =>

            {
                CreateModifier<Combine>();
            }, () => { return true; });

            mNodeEditor.AddMenuItem("Selector/Slope", () =>

            {
                CreateModifier<Slope>();
            }, () => { return true; });

            mNodeEditor.AddMenuItem("Selector/Height", () =>

            {
                CreateModifier<Height>();
            }, () => { return true; });

            mNodeEditor.AddMenuItem("Color/Color Gradient", () =>

            {
                CreateModifier<ColorGradient>();
            }, () => { return true; });

            mNodeEditor.AddMenuItem("Color/RGBA", () =>

            {
                CreateModifier<RGBA>();
            }, () => { return true; });

            mNodeEditor.AddMenuItem("Output/Texture Blend", () =>

            {
                CreateModifier<TextureBlend>();
            }, () => { return true; });

            mNodeEditor.AddMenuItem("Filter/FIR", () =>

            {
                CreateModifier<FIRFilter>();
            }, () => { return true; });
        }

        private T CreateModifier<T>() where T : TerrainModifier, new()
        {
            T modifier = Activator.CreateInstance(typeof(T), mGenerator.GetNextID()) as T;
            DoCreateModifier(modifier);
            return modifier;
        }

        private void DoCreateModifier(TerrainModifier modifier)
        {
            mGenerator.AddModifier(modifier);

            var node = new TerrainModifierNode(modifier.GetID());
            node.Initialize(mGenerator);

            node.SetPosition(mNodeEditor.GetContextMenuPos());
            mNodeEditor.AddNode(node);
        }

        private void Create()
        {
            Clear();

            mGenerator = new TerrainGenerator(0);
            mNodeEditor = new XNodeEditor(this, this, createNode :() =>
            {
                var node = new TerrainModifierNode();
                node.Initialize(mGenerator);

                return node;
            },
             getUserData :() =>
             {
                 return mGenerator;
             }, () => { Repaint(); });

            Vector2 pos = new(-400, 0);

            var rootModifier = mGenerator.GetStartModifier();
            TerrainModifierNode rootNode = null;
            foreach (var modifier in mGenerator.GetModifiers())
            {
                var node = new TerrainModifierNode(modifier.GetID());
                node.Initialize(mGenerator);

                node.SetPosition(pos);

                if (modifier == rootModifier)
                {
                    rootNode = node;
                }

                mNodeEditor.AddNode(node);

                pos += new Vector2(node.GetSize().x + 10, 0);
            }

            Connect(rootNode);

            CreateMenu();
        }

        private void Load()
        {
            Clear();

            var reader = IDeserializer.CreateBinary(new FileStream("Assets/Terrain.bytes", FileMode.Open), null);

            mGenerator = new TerrainGenerator();
            mGenerator.Load(reader);

            mNodeEditor = new XNodeEditor(this, this, createNode: () => { return new TerrainModifierNode(); }, getUserData: () => { return mGenerator; }, () => { Repaint(); });
            mNodeEditor.Load(reader);

            reader.Uninit();

            CreateMenu();
        }

        private void Save()
        {
            if (mGenerator == null)
            {
                return;
            }

            IObjectIDConverter translator = new ToPersistentID();

            var writer = ISerializer.CreateBinary();

            mGenerator.Save(writer, translator);

            mNodeEditor.Save(writer, translator);

            writer.Uninit();

            File.WriteAllBytes("Assets/Terrain.bytes", writer.Data);
        }

        private void Clear()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var obj in scene.GetRootGameObjects())
            {
                Helper.DestroyUnityObject(obj);
            }

            mNodeEditor?.OnDestroy();
            mNodeEditor = null;

            mGenerator?.OnDestroy();
            mGenerator = null;

            mSelectedNode = null;
        }

        private XNodeEditor mNodeEditor = null;
        private TerrainModifierNode mSelectedNode = null;
        private TerrainGenerator mGenerator = null;
        private float m_SplitterPosition = 0.75f;
        private bool m_IsDragging;
    };
}
