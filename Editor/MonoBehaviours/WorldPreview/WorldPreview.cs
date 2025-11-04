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

using System.Collections.Generic;
using System.IO;
using FixMath.NET;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XDay.API;
using XDay.CameraAPI;
using XDay.RenderingAPI;
using XDay.UtilityAPI;
using XDay.WorldAPI;
using XDay.WorldAPI.Decoration;

internal partial class WorldPreview : MonoBehaviour
{
    public Vector3 CameraPosition;
    public Camera Camera;
    public float MoveOffset = 10;
    public float Radius = 5;
    public string AssetPath;
    public GameObject FollowTarget;

    private async void Start()
    {
        Application.runInBackground = true;

        m_XDay = IXDayContext.Create(EditorHelper.QueryAssetFilePath<WorldSetupManager>(), new EditorWorldAssetLoader(), true, true);
        var world = await m_XDay.WorldManager.LoadWorldAsync("", () => Camera);
        //world.CameraVisibleAreaCalculator.ExpandSize = new Vector2(50, 50);
        world.CameraManipulator.SetPosition(CameraPosition);

        Fix64 f = (Fix64)12.1;
        Fix64 f2 = (Fix64)12.1;
        var k = f - f2;
        var p = Fix64.Pow(f, (Fix64)3.2f);
        int a = 1;
    }

    private void OnDestroy()
    {
        m_XDay.OnDestroy();
    }

    private void Update()
    {
        m_XDay.Update(Time.deltaTime);

        UpdateTests();
    }

    private void LateUpdate()
    {
        m_XDay.LateUpdate();
    }

    private void OnGUI()
    {
        if (m_Style == null)
        {
            m_Style = new GUIStyle(GUI.skin.label);
            m_Style.fontSize = 50;
        }

        if (m_XDay.WorldManager != null)
        {
            var world = m_XDay.WorldManager.FirstWorld;
            if (world != null)
            {
                GUILayout.Label($"地图大小: {world.Bounds.size}", m_Style);
                Camera camera = null;
                if (world.CameraManipulator != null)
                {
                    camera = world.CameraManipulator.Camera;
                }
                else
                {
                    camera = Camera.main;
                }
                GUILayout.Label($"相机高度: {camera.transform.position.y}", m_Style);
                foreach (var plugin in world.QueryPlugins<WorldPlugin>())
                {
                    if (plugin.LODSystem != null)
                    {
                        GUILayout.Label($"{plugin.Name} LOD: {plugin.LODSystem.CurrentLOD}", m_Style);
                    }
                }
            }
        }
    }

    private void UpdateTests()
    {
        //MoveCamera();
        //DecorationSystemTest();
        //AddressableTest();
        //UpdateFollowTargetTest();
        m_StripeEffect?.Update();
    }

    private void UpdateFollowTargetTest()
    {
        if (FollowTarget != null)
        {
            FollowTarget.transform.position += new Vector3(1, 1, 0) * Time.deltaTime;
        }
    }

    private void MoveCamera()
    {
        if (m_XDay.WorldManager == null)
        {
            return;
        }

        var world = m_XDay.WorldManager.FirstWorld;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (world == null)
            {
                m_XDay.WorldManager.LoadWorld("");
            }
            else
            {
                m_XDay.WorldManager.UnloadWorld(world.Name);
            }
        }

        if (world == null)
        {
            return;
        }

        var manipulator = world.CameraManipulator;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                manipulator.SetPosition(manipulator.RenderPosition + new Vector3(-MoveOffset, 0, 0));
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                manipulator.SetPosition(manipulator.RenderPosition + new Vector3(MoveOffset, 0, 0));
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                manipulator.SetPosition(manipulator.RenderPosition + new Vector3(0, 0, MoveOffset));
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                manipulator.SetPosition(manipulator.RenderPosition + new Vector3(0, 0, -MoveOffset));
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (manipulator.Direction == CameraDirection.XZ)
            {
                var param = new FocusParam(new Vector3(50, 0, 100), 40);
                manipulator.Focus(param);
            }
            else
            {
                var param = new FocusParam(new Vector3(50, 100, 0), 40);
                param.MovementType = FocusMovementType.HorizontalAndVertical;
                param.ScreenPosition = new Vector2(0, 0);
                manipulator.Focus(param);
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            var followParam = new FollowParam(new FollowGameObject(FollowTarget));
            manipulator.FollowTarget(followParam);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            manipulator.Shake(2, 10, 0.2f, true);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            manipulator.SetPosition(new Vector3(110, 30, 120));
        }
    }

    private void DecorationSystemTest()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var world = m_XDay.WorldManager.FirstWorld;
            if (world != null)
            {
                var pos = Helper.RayCastWithXZPlane(Input.mousePosition, world.CameraManipulator.Camera);
                var decorationSystem = world.QueryPlugin<IDecorationSystem>();
                if (decorationSystem != null)
                {
                    List<int> decorationIDs = new();
                    decorationSystem.QueryDecorationIDsInCircle(pos, Radius, decorationIDs, DecorationTagType.All);
                    var circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    circle.transform.localScale = Vector3.one * Radius * 2;
                    circle.transform.position = pos;
                    foreach (var id in decorationIDs)
                    {
                        //decorationSystem.ShowDecoration(id, false);
                        decorationSystem.PlayAnimation(id, "Drunk Walk");
                    }
                }
            }
        }
    }

    private async void AddressableTest()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            var inst = await m_XDay.WorldAssetLoader.LoadGameObjectAsync(AssetPath);
            inst.name += "_addressable";
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            m_XDay.WorldAssetLoader.UnloadAsset(AssetPath);
        }
    }

    [MenuItem("XDay/地图/预览地图", false, 1)]
    static void Open()
    {
        var sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
        if (sceneGUIDs.Length > 0)
        {
            foreach (var guid in sceneGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileName(path);
                if (name == "WorldPreview.unity")
                {
                    EditorSceneManager.OpenScene(path);
                }
            }
        }
    }

    private GUIStyle m_Style;
    private IXDayContext m_XDay;
    private StripeEffect m_StripeEffect;
}

class FollowGameObject : IFollowTarget
{
    public bool IsValid => true;
    public Vector3 Position => m_Target.transform.position;

    public FollowGameObject(GameObject target)
    {
        m_Target = target;
    }

    public void OnStopFollow()
    {
    }

    private GameObject m_Target;
}

#endif