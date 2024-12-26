/*
 * Copyright (c) 2024 XDay
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

namespace XDay.WorldAPI
{
    public class WorldSetupManager : ScriptableObject
    {
        public string GameFolder { get => m_GameFolder; set => m_GameFolder = value; }
        public string EditorFolder { get => m_EditorFolder; set => m_EditorFolder = value; }
        public List<WorldSetup> Setups => m_Setups;
        public string PreviewWorldName { get => m_PreviewWorldName; set => m_PreviewWorldName = value; }

        public WorldSetup AddSetup(int id, string name, string cameraSetupFileName)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("invalid world name!");
                return null;
            }

            if (QuerySetup(name) != null)
            {
                Debug.LogError($"setup name {name} already exists!");
                return null;
            }

            if (QuerySetup(id) != null)
            {
                Debug.LogError($"setup id {id} already exists!");
                return null;
            }

            if (string.IsNullOrEmpty(m_EditorFolder))
            {
                Debug.LogError("invalid editor root folder!");
                return null;
            }

            if (string.IsNullOrEmpty(m_GameFolder))
            {
                Debug.LogError("invalid game root folder!");
                return null;
            }

            var setup = new WorldSetup(name, id, $"{m_GameFolder}/{name}", $"{m_EditorFolder}/{name}", cameraSetupFileName);
            m_Setups.Add(setup);

            return setup;
        }

        public string GetValidName()
        {
            var start = 1;
            while (true)
            {
                var name = $"World{start}";
                if (QuerySetup(name) == null)
                {
                    return name;
                }
                ++start;
            }
        }

        public void RemoveSetup(WorldSetup setup)
        {
            m_Setups.Remove(setup);
        }

        public void RemoveSetup(int index)
        {
            if (index >= 0 && index < m_Setups.Count)
            {
                m_Setups.RemoveAt(index);
            }
        }

        public WorldSetup QuerySetup(string name)
        {
            foreach (var setup in m_Setups)
            {
                if (setup.Name == name)
                {
                    return setup;
                }
            }
            return null;
        }

        public WorldSetup QuerySetup(int id)
        {
            foreach (var setup in m_Setups)
            {
                if (setup.ID == id)
                {
                    return setup;
                }
            }
            return null;
        }

        public int GetValidID()
        {
            var max = 0;
            foreach (var setup in m_Setups)
            {
                if (setup.ID > max)
                {
                    max = setup.ID;
                }
            }
            return max + 1;
        }

        public bool ValidateID()
        {
            foreach (var setup in m_Setups)
            {
                if (setup.ID == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void ResetIDs()
        {
            int index = 0;
            foreach (var setup in m_Setups)
            {
                setup.ID = ++index;
            }
        }

        public void Save()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        [SerializeField]
        private List<WorldSetup> m_Setups = new();
        [SerializeField]
        private string m_GameFolder;
        [SerializeField]
        private string m_EditorFolder;
        [SerializeField]
        private string m_PreviewWorldName;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(WorldSetupManager))]
    internal class WorldSetupManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.TextArea("Edit in world editor!");

            GUI.enabled = false;
            base.OnInspectorGUI();
            GUI.enabled = true;
        }
    }
#endif
}

//XDay