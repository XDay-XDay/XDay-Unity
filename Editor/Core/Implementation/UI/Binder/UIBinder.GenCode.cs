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

using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using XDay.SerializationAPI.Editor;
using XDay.UtilityAPI;

namespace XDay.GUIAPI.Editor
{
    internal partial class UIBinder
    {
        private void Generate()
        {
            RemoveInvalidateData();

            var err = Validate();
            if (!string.IsNullOrEmpty(err))
            {
                Debug.LogError(err);
                return;
            }

            var fileName = $"{m_Config.CodeOutputPath}/{m_Metadata.ViewClassName}.cs";
            Helper.CreateDirectory(m_Config.CodeOutputPath);

            string viewContent = GenerateView();
            string controllerContent = GenerateController();

            File.WriteAllText(fileName, viewContent + controllerContent);
            AssetDatabase.Refresh();

            SerializationHelper.FormatCode(fileName);
        }

        private string GenerateView()
        {
            string template =
@"

//this file is auto generated from UIBinder

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using XDay.GUIAPI;

$NAMESPACE_BEGIN$

    $EVENT_HANDLER_INTERFACE$

    internal class $CLASS_NAME$ : UIView
    {
        $PROPERTY$

        public $CLASS_NAME$()
        {
        }

        public $CLASS_NAME$(GameObject root) : base(root)
        {
        }

        public override string GetPath()
        {
            return ""$PREFAB_PATH$"";
        }

        protected override void OnLoad()
        {
            $BIND_EVENTS$

            $BIND_VARIABLES$
        }

        protected override void OnShow()
        {
            $SHOW$
        }

        protected override void OnHide()
        {
            $HIDE$
        }

        protected override void OnUpdate(float dt)
        {
            $UPDATE$
        }

        protected override void OnDestroyInternal() 
        {
            $DESTROY$
        }
        
        $FIELD$

        $EVENT_HANDLER$
    }

";
            template = template.Replace("$CLASS_NAME$", m_Metadata.ViewClassName);
            template = template.Replace("$PROPERTY$", GenerateProperty());
            template = template.Replace("$BIND_VARIABLES$", GenerateVariableBinding());
            template = template.Replace("$BIND_EVENTS$", GenerateEventsBinding());
            template = template.Replace("$FIELD$", GenerateField());
            template = template.Replace("$PREFAB_PATH$", GeneratePrefabPath());
            template = template.Replace("$DESTROY$", GenerateFunc("OnDestroy"));
            template = template.Replace("$SHOW$", GenerateFunc("Show"));
            template = template.Replace("$HIDE$", GenerateFunc("Hide"));
            template = template.Replace("$UPDATE$", GenerateUpdateFunc());
            template = template.Replace("$EVENT_HANDLER_INTERFACE$", GenerateEventHandlerInterface());
            template = template.Replace("$EVENT_HANDLER$", GenerateEventHandler());

            if (!string.IsNullOrEmpty(m_Metadata.Namespace))
            {
                template = template.Replace("$NAMESPACE_BEGIN$", $"namespace {m_Metadata.Namespace} {{");
            }
            else
            {
                template = template.Replace("$NAMESPACE_BEGIN$", "");
            }
            return template;
        }

        private string GenerateController()
        {
            string template =
@"

    internal partial class $CONTROLLER_CLASS_NAME$ : UIController<$VIEW_CLASS_NAME$> $EVENT_HANDLER_INTERFACE$
    {
        public $CONTROLLER_CLASS_NAME$($VIEW_CLASS_NAME$ view) : base(view)
        {
        }
    }
$NAMESPACE_END$
";
            template = template.Replace("$CONTROLLER_CLASS_NAME$", m_Metadata.ControllerClassName);
            template = template.Replace("$VIEW_CLASS_NAME$", m_Metadata.ViewClassName);
            template = template.Replace("$EVENT_HANDLER_INTERFACE$", GenerateControllerEventHandlerInterface());
            if (!string.IsNullOrEmpty(m_Metadata.Namespace))
            {
                template = template.Replace("$NAMESPACE_END$", "}");
            }
            else
            {
                template = template.Replace("$NAMESPACE_END$", "");
            }
            return template;
        }

