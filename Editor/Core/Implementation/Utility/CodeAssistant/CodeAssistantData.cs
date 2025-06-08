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
using System.IO;
using UnityEngine;

namespace XDay.UtilityAPI.Editor.CodeAssistant
{
    [System.Flags]
    public enum SearchType
    {
        Type = 1,
        Method = 2,
        Property = 4,
        All = -1,
    }

    public enum SearchMode
    {
        MatchStart,
        MatchPart,
    }

    public enum DisplayMode
    {
        Detail,
        Icon,
    }

    public class CodeAssistantData
    {
        public void Load()
        {
            mAllTypes.Clear();
            mAllExternalTypes.Clear();
            mGroups.Clear();

            using (FileStream stream = new FileStream(mParseResultSavePath, FileMode.Open))
            {
                var reader = new BinaryReader(stream);

                Deserialize(reader);

                PostLoad();

                reader.Close();
            }
        }

        void Deserialize(BinaryReader reader)
        {
            int version = reader.ReadInt32();

            int allTypeCount = reader.ReadInt32();
            for (int i = 0; i < allTypeCount; ++i)
            {
                DeserializeTypePass1(reader);
            }
            for (int i = 0; i < allTypeCount; ++i)
            {
                DeserializeTypePass2(reader, mAllTypes[i]);
            }

            int groupCount = reader.ReadInt32();
            for (int i = 0; i < groupCount; ++i)
            {
                DeserializeGroup(reader);
            }
        }

        void DeserializeTypePass1(BinaryReader reader)
        {
            TypeInfo typeInfo = null;

            TypeName type = (TypeName)reader.ReadInt32();
            ulong hash = reader.ReadUInt64();
            string name = Utility.ReadString(reader);
            string namespaceName = Utility.ReadString(reader);
            string outerClassName = Utility.ReadString(reader);
            string fullNameWithoutNameSpaceName = Utility.ReadString(reader);
            string fullName = Utility.ReadString(reader);

            var nameInfo = new NameInfo(name, namespaceName, outerClassName, fullNameWithoutNameSpaceName, fullName);

            string filePath = Utility.ReadString(reader);
            string comment = Utility.ReadString(reader);

            int line = reader.ReadInt32();
            int column = reader.ReadInt32();
            TypeModifier modifier = (TypeModifier)reader.ReadInt32();
            Attribute attribute = (Attribute)reader.ReadInt32();
            AccessModifier accessModifier = (AccessModifier)reader.ReadInt32();
            bool isExternal = reader.ReadBoolean();

            if (type == TypeName.Class)
            {
                bool isAttributeClass = reader.ReadBoolean();
                typeInfo = new ClassInfo(attribute, line, column, comment, hash, nameInfo, filePath, modifier, accessModifier, isExternal, isAttributeClass);
            }
            else if (type == TypeName.Enum)
            {
                string[] members = Utility.ReadStringArray(reader);
                typeInfo = new EnumInfo(attribute, line, column, comment, hash, nameInfo, filePath, accessModifier, members);
            }
            else if (type == TypeName.Interface)
            {
                typeInfo = new InterfaceInfo(attribute, line, column, comment, hash, nameInfo, filePath, modifier, accessModifier, isExternal);
            }
            else if (type == TypeName.Struct)
            {
                typeInfo = new StructInfo(attribute, line, column, comment, hash, nameInfo, filePath, modifier, accessModifier, isExternal);
            }
            else
            {
                Debug.Assert(false);
            }

            mAllTypes.Add(typeInfo);
        }

        void DeserializeTypePass2(BinaryReader reader, TypeInfo type)
        {
            List<TypeInfo> baseTypes = DeserializeTypeReference(reader);
            foreach (var b in baseTypes)
            {
                type.AddBase(b);
            }

            var derivedTypes = DeserializeTypeReference(reader);
            foreach (var d in derivedTypes)
            {
                type.AddDerived(d);
            }

            var nestedTypes = DeserializeTypeReference(reader);
            foreach (var n in nestedTypes)
            {
                type.AddNest(n);
            }

            var attributes = DeserializeAttributes(reader);
            foreach (var a in attributes)
            {
                type.AddAttribute(a);
            }

            var methods = DeserializeMethods(reader);
            foreach (var m in methods)
            {
                type.AddMethod(m);
            }
            type.SortMethods();

            var properties = DeserializeProperties(reader);
            foreach (var p in properties)
            {
                type.AddProperty(p);
            }
        }

        List<TypeInfo> DeserializeTypeReference(BinaryReader reader)
        {
            int n = reader.ReadInt32();
            List<TypeInfo> ret = new List<TypeInfo>(n);
            for (int i = 0; i < n; ++i)
            {
                var type = FindType(Utility.ReadString(reader));
                Debug.Assert(type != null);
                ret.Add(type);
            }
            return ret;
        }

        List<AttributeInfo> DeserializeAttributes(BinaryReader reader)
        {
            int n = reader.ReadInt32();
            List<AttributeInfo> attributes = new List<AttributeInfo>(n);
            for (int i = 0; i < n; ++i)
            {
                string name = Utility.ReadString(reader);
                AttributeInfo info = new AttributeInfo(name);
                attributes.Add(info);
            }
            return attributes;
        }

