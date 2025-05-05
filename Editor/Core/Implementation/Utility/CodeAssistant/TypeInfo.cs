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

namespace XDay.UtilityAPI.Editor.CodeAssistant
{
    public enum TypeName
    {
        Struct,
        Class,
        Interface,
        Enum,
    }

    public enum AccessModifier
    {
        Public,
        Internal,
        Protected,
        ProtectedInternal,
        Private,
    }

    [System.Flags]
    public enum TypeModifier
    {
        None = 0,
        Abstract = 1,
        Virtual = 2,
        Override = 4,
        Sealed = 8,
        Static = 16,
        Partial = 32,
        New = 64,
        Async = 128,
    }

    [System.Flags]
    public enum Attribute
    {
        NormalClass = 1 << 0,
        NormalMethodsAndProperties = 1 << 1,
        ImportantClass = 1 << 2,
        ImportantMethodsAndProperties = 1 << 3,
        TrivialClass = 1 << 4,
        TrivialMethodsAndProperties = 1 << 5,
        All = -1,
    }

    public class AttributeInfo
    {
        public AttributeInfo(string name)
        {
            mName = name;
        }

        public string name { get { return mName; } }

        string mName;
    }

    class TypeInfoComparer : IComparer<TypeInfo>
    {
        public int Compare(TypeInfo x, TypeInfo y)
        {
            return x.nameInfo.name.CompareTo(y.nameInfo.name);
        }
    }

    public class NameInfo
    {
        public NameInfo(string name, string namespaceName, string outerClassName, string fullNameWithoutNameSpaceName, string fullName)
        {
            this.name = name;
            this.namespaceName = namespaceName;
            this.outerClassName = outerClassName;
            this.fullNameWithoutNameSpaceName = fullNameWithoutNameSpaceName;
            this.fullName = fullName;
        }

        public string name;
        public string namespaceName;
        //对嵌套类型有效
        public string outerClassName;
        public string fullNameWithoutNameSpaceName;
        public string fullName;
        public string modifiedName;
        public string modifiedFullNameWithoutNameSpaceName;
    }

    public abstract class Object
    {
        public Object(Attribute attribute, int line, int column, string comment)
        {
            mAttribute = attribute;
            mLineNumber = line;
            mColumn = column;
            mComment = comment;
        }

        public int rows { get { return mRows; } set { mRows = value; } }
        public Attribute attribute { get { return mAttribute; } }
        public string comment { get { return mComment; } }
        public int lineNumber { get { return mLineNumber; } }
        public int column { get { return mColumn; } }

        Attribute mAttribute;
        int mLineNumber;
        int mColumn;
        string mComment;
        int mRows = -1;
    }

    public abstract class TypeInfo : Object
    {
        public TypeInfo(Attribute attribute, int line, int column, string comment, ulong hash, NameInfo name, string filePath, TypeModifier modifier, AccessModifier accessModifier, bool isExternal) : base(attribute, line, column, comment)
        {
            mName = name;
            mFilePath = filePath;
            mFileName = System.IO.Path.GetFileName(filePath);
            mModifier = modifier;
            mHash = hash;
            mIsExternal = isExternal;
            mAccessModifier = accessModifier;
            mName.modifiedName = $"{Utility.GetAccessModifierString(accessModifier)} {name.name}";
            mName.modifiedFullNameWithoutNameSpaceName = $"{Utility.GetAccessModifierString(accessModifier)} {name.fullNameWithoutNameSpaceName}";
        }

        public void AddBase(TypeInfo type)
        {
            if (mBase.Contains(type) == false)
            {
                mBase.Add(type);
                if (type.isExternal == false)
                {
                    mNoneExternalBase.Add(type);
                }
            }
        }

        public void AddDerived(TypeInfo derived)
        {
            if (mDerived.Contains(derived) == false)
            {
                mDerived.Add(derived);
            }
        }

        public void AddNest(TypeInfo nest)
        {
            mNested.Add(nest);
        }

        public void AddAttribute(AttributeInfo attribute)
        {
            if (mAttributes.Contains(attribute) == false)
            {
                mAttributes.Add(attribute);
            }
        }

        public void AddMethod(MethodInfo method)
        {
            if (mMethods.Contains(method) == false)
            {
                mMethods.Add(method);
            }
        }

