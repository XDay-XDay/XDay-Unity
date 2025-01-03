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
using System;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Editor
{
    public enum UndoActionJoinMode
    {
        None = 0,
        NextGroup = 1,
        NextJoin = 2,
        Both = 3,
    }

    public delegate IWorldObjectContainer DelegateQueryRelay(int worldID, int containerID);
    public delegate IWorldObject DelegateQueryObject(int worldID, int objectID);

    public static class UndoSystem
    { 
        public static string LastActionName => m_Pointer >= 0 ? m_Actions[m_Pointer].DisplayName : null;
        public static UndoActionGroup Group => m_Group;

        internal static void Init(
            UndoSerializer objectSeralizer,
            DelegateQueryObject queryObject,
            DelegateQueryRelay queryRelay)
        {
            m_Serializer = objectSeralizer;
            m_QueryObject = queryObject;
            m_QueryRelay = queryRelay;
        }

        public static void Clear()
        {
            m_Actions = new();
            m_Group = new();
            SlidePointer(-1);
        }

        public static void NextGroupAndJoin()
        {
            m_Group.Next();
        }

        public static void NextGroup()
        {
            m_Group.NextGroupID();
        }

        public static void NextJoin()
        {
            m_Group.NextJoinID();
        }

        public static void SetAspect(
            IWorldObject obj, 
            string aspectName, 
            IAspect newAspect, 
            string displayName,
            int relayID,
            UndoActionJoinMode joinMode)
        {
            if (obj == null)
            {
                Debug.LogError($"Set aspect {aspectName} failed!");
                return;
            }

            if (joinMode == UndoActionJoinMode.NextJoin)
            {
                m_Group.NextJoinID();
            }
            else if (joinMode == UndoActionJoinMode.NextGroup)
            {
                m_Group.NextGroupID();
            }
            else if (joinMode == UndoActionJoinMode.Both)
            {
                m_Group.Next();
            }

            var oldAspect = GetAspect(relayID, obj, aspectName);
            if (newAspect != oldAspect)
            {
                var action = new UndoActionAspect(displayName, m_Group, obj.ID, aspectName, oldAspect, newAspect, obj.WorldID, relayID);

                Queue(action);
                Perform(action, true);
            }
        }

        public static void PerformCustomAction(CustomUndoAction action, bool perform)
        {
            if (action != null)
            {
                Queue(action);

                if (perform)
                {
                    Perform(action, true);
                }
            }
        }

        public static void CreateObject(IWorldObject obj, int worldID, string actionName, int relayID = 0, int lod = 0)
        {
            Debug.Assert(obj != null);
            ObjectFactory(obj, worldID, actionName, relayID, lod, true);
        }

        public static void DestroyObject(IWorldObject obj, string actionName, int relayID = 0, int lod = 0)
        {
            if (obj == null)
            {
                return;
            }
            ObjectFactory(obj, obj.WorldID, actionName, relayID, lod, false);
        }

        public static void Redo()
        {
            Apply(true);
        }

        public static void Undo()
        {
            Apply(false);
        }

        internal static int Size()
        {
            var size = 0;
            for (var i = 0; i < m_Actions.Count; ++i)
            {
                size += m_Actions[i].Size;
            }
            return size;
        }

        internal static void AddUndoRedoCallback(Action<UndoAction, bool> callback)
        {
            EventUndoRedo -= callback;
            EventUndoRedo += callback;
        }

        internal static void RemoveUndoRedoCallback(Action<UndoAction, bool> callback)
        {
            EventUndoRedo -= callback;
        }

        private static bool Join(UndoActionAspect action)
        {
            for (var i = m_Actions.Count - 2; i >= 0; --i)
            {
                if (m_Actions[i] is UndoActionAspect aspectAction)
                {
                    if (aspectAction.Join(action))
                    {
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
            return false;
        }

        private static bool JoinCustomAction(CustomUndoAction action)
        {
            if (!action.CanJoin)
            {
                return false;
            }

            for (var i = m_Actions.Count - 2; i >= 0; --i)
            {
                if (m_Actions[i] is CustomUndoAction userAction)
                {
                    if (userAction.Join(action))
                    {
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
            return false;
        }

        private static void Queue(UndoAction action)
        {
            var expiredCount = m_Actions.Count - m_Pointer - 1;
            EventActionDestroyed?.Invoke(m_Pointer + 1, expiredCount);
            m_Actions.RemoveRange(m_Pointer + 1, expiredCount);

            m_Actions.Add(action);
            SlidePointer(m_Pointer + 1);

            var actionIsJoined = false;
            if (expiredCount == 0 && 
                m_Actions.Count > 1)
            {
                if (action is CustomUndoAction customAction)
                {
                    actionIsJoined = JoinCustomAction(customAction);
                }
                else if (action is UndoActionAspect aspectAction)
                {
                    actionIsJoined = Join(aspectAction);
                }

                if (actionIsJoined)
                {
                    m_Actions.RemoveAt(m_Actions.Count - 1);
                    SlidePointer(m_Pointer - 1);
                }
            }

            if (!actionIsJoined)
            {
                EventActionAdded?.Invoke(m_Actions.Count - 1);
            }
        }

        private static bool PerformChangeAspect(UndoActionAspect action, bool redo)
        {
            if (action.RelayID == 0)
            {
                var obj = m_QueryObject(action.WorldID, action.ObjectID) as WorldObject;
                obj.SetAspect(0, action.AspectName, redo ? action.NewAspect : action.OldAspect);
            }
            else
            {
                var relay = m_QueryObject(action.WorldID, action.RelayID) as WorldObject;
                relay.SetAspect(action.ObjectID, action.AspectName, redo ? action.NewAspect : action.OldAspect);
            }
            return true;
        }

        private static bool PerformCustomActionInternal(CustomUndoAction action, bool redo)
        {
            return redo ? action.Redo() : action.Undo();
        }

        private static bool PerformDestroy(UndoActionObjectFactory action, bool redo)
        {
            if (redo)
            {
                var relay = m_QueryRelay(action.WorldID, action.RelayID);
                relay.DestroyObjectUndo(action.ObjectID);
            }
            else
            {
                var relay = m_QueryRelay(action.WorldID, action.RelayID);
                relay.AddObjectUndo(Deserialize(action), action.LOD, action.ObjectIndex);
            }
            return true;
        }

        private static bool PerformCreate(UndoActionObjectFactory action, bool redo)
        {
            if (redo)
            {
                var relay = m_QueryRelay(action.WorldID, action.RelayID);
                relay.AddObjectUndo(Deserialize(action), action.LOD, action.ObjectIndex);
            }
            else
            {
                var relay = m_QueryRelay(action.WorldID, action.RelayID);
                relay.DestroyObjectUndo(action.ObjectID);
            }
            return true;
        }

        private static bool Perform(UndoAction action, bool redo)
        {
            if (action.Type == UndoActionType.Custom)
            {
                return PerformCustomActionInternal(action as CustomUndoAction, redo);
            }

            if (action.Type == UndoActionType.ChangeAspect)
            {
                return PerformChangeAspect(action as UndoActionAspect, redo);
            }

            if (action.Type == UndoActionType.CreateObject)
            {
                return PerformCreate(action as UndoActionObjectFactory, redo);
            }

            if (action.Type == UndoActionType.DestroyObject)
            {
                return PerformDestroy(action as UndoActionObjectFactory, redo);
            }

            Debug.Assert(false, $"Unknown action type: {action.GetType()}");
            return false;
        }

        private static byte[] Serialize(IWorldObject obj)
        {
            return m_Serializer.Serialize(obj);
        }

        private static IWorldObject Deserialize(UndoActionObjectFactory action)
        {
            return m_Serializer.Deserialize(action.WorldID, action.TypeName, action.ObjectData);
        }

        private static IAspect GetAspect(int relayID, IWorldObject obj, string name)
        {
            if (relayID != 0)
            {
                var relay = m_QueryObject(obj.WorldID, relayID) as WorldObject;
                return relay.GetAspect(obj.ID, name);
            }
            return (obj as WorldObject).GetAspect(0, name);
        }

        private static void SlidePointer(int pointer)
        {
            if (m_Pointer != pointer)
            {
                m_Pointer = pointer;
                EventActionPointerChanged?.Invoke(pointer);
            }
        }

        private static void ObjectFactory(IWorldObject obj, int worldID, string actionName, int relayID, int lod, bool create)
        {
            var data = Serialize(obj);
            var action = new UndoActionObjectFactory(
                actionName,
                m_Group,
                create ? UndoActionType.CreateObject : UndoActionType.DestroyObject,
                obj.ID,
                worldID,
                lod,
                obj.ObjectIndex,
                obj.GetType().AssemblyQualifiedName,
                data,
                relayID);

            Queue(action);
            Perform(action, true);
        }

        private static void Apply(bool redo)
        {
            long groupID = -1;
            while (true)
            {
                var pointer = m_Pointer + (redo ? 1 : 0);
                if (pointer >= 0 && pointer < m_Actions.Count)
                {
                    var action = m_Actions[pointer];
                    if (groupID == -1)
                    {
                        groupID = action.Group.ID;
                    }
                    else if (groupID != action.Group.ID)
                    {
                        break;
                    }

                    if (Perform(action, redo))
                    {
                        EventUndoRedo?.Invoke(action, redo);
                        SlidePointer(pointer - (redo ? 0 : 1));
                    }
                }
                else
                {
                    break;
                }
            }
        }

        internal delegate void DelegateActionAdded(int index);
        internal delegate void DelegateActionDestroyed(int startIndex, int count);
        internal delegate void DelegateActionPointerChanged(int newIndex);
        internal static event DelegateActionAdded EventActionAdded;
        internal static event DelegateActionDestroyed EventActionDestroyed;
        internal static event DelegateActionPointerChanged EventActionPointerChanged;
        internal static int Pointer => m_Pointer;
        internal static List<UndoAction> Actions => m_Actions;

        private static event Action<UndoAction, bool> EventUndoRedo;
        private static UndoActionGroup m_Group;
        private static int m_Pointer = -1;
        private static List<UndoAction> m_Actions = new();
        private static UndoSerializer m_Serializer;
        private static DelegateQueryObject m_QueryObject;
        private static DelegateQueryRelay m_QueryRelay;
    }
}

//XDay