        List<MethodInfo> DeserializeMethods(BinaryReader reader)
        {
            int n = reader.ReadInt32();
            List<MethodInfo> methods = new List<MethodInfo>(n);
            for (int i = 0; i < n; ++i)
            {
                string name = Utility.ReadString(reader);
                string ownerFullName = Utility.ReadString(reader);
                var ownerType = FindType(ownerFullName);
                Debug.Assert(ownerType != null);
                int line = reader.ReadInt32();
                int column = reader.ReadInt32();
                AccessModifier accessModifier = (AccessModifier)reader.ReadInt32();
                TypeModifier modifier = (TypeModifier)reader.ReadInt32();
                var attributes = DeserializeAttributes(reader);
                string parameterList = Utility.ReadString(reader);
                string comment = Utility.ReadString(reader);
                var attribute = (Attribute)reader.ReadInt32();

                MethodInfo method = new MethodInfo(attribute, line, column, comment, name, ownerType, accessModifier, modifier, attributes, parameterList);
                methods.Add(method);
            }

            return methods;
        }

        List<PropertyInfo> DeserializeProperties(BinaryReader reader)
        {
            int n = reader.ReadInt32();
            List<PropertyInfo> properties = new List<PropertyInfo>(n);
            for (int i = 0; i < n; ++i)
            {
                string name = Utility.ReadString(reader);
                TypeInfo ownerType = FindType(Utility.ReadString(reader));
                Debug.Assert(ownerType != null);
                TypeModifier modifier = (TypeModifier)reader.ReadInt32();
                int line = reader.ReadInt32();
                int column = reader.ReadInt32();
                var attributes = DeserializeAttributes(reader);
                PropertySetterGetterInfo setter = DeserializePropertySetterGetter(reader);
                PropertySetterGetterInfo getter = DeserializePropertySetterGetter(reader);
                string comment = Utility.ReadString(reader);
                var attribute = (Attribute)reader.ReadInt32();

                PropertyInfo prop = new PropertyInfo(attribute, line, column, comment, name, ownerType, setter, getter, modifier, attributes);
                properties.Add(prop);
            }

            return properties;
        }

        PropertySetterGetterInfo DeserializePropertySetterGetter(BinaryReader reader)
        {
            PropertySetterGetterInfo info = null;
            bool hasSetterOrGetter = reader.ReadBoolean();
            if (hasSetterOrGetter)
            {
                AccessModifier accessModifier = (AccessModifier)reader.ReadInt32();
                int line = reader.ReadInt32();
                int column = reader.ReadInt32();
                info = new PropertySetterGetterInfo(accessModifier, line, column);
            }
            return info;
        }

        void DeserializeGroup(BinaryReader reader)
        {
            var group = new GroupInfo();
            int typeCount = reader.ReadInt32();
            for (int i = 0; i < typeCount; ++i)
            {
                var typeName = Utility.ReadString(reader);
                group.AddType(FindType(typeName));
            }
            mGroups.Add(group);
        }

        TypeInfo FindType(string fullName)
        {
            for (int i = 0; i < mAllTypes.Count; ++i)
            {
                if (mAllTypes[i].nameInfo.fullName == fullName)
                {
                    return mAllTypes[i];
                }
            }
            Debug.Assert(false);
            return null;
        }

        void PostLoad()
        {
            for (int i = mAllTypes.Count - 1; i >= 0; --i)
            {
                if (mAllTypes[i].isExternal)
                {
                    mAllExternalTypes.Add(mAllTypes[i]);
                    mAllTypes.RemoveAt(i);
                }
            }
        }

        public List<TypeInfo> allTypes { get { return mAllTypes; } }
        public List<TypeInfo> allExternalTypes { get { return mAllExternalTypes; } }
        public List<GroupInfo> groups { get { return mGroups; } }
        public string csProjectName { get { return mCSProjectName; } set { mCSProjectName = value; } }
        public string predefinedSymbols { get { return mPredefinedSymbols; } set { mPredefinedSymbols = value; } }
        public string codeFolderName { get { return mCodeFolderName; } set { mCodeFolderName = value; } }
        public string parseResultSavePath { get { return mParseResultSavePath; } set { mParseResultSavePath = value; } }
        public SearchType searchType { get { return mSearchType; } set { mSearchType = value; } }
        public SearchMode searchMode { get { return mSearchMode; } set { mSearchMode = value; } }
        public bool searchMatchCase { get { return mSearchMatchCase; } set { mSearchMatchCase = value; } }
        public string searchText { get { return mSearchText; } set { mSearchText = value; } }
        public string vsDTEFilePath { get { return mVSDTEFilePath; } }
        public string serverFilePath { get { return mServerFilePath; } }
        public DisplayMode displayMode { get { return mDisplayMode; } set { mDisplayMode = value; } }
        public Attribute attributeDisplayMask { get { return mAttributeDisplayMask; } set { mAttributeDisplayMask = value; } }

        List<TypeInfo> mAllTypes = new List<TypeInfo>();
        List<TypeInfo> mAllExternalTypes = new List<TypeInfo>();
        List<GroupInfo> mGroups = new List<GroupInfo>();
        
        //wzw temp
        string mCodeFolderName = "Assets\\XDay\\Editor\\Core\\Implementation\\Utility\\CodeAssistant";
        string mVSDTEFilePath = "D:\\my_projects\\playground\\tool\\vs_dte\\vs_dte\\bin\\Debug\\vs_dte.exe";
        string mServerFilePath = "Tools\\CodeAssistantServer\\bin\\Debug\\net6.0\\CodeAssistantServer.exe";
        string mCSProjectName = "map2";

        //;隔开
        string mPredefinedSymbols = "";
        string mParseResultSavePath = "d:/temp/test.dat";
        SearchType mSearchType = SearchType.All;
        SearchMode mSearchMode = SearchMode.MatchPart;
        bool mSearchMatchCase = false;
        Attribute mAttributeDisplayMask = Attribute.All;
        string mSearchText;
        DisplayMode mDisplayMode = DisplayMode.Detail;
    }
}

