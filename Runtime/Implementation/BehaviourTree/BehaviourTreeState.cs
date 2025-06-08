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
using System.Reflection;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.BehaviourTreeAPI
{
    [System.Serializable]
    public class VariableState
    {
        public string FieldName;
        public int VariableID;
        public VariableScope Scope;
    }

    [System.Serializable]
    public abstract class NodeState
    {
        public int ID;
        public int ParentID;
        public int SortOrder;
        public string Name;
        public string Comment;
        public bool HasBreakpoint;
        //编辑器显示
        public Vector2 Position;
        //自动填充
        public List<VariableState> Variables = new();

        public NodeState(Node node)
        {
            Variables = GetAllVariableFields(node);
        }

        public Node CreateNode(BehaviourTree tree)
        {
            var node = DoCreateNode(tree);
            node.SortOrder = SortOrder;
            node.Comment = Comment;
            node.HasBreakpoint = HasBreakpoint;
            SetAllVariableFields(node, tree);
            return node;
        }

        private List<VariableState> GetAllVariableFields(Node node)
        {
            List<VariableState> variables = new();
            var fields = node.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var field in fields)
            {
                if (typeof(IVariable).IsAssignableFrom(field.FieldType))
                {
                    var variable = field.GetValue(node) as IVariable;
                    var scope = GetVariableScope(field);

                    VariableState vs = new VariableState()
                    {
                        FieldName = field.Name,
                        Scope = scope,
                        VariableID = variable == null ? 0 : variable.ID,
                    };
                    variables.Add(vs);
                }
            }
            return variables;
        }

        private void SetAllVariableFields(Node node, BehaviourTree tree)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            foreach (var state in Variables)
            {
                FieldInfo field = node.GetType().GetField(state.FieldName, flags);
                if (field != null)
                {
                    if (typeof(IVariable).IsAssignableFrom(field.FieldType))
                    {
                        var scope = GetVariableScope(field);
                        if (scope == state.Scope)
                        {
                            VariableManager variableManager = tree.VariableManager;
                            if (state.Scope == VariableScope.Global)
                            {
                                variableManager = Global.GlobalVariableManager;
                            }
                            var variable = variableManager.GetVariableByID(state.VariableID);
                            if (variable != null)
                            {
                                field.SetValue(node, variable);
                            }
                        }
                        else
                        {
                            field.SetValue(node, null);
                        }
                    }
                }
            }
        }

        private VariableScope GetVariableScope(FieldInfo field)
        {
            var scopeAttribute = field.GetCustomAttribute<VariableScopeAttribute>();
            VariableScope scope = VariableScope.Local;
            if (scopeAttribute != null)
            {
                scope = scopeAttribute.Scope;
            }
            return scope;
        }

        protected abstract Node DoCreateNode(BehaviourTree tree);
    }

    public class BehaviourTreeState : ScriptableObject
    {
        public void Assign(BehaviourTreeState state)
        {
            RootID = state.RootID;
            NodesState = state.NodesState;
            Zoom = state.Zoom;
            ViewPosition = state.ViewPosition;
            VariableDatas = state.VariableDatas;
        }

        public BehaviourTree CreateTree(int id, string name)
        {
            var variableManagerState = ScriptableObject.CreateInstance<VariableManagerState>();
            variableManagerState.Variables = VariableDatas;
            var tree = new BehaviourTree(id, name, this, variableManagerState.CreateVariableManager());
            Helper.DestroyUnityObject(variableManagerState);
            List<Node> nodes = new();
            foreach (var nodeState in NodesState)
            {
                var node = nodeState?.CreateNode(tree);
                if (node != null)
                {
                    nodes.Add(node);
                }
            }

            for (var i = 0; i < nodes.Count; ++i)
            {
                var nodeState = NodesState[i];
                if (nodeState != null)
                {
                    nodes[i].SetParent(GetNode(nodes, nodeState.ParentID) as CompoundNode);
                    tree.AddNode(nodes[i]);
                }
            }

            tree.SetRoot(GetNode(nodes, RootID));

            return tree;
        }

        public NodeState GetNodeState(int id)
        {
            foreach (var node in NodesState)
            {
                if (node.ID == id)
                {
                    return node;
                }
            }
            return null;
        }

        private Node GetNode(List<Node> nodes, int id)
        {
            foreach (var node in nodes)
            {
                if (node.ID == id) return node;
            }
            return null;
        }

        public int RootID = 0;
        [SerializeReference]
        public List<NodeState> NodesState = new();
        public VariableDatas VariableDatas = new();
        //编辑器显示
        public float Zoom = 1;
        public Vector2 ViewPosition;
    }
}