        public void SortMethods()
        {
            mMethods.Sort((MethodInfo a, MethodInfo b) => { 
                if (a.accessModifier != b.accessModifier)
                {
                    return (int)a.accessModifier - (int)b.accessModifier;
                }
                return a.name.CompareTo(b.name);
            });
        }

        public void AddProperty(PropertyInfo property)
        {
            if (mProperties.Contains(property) == false)
            {
                mProperties.Add(property);
            }
        }

        public override string ToString()
        {
            return mName.fullNameWithoutNameSpaceName;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is TypeInfo type)
            {
                return mName.fullName == type.nameInfo.fullName;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return mName.fullName.GetHashCode();
        }

        public abstract TypeName type { get; }
        public NameInfo nameInfo { get { return mName; } }
        
        public string description { get
            {
                return $"File: {mFileName}, Namespace: {mName.namespaceName}";
            } }
        public string filePath { get { return mFilePath; } }
        public string fileName { get { return mFileName; } }
        public bool isExternal { get { return mIsExternal; } }
        
        public AccessModifier accessModifier { get { return mAccessModifier; } }
        public TypeModifier modifier { get { return mModifier; } }
        public ulong hash { get { return mHash; } }
        public List<TypeInfo> baseTypes { get { return mBase; } }
        public List<TypeInfo> noneExternalBaseTypes { get { return mNoneExternalBase; } }
        public List<TypeInfo> derivedTypes { get { return mDerived; } }
        public List<TypeInfo> nestedTypes { get { return mNested; } }
        public List<MethodInfo> methods { get { return mMethods; } }
        public List<PropertyInfo> properties { get { return mProperties; } }
        public List<AttributeInfo> attributes { get { return mAttributes; } }

        ulong mHash;
        NameInfo mName;
        string mFilePath;
        string mFileName;
        TypeModifier mModifier;
        AccessModifier mAccessModifier;        
        bool mIsExternal;
        List<TypeInfo> mBase = new List<TypeInfo>();
        List<TypeInfo> mNoneExternalBase = new List<TypeInfo>();
        List<TypeInfo> mDerived = new List<TypeInfo>();
        List<TypeInfo> mNested = new List<TypeInfo>();
        List<AttributeInfo> mAttributes = new List<AttributeInfo>();
        List<MethodInfo> mMethods = new List<MethodInfo>();
        List<PropertyInfo> mProperties = new List<PropertyInfo>();
    }

    internal class ClassInfo : TypeInfo
    {
        public ClassInfo(Attribute attribute, int line, int column, string comment, ulong hash, NameInfo name, string filePath, TypeModifier modifier, AccessModifier accessModifier, bool isExternal, bool isAttributeClass) : base(attribute, line, column, comment, hash, name, filePath, modifier, accessModifier, isExternal)
        {
            mIsAttribute = isAttributeClass;
        }

        public override string ToString()
        {
            var s = base.ToString();
            s += $" IsAttribute: {mIsAttribute}";
            return s;
        }

        public override TypeName type { get { return TypeName.Class; } }
        public bool isAttribute { get { return mIsAttribute; } }

        bool mIsAttribute;
    }

    internal class InterfaceInfo : TypeInfo
    {
        public InterfaceInfo(Attribute attribute, int line, int column, string comment, ulong hash, NameInfo name, string filePath, TypeModifier modifier, AccessModifier accessModifier, bool isExternal) : base(attribute, line, column, comment, hash, name, filePath, modifier, accessModifier, isExternal)
        {
        }

        public override TypeName type { get { return TypeName.Interface; } }
    }

    internal class StructInfo : TypeInfo
    {
        public StructInfo(Attribute attribute, int line, int column, string comment, ulong hash, NameInfo name, string filePath, TypeModifier modifier, AccessModifier accessModifier, bool isExternal) : base(attribute, line, column, comment, hash, name, filePath, modifier, accessModifier, isExternal)
        {
        }

        public override TypeName type { get { return TypeName.Struct; } }
    }

    internal class EnumInfo : TypeInfo
    {
        public EnumInfo(Attribute attribute, int line, int column, string comment, ulong hash, NameInfo name, string filePath, AccessModifier accessModifier, string[] members) : base(attribute, line, column, comment, hash, name, filePath, TypeModifier.None, accessModifier, false)
        {
            mMembers = members;
        }

