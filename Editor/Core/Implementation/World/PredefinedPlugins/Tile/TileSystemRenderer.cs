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
using UnityEditor;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileSystemRenderer
    {
        public GameObject Root => m_Root;

        public TileSystemRenderer(TileSystem system) 
        {
            m_TileGameObjects = new GameObject[system.XTileCount * system.YTileCount];
            m_GameObjectPool = system.World.GameObjectPool;
            m_TileSystem = system;
            CreateRoot();

            m_RuntimeHeightMeshManager = new TerrainHeightMeshManager(system.World.GameFolder, system.World.AssetLoader);
        }

        public void OnDestroy()
        {
            foreach (var renderer in m_TileGameObjects)
            {
                Helper.DestroyUnityObject(renderer);
            }

            Helper.DestroyUnityObject(m_Root);
        }

        public MeshRenderer GetTileMeshRenderer(int x, int y)
        {
            var gameObject = GetTileGameObject(x, y);
            if (gameObject != null)
            {
                var renderers = gameObject.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer.gameObject.CompareTag(TileDefine.TILE_ROOT_TAG) && 
                        renderer.sharedMaterial != null)
                    {
                        return renderer;
                    }
                }

                return renderers[0];
            }

            return null;
        }

        public void SyncRotation()
        {
            m_Root.transform.rotation = m_TileSystem.Rotation;
        }

        public void ShowTile(TileObject tile, int x, int y)
        {
            var idx = y * m_TileSystem.XTileCount + x;
            if (m_TileGameObjects[idx] != null && m_TileGameObjects[idx].gameObject != null)
            {
                return;
            }

            var gameObject = m_GameObjectPool.Get(tile.AssetPath);
            m_TileGameObjects[idx] = gameObject;
            gameObject.tag = WorldDefine.EDITOR_ONLY_TAG;
            gameObject.transform.localPosition = tile.Position;
            gameObject.transform.SetParent(m_Root.transform, false);
            if (gameObject.GetComponent<NoKeyDeletion>() == null)
            {
                gameObject.AddComponent<NoKeyDeletion>();
            }
            UpdateMesh(x, y, false);
        }

        public GameObject GetTileGameObject(int x, int y)
        {
            return m_TileGameObjects[y * m_TileSystem.XTileCount + x];
        }

        public void SetAspect(int tileObjectID, string name, IAspect aspect)
        {
            var tile = m_TileSystem.QueryObjectUndo(tileObjectID) as TileObject;
            var coord = m_TileSystem.UnrotatedPositionToCoordinate(tile.Position.x, tile.Position.z);

            if (name == TileDefine.ENABLE_ASPECT_NAME)
            {
                ToggleActiveState(tile, coord.x, coord.y);
            }
            else if (name == TileDefine.TILE_MATERIAL_ID_NAME)
            {
            }
            else if (name.StartsWith(TileDefine.SHADER_PROPERTY_ASPECT_NAME))
            {
                ParseActionInfo(name, out var aspectName, out var shaderPropName);
                var material = GetTileMeshRenderer(coord.x, coord.y).sharedMaterial;
                EditorUtility.SetDirty(material);
                switch (aspectName)
                {
                    case TileDefine.SHADER_SINGLE_PROPERTY_NAME:
                        {
                            material.SetFloat(shaderPropName, aspect.GetSingle());
                            break;
                        }
                    case TileDefine.SHADER_VECTOR4_PROPERTY_NAME:
                        {
                            material.SetVector(shaderPropName, aspect.GetVector4());
                            break;
                        }
                    case TileDefine.SHADER_COLOR_PROPERTY_NAME:
                        {
                            material.SetColor(shaderPropName, aspect.GetColor());
                            break;
                        }
                    case TileDefine.SHADER_TEXTURE_PROPERTY_NAME:
                        {
                            material.SetTexture(shaderPropName, AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(aspect.GetString())));
                            break;
                        }
                    default:
                        Debug.Assert(false, $"todo: {aspectName}");
                        break;
                }
            }
            else
            {
                Debug.Assert(false, $"todo: {name}");
            }
        }

        public IAspect GetAspect(int objectID, string name)
        {
            if (!name.StartsWith(TileDefine.SHADER_PROPERTY_ASPECT_NAME))
            {
                return null;
            }
            
            ParseActionInfo(name, out var aspectName, out var shaderPropName);

            var tile = m_TileSystem.QueryObjectUndo(objectID) as TileObject;
            var coord = m_TileSystem.UnrotatedPositionToCoordinate(tile.Position.x, tile.Position.z);
            var material = GetTileMeshRenderer(coord.x, coord.y).sharedMaterial;

            switch (aspectName)
            {
                case TileDefine.SHADER_SINGLE_PROPERTY_NAME:
                    {
                        return IAspect.FromSingle(material.GetFloat(shaderPropName));
                    }
                case TileDefine.SHADER_VECTOR4_PROPERTY_NAME:
                    {
                        return IAspect.FromVector4(material.GetVector(shaderPropName));
                    }
                case TileDefine.SHADER_COLOR_PROPERTY_NAME:
                    {
                        return IAspect.FromColor(material.GetColor(shaderPropName));
                    }
                case TileDefine.SHADER_TEXTURE_PROPERTY_NAME:
                    {
                        return IAspect.FromString(EditorHelper.GetObjectGUID(material.GetTexture(shaderPropName)));
                    }
                default:
                    Debug.Assert(false, $"todo: {aspectName}");
                    return null;
            }
        }

        public void ToggleActiveState(TileObject tile, int x, int y)
        {
            if (tile.IsActive)
            {
                ShowTile(tile, x, y);
            }
            else
            {
                HideTile(tile, x, y);
            }
        }

        private void CreateRoot()
        {
            m_Root = new GameObject(m_TileSystem.Name)
            {
                tag = WorldDefine.EDITOR_ONLY_TAG
            };
            m_Root.transform.SetParent(m_TileSystem.World.Root.transform, true);
            m_Root.transform.rotation = m_TileSystem.Rotation;
            m_Root.transform.SetSiblingIndex(m_TileSystem.ObjectIndex);
            Selection.activeGameObject = m_Root;
        }

        private void HideTile(TileObject tile, int x, int y)
        {
            var idx = y * m_TileSystem.XTileCount + x;
            if (m_TileGameObjects[idx] == null)
            {
                return;
            }
            
            string prefabPath = tile.AssetPath;
            var filter = m_TileGameObjects[idx].GetComponentInChildren<MeshFilter>();
            var originalMesh = m_RuntimeHeightMeshManager.GetOriginalMesh(prefabPath);
            if (originalMesh != null) 
            {
                filter.sharedMesh = originalMesh;
            }

            m_GameObjectPool.Release(tile.AssetPath, m_TileGameObjects[idx]);
            m_TileGameObjects[idx] = null;
        }

        private void ParseActionInfo(string text, out string aspectName, out string shaderPropName)
        {
            var tokens = text.Split("@");
            aspectName = tokens[1];
            shaderPropName = tokens[2];
        }

        private readonly GameObject[] m_TileGameObjects;
        private GameObject m_Root;
        private readonly IGameObjectPool m_GameObjectPool;
        private readonly TileSystem m_TileSystem;
    };
}


//XDay