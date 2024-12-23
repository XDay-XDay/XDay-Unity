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

using System;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI
{
    public enum UndoActionType
    {
        Custom,
        ChangeAspect,
        CreateObject,
        DestroyObject,
    }

    public struct UndoActionGroup
    {
        public long JoinID => m_JoinID;
        public long ID => m_ID;

        public void NextJoinID()
        {
            ++m_JoinID;
        }

        public void NextGroupID()
        {
            ++m_ID;
        }

        public void Next()
        {
            ++m_ID;
            ++m_JoinID;
        }

        public bool CanJoin(UndoActionGroup other)
        {
            return m_JoinID == other.m_JoinID;
        }

        public static bool operator == (UndoActionGroup a, UndoActionGroup b)
        {
            return a.m_JoinID == b.m_JoinID &&
                a.m_ID == b.m_ID;
        }

        public static bool operator != (UndoActionGroup p1, UndoActionGroup p2)
        {
            return !(p1 == p2);
        }

        public override bool Equals(object obj)
        {
            if (obj is UndoActionGroup)
            {
                var other = (UndoActionGroup)obj;
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(m_JoinID, m_ID);
        }

        private long m_ID;
        private long m_JoinID;
    }

    public abstract class UndoAction
    {
        public UndoActionGroup Group => m_Group;
        public string DisplayName => m_DisplayName;
        public abstract int Size { get; }
        public abstract UndoActionType Type { get; }

        public UndoAction(string displayName, UndoActionGroup group)
        {
            m_DisplayName = displayName;
            m_Group = group;
        }

        private string m_DisplayName;
        protected UndoActionGroup m_Group = new();
    }

    public abstract class CustomUndoAction : UndoAction
    {
        public override UndoActionType Type => UndoActionType.Custom;
        public abstract bool CanJoin { get; }

        public CustomUndoAction(string displayName, UndoActionGroup group)
            : base(displayName, group)
        {
        }

        public bool Join(CustomUndoAction action)
        {
            if (m_Group.CanJoin(action.Group) &&
                DisplayName == action.DisplayName)
            {
                return JoinInternal(action);
            }
            return false;
        }

        public abstract bool Redo();
        public abstract bool Undo();
        protected abstract bool JoinInternal(CustomUndoAction action);
    }

    internal abstract class UndoActionBase : UndoAction
    {
        public int WorldID => m_WorldID;
        public int ObjectID => m_ObjectID;

        public UndoActionBase(string displayName,
            UndoActionGroup group,
            int objectID,
            int worldID)
            : base(displayName, group)
        {
            m_WorldID = worldID;
            m_ObjectID = objectID;
        }

        private int m_WorldID;
        private int m_ObjectID;
    }

    internal class UndoActionObjectFactory : UndoActionBase
    {
        public int LOD => m_LOD;
        public byte[] ObjectData => m_Bytes;
        public int RelayID => m_RelayID;
        public int ObjectIndex => m_ObjectIndex;
        public string TypeName => m_TypeName;
        public override int Size => m_Bytes.Length;
        public override UndoActionType Type => m_Type;

        public UndoActionObjectFactory(string displayName,
            UndoActionGroup group,
            UndoActionType type,
            int objectID,
            int worldID,
            int lod,
            int objectIndex,
            string objectTypeName,
            byte[] objectData,
            int relayID)
            : base(displayName, group, objectID, worldID)
        {
            m_Type = type;
            m_Bytes = objectData;
            m_RelayID = relayID;
            m_TypeName = objectTypeName;
            m_LOD = lod;
            m_ObjectIndex = objectIndex;
        }

        private UndoActionType m_Type;
        private int m_ObjectIndex;
        private int m_RelayID;
        private int m_LOD;
        private byte[] m_Bytes;
        private string m_TypeName;
    }

    internal class UndoActionAspect : UndoActionBase
    {
        public int RelayID => m_RelayID;
        public string AspectName => m_AspectName;
        public IAspect OldAspect => m_OldAspect;
        public IAspect NewAspect => m_NewAspect;
        public override int Size
        {
            get
            {
                var newSize = m_NewAspect != null ? m_NewAspect.Size : 0;
                var oldSize = m_OldAspect != null ? m_OldAspect.Size : 0;
                return newSize + oldSize;
            }
        }
        public override UndoActionType Type => UndoActionType.ChangeAspect;

        public UndoActionAspect(
            string displayName, 
            UndoActionGroup group,
            int objectID,
            string aspectName,
            IAspect oldAspect,
            IAspect newAspect,
            int worldID,
            int relayID)
            : base(displayName, group, objectID, worldID)
        {
            m_OldAspect = oldAspect;
            m_NewAspect = newAspect;
            m_RelayID = relayID;
            m_AspectName = aspectName;
        }

        public bool Join(UndoActionAspect action)
        {
            if (CanJoin(action))
            {
                Debug.Log($"Action {action.DisplayName}@{action.m_AspectName} joined!");
                m_NewAspect = action.m_NewAspect;
                return true;
            }
            return false;
        }

        private bool CanJoin(UndoActionAspect action)
        {
            return
                m_Group.CanJoin(action.Group) &&
                DisplayName == action.DisplayName &&
                WorldID == action.WorldID &&
                m_AspectName == action.m_AspectName &&
                ObjectID == action.ObjectID;
        }

        private string m_AspectName;
        private int m_RelayID;
        private IAspect m_OldAspect;
        private IAspect m_NewAspect;
    }
}

//XDay