        public override string ToString()
        {
            string memberString = "";
            for (int i = 0; i < members.Length; ++i)
            {
                memberString += $"{mMembers[i]}";
                if (i != members.Length - 1)
                {
                    memberString += ",";
                }
            }
            var s = base.ToString();
            s += $" Members: {memberString}";
            return s;
        }

        public override TypeName type { get { return TypeName.Enum; } }
        public string[] members { get { return mMembers; } }

        string[] mMembers;
    }

    public class MethodInfo : Object
    {
        public MethodInfo(Attribute attribute, int line, int column, string comment, string name, TypeInfo ownerType, AccessModifier accessModifier, TypeModifier modifier, List<AttributeInfo> attributes, string parameterList) : base(attribute, line, column, comment)
        {
            mName = name;
            mOwner = ownerType;
            mModifier = modifier;
            mAccessModifier = accessModifier;
            mAttributes = attributes;
            mParameterList = parameterList;
            mFullName = $"{name}({parameterList})";
            mModifiedName = $"{Utility.GetAccessModifierString(accessModifier)} {name}";
            mModifiedFullName = $"{Utility.GetAccessModifierString(accessModifier)} {mFullName}";
        }

        public string name { get { return mName; } }
        public string fullName { get { return mFullName; } }
        public string modifiedFullName { get { return mModifiedFullName; } }
        public string modifiedName { get { return mModifiedName; } }
        public string parameterList { get { return mParameterList; } }
        public string description { get
            {
                return $"File: {owner.fileName}, Type: {owner.nameInfo.name}";
            } }
        public TypeInfo owner { get { return mOwner; } }
        public AccessModifier accessModifier { get { return mAccessModifier; } }
        public TypeModifier modifier { get { return mModifier; } }
        public List<AttributeInfo> attributes { get { return mAttributes; } }

        string mName;
        string mParameterList;
        string mFullName;
        string mModifiedFullName;
        string mModifiedName;
        TypeInfo mOwner;
        AccessModifier mAccessModifier;
        TypeModifier mModifier;
        List<AttributeInfo> mAttributes = new List<AttributeInfo>();
    }

    public class PropertySetterGetterInfo
    {
        public PropertySetterGetterInfo(AccessModifier accessModifier, int line, int column)
        {
            mAccessModifier = accessModifier;
            mLine = line;
            mColumn = column;
        }

        public AccessModifier accessModifier { get { return mAccessModifier; } }
        public int line { get { return mLine; } }
        public int column { get { return mColumn; } }

        AccessModifier mAccessModifier;
        int mLine;
        int mColumn;
    }

    public class PropertyInfo : Object
    {
        public PropertyInfo(Attribute attribute, int line, int column, string comment, string name, TypeInfo ownerType, PropertySetterGetterInfo setterInfo, PropertySetterGetterInfo getterInfo, TypeModifier modifier, List<AttributeInfo> attributes) : base(attribute, line, column, comment)
        {
            mName = name;
            mOwner = ownerType;
            mModifier = modifier;
            mAttributes = attributes;
            mSetter = setterInfo;
            mGetter = getterInfo;
            if (setter != null)
            {
                mFullName = $"{name} [{Utility.GetAccessModifierString(getter.accessModifier)}g,{Utility.GetAccessModifierString(setter.accessModifier)}s]";
            }
            else
            {
                mFullName = $"{name} [{Utility.GetAccessModifierString(getter.accessModifier)}g]";
            }
        }

        public string description
        {
            get
            {
                return $"File: {owner.fileName}, Type: {owner.nameInfo.name}";
            }
        }
        public string name { get { return mName; } }
        public string fullName { get { return mFullName; } }
        public TypeInfo owner { get { return mOwner; } }
        public TypeModifier modifier { get { return mModifier; } }
        public List<AttributeInfo> attributes { get { return mAttributes; } }
        public PropertySetterGetterInfo getter { get { return mGetter; } }
        public PropertySetterGetterInfo setter { get { return mSetter; } }

        string mName;
        string mFullName;
        TypeInfo mOwner;
        PropertySetterGetterInfo mSetter;
        PropertySetterGetterInfo mGetter;
        TypeModifier mModifier;
        List<AttributeInfo> mAttributes = new List<AttributeInfo>();
    }
}