        private string GenerateProperty()
        {
            StringBuilder builder = new();
            foreach (var gameObjectMetadata in m_Metadata.GameObjects)
            {
                foreach (var componentMetadata in gameObjectMetadata.Components)
                {
                    builder.AppendLine($"public {componentMetadata.TypeName} {componentMetadata.VariableName} => m_{componentMetadata.VariableName};");
                }

                if (gameObjectMetadata.BindGameObject)
                {
                    builder.AppendLine($"public GameObject {gameObjectMetadata.GameObjectVariableName} => m_{gameObjectMetadata.GameObjectVariableName};");
                }

                if (gameObjectMetadata.BindView)
                {
                    builder.AppendLine($"public {gameObjectMetadata.ViewClassName} {gameObjectMetadata.ViewVariableName} => m_{gameObjectMetadata.ViewVariableName};");
                }
            }

            builder.AppendLine($"public override UIWindowLayer? Layer => UIWindowLayer.{m_Metadata.WindowLayer};");

            return builder.ToString();
        }

        private string GenerateField()
        {
            StringBuilder builder = new();
            foreach (var gameObjectMetadata in m_Metadata.GameObjects)
            {
                foreach (var componentMetadata in gameObjectMetadata.Components)
                {
                    builder.AppendLine($"private {componentMetadata.TypeName} m_{componentMetadata.VariableName};");
                }

                if (gameObjectMetadata.BindGameObject)
                {
                    builder.AppendLine($"private GameObject m_{gameObjectMetadata.GameObjectVariableName};");
                }

                if (gameObjectMetadata.BindView)
                {
                    builder.AppendLine($"private {gameObjectMetadata.ViewClassName} m_{gameObjectMetadata.ViewVariableName};");
                }
            }
            return builder.ToString();
        }

        private string GenerateVariableBinding()
        {
            StringBuilder builder = new();
            foreach (var gameObjectMetadata in m_Metadata.GameObjects)
            {
                foreach (var componentMetadata in gameObjectMetadata.Components)
                {
                    GetComponentPathInHierarchy(componentMetadata.localID, out var path, out var index);
                    builder.AppendLine($"m_{componentMetadata.VariableName} = QueryComponent<{componentMetadata.TypeName}>(\"{path}\", {index});");
                    builder.AppendLine($"Debug.Assert(m_{componentMetadata.VariableName} != null, \"m_{componentMetadata.VariableName} is null\");");
                }

                if (gameObjectMetadata.BindGameObject)
                {
                    var path = GetGameObjectPathInHierarchy(gameObjectMetadata.localID);
                    builder.AppendLine($"m_{gameObjectMetadata.GameObjectVariableName} = QueryGameObject(\"{path}\");");
                    builder.AppendLine($"Debug.Assert(m_{gameObjectMetadata.GameObjectVariableName} != null, \"m_{gameObjectMetadata.GameObjectVariableName} is null\");");
                }

                if (gameObjectMetadata.BindView)
                {
                    var controllerClassName = GetControllerClassName(gameObjectMetadata.ViewClassName);
                    var path = GetGameObjectPathInHierarchy(gameObjectMetadata.localID);
                    builder.AppendLine($"m_{gameObjectMetadata.ViewVariableName} = new {gameObjectMetadata.ViewClassName}(QueryGameObject(\"{path}\"));");
                    builder.AppendLine($"m_{gameObjectMetadata.ViewVariableName}.SetController(new {controllerClassName}(m_{gameObjectMetadata.ViewVariableName}));");
                }
            }
            return builder.ToString();
        }

        private string GenerateEventsBinding()
        {
            int index = 0;
            StringBuilder builder = new();
            foreach (var gameObjectMetadata in m_Metadata.GameObjects)
            {
                for (var i = 0; i < gameObjectMetadata.Events.Count; ++i)
                {
                    var eventData = gameObjectMetadata.Events[i];
                    if (i == 0)
                    {
                        var path = GetGameObjectPathInHierarchy(gameObjectMetadata.localID);
                        builder.AppendLine($"var gameObject{index} = QueryGameObject(\"{path}\");");
                        builder.AppendLine($"var gameObject{index}Listener = gameObject{index}.AddComponent<UIEventListener>();");
                    }
                    builder.AppendLine($"gameObject{index}Listener.Add{eventData.Type}Event({eventData.HandlerName});");
                }

                if (gameObjectMetadata.Events.Count > 0)
                {
                    ++index;
                }
            }
            return builder.ToString();
        }

        private string GetControllerClassName(string viewClassName)
        {
            foreach (var metadata in m_MetadataManager.UIMetadatas)
            {
                if (metadata.ViewClassName == viewClassName)
                {
                    return metadata.ControllerClassName;
                }
            }
            Debug.Assert(false);
            return "";
        }

