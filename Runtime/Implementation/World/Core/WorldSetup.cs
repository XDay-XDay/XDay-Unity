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



using UnityEngine;

namespace XDay.WorldAPI
{
    [System.Serializable]
    public class WorldSetup
    {
        public int ID { get => m_ID; set => m_ID = value; }
        public string Name { get => m_Name; set => m_Name = value; }
        public string CameraSetupFileName { get => m_CameraSetupFileName; set => m_CameraSetupFileName = value; }
        public string CameraSetupFilePath => $"{GameFolder}/{WorldDefine.CONSTANT_FOLDER_NAME}/{CameraSetupFileName}.bytes";
        public string GameFolder { get => m_GameFolder; set => m_GameFolder = value; }
        public string EditorFolder { get => m_EditorFolder; set => m_EditorFolder = value; }
        public string SceneFilePath => $"{GameFolder}/{WorldDefine.CONSTANT_FOLDER_NAME}/{WorldDefine.WORLD_EDITOR_NAME}.unity";

        public WorldSetup(string name, int id, string gameFolder, string editorFolder, string cameraSetupFilePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(cameraSetupFilePath));
            m_Name = name;
            m_ID = id;
            m_GameFolder = gameFolder;
            m_EditorFolder = editorFolder;
            m_CameraSetupFileName = cameraSetupFilePath;
        }

        [SerializeField]
        private int m_ID;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private string m_EditorFolder;

        [SerializeField]
        private string m_GameFolder;

        [SerializeField]
        private string m_CameraSetupFileName;
    }
}

//XDay