        private string GetGameObjectPathInHierarchy(long localID)
        {
            var prefab = EditorHelper.GetEditingPrefab();
            string path = null;
            Helper.Traverse(prefab.transform, false, (transform) =>
            {
                if (EditorHelper.GetObjectLocalID(transform.gameObject) == localID)
                {
                    if (Helper.IsRoot(transform))
                    {
                        //is root
                        path = "";
                    }
                    else
                    {
                        path = transform.gameObject.GetPathInHierarchy(false);
                    }
                }
            });

            return path;
        }

        private void GetComponentPathInHierarchy(long localID, out string path, out int index)
        {
            var prefab = EditorHelper.GetEditingPrefab();
            path = null;
            index = -1;
            string tempPath = null;
            int tempIndex = -1;
            Helper.Traverse(prefab.transform, false, (transform) =>
            {
                var components = transform.GetComponents<Component>();
                for (var i = 0; i < components.Length; ++i)
                {
                    if (EditorHelper.GetObjectLocalID(components[i]) == localID)
                    {
                        if (Helper.IsRoot(components[i].transform))
                        {
                            tempPath = "";
                        }
                        else
                        {
                            tempPath = components[i].gameObject.GetPathInHierarchy(false);
                        }
                        tempIndex = i;
                    }
                }
            });

            path = tempPath;
            index = tempIndex;
        }

        private string GeneratePrefabPath()
        {
            return AssetDatabase.GUIDToAssetPath(m_Metadata.PrefabGUID);
        }

        private string GenerateFunc(string funcName)
        {
            StringBuilder builder = new();
            foreach (var gameObjectMetadata in m_Metadata.GameObjects)
            {
                if (gameObjectMetadata.BindView)
                {
                    builder.AppendLine($"m_{gameObjectMetadata.ViewVariableName}?.{funcName}();");
                }
            }
            return builder.ToString();
        }

        private string GenerateUpdateFunc()
        {
            StringBuilder builder = new();
            foreach (var gameObjectMetadata in m_Metadata.GameObjects)
            {
                if (gameObjectMetadata.BindView)
                {
                    builder.AppendLine($"m_{gameObjectMetadata.ViewVariableName}.Controller?.Update(dt);");
                }
            }
            return builder.ToString();
        }

        private string GenerateEventHandlerInterface()
        {
            StringBuilder builder = new();
            foreach (var gameObjectMetadata in m_Metadata.GameObjects)
            {
                foreach (var e in gameObjectMetadata.Events)
                {
                    builder.AppendLine($"void {e.HandlerName}(PointerEventData pointerData);");
                }
            }

            if (builder.Length > 0)
            {
                string template =
@"
public interface I$CLASS_NAME$EventHandler
{
    $FUNC$
}
";
                template = template.Replace("$CLASS_NAME$", m_Metadata.ViewClassName);
                template = template.Replace("$FUNC$", builder.ToString());
                return template;
            }

            return "";
        }

        private string GenerateControllerEventHandlerInterface()
        {
            bool hasEvents = false;
            foreach (var gameObjectMetadata in m_Metadata.GameObjects)
            {
                foreach (var e in gameObjectMetadata.Events)
                {
                    hasEvents = true;
                    goto label;
                }
            }
        label:
            if (hasEvents)
            {
                return $", I{m_Metadata.ViewClassName}EventHandler";
            }
            return "";
        }

        private string GenerateEventHandler()
        {
            StringBuilder builder = new();
            foreach (var gameObjectMetadata in m_Metadata.GameObjects)
            {
                foreach (var e in gameObjectMetadata.Events)
                {
                    string template =
@"
private void $HANDLER_NAME$(PointerEventData pointerData)
{
    var eventHandler = m_Controller as I$CLASS_NAME$EventHandler;
    eventHandler.$HANDLER_NAME$(pointerData);
}
";
                    template = template.Replace("$HANDLER_NAME$", e.HandlerName);
                    template = template.Replace("$CLASS_NAME$", m_Metadata.ViewClassName);
                    builder.AppendLine(template);
                }
            }

            return builder.ToString();
        }

        private string GenerateControllerEventHandler()
        {
            StringBuilder builder = new();
            foreach (var gameObjectMetadata in m_Metadata.GameObjects)
            {
                foreach (var e in gameObjectMetadata.Events)
                {
                    string template =
@"
public void $HANDLER_NAME$(PointerEventData pointerData)
{
    //custom handler code here
}
";
                    template = template.Replace("$HANDLER_NAME$", e.HandlerName);
                    builder.AppendLine(template);
                }
            }

            return builder.ToString();
        }
    }
}



