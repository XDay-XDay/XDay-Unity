#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
// https://developers.google.com/protocol-buffers/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;

namespace Google.Protobuf
{
    /// <summary>
    /// Methods for managing <see cref="ExtensionSet{TTarget}"/>s with null checking.
    /// 
    /// Most users will not use this class directly and its API is experimental and subject to change.
    /// </summary>
    public static class ExtensionSet
    {
        private static bool TryGetValue<TTarget>(ref ExtensionSet<TTarget> set, Extension extension, out IExtensionValue value) where TTarget : IExtendableMessage<TTarget>
        {
            if (set == null)
            {
                value = null;
                return false;
            }
            return set.ValuesByNumber.TryGetValue(extension.FieldNumber, out value);
        }

        /// <summary>
        /// Gets the value of the specified extension
        /// </summary>
        public static TValue Get<TTarget, TValue>(ref ExtensionSet<TTarget> set, Extension<TTarget, TValue> extension) where TTarget : IExtendableMessage<TTarget>
        {
            if (TryGetValue(ref set, extension, out var value))
            {
                // The stored ExtensionValue can be a different type to what is being requested.
                // This happens when the same extension proto is compiled in different assemblies.
                // To allow consuming assemblies to still get the value when the TValue type is
                // different, this get method:
                // 1. Attempts to cast the value to the expected ExtensionValue<TValue>.
                //    This is the usual case. It is used first because it avoids possibly boxing the value.
                // 2. Fallback to get the value as object from IExtensionValue then casting.
                //    This allows for someone to specify a TValue of object. They can then convert
                //    the values to bytes and reparse using expected value.
                // 3. If neither of these work, throw a user friendly error that the types aren't compatible.
                if (value is ExtensionValue<TValue> extensionValue)
                {
                    return extensionValue.GetValue();
                }
                else if (value.GetValue() is TValue underlyingValue)
                {
                    return underlyingValue;
                }
                else
                {
                    var valueType = value.GetType().GetTypeInfo();
                    if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(ExtensionValue<>))
                    {
                        var storedType = valueType.GenericTypeArguments[0];
                        throw new InvalidOperationException(
                            "The stored extension value has a type of '" + storedType.AssemblyQualifiedName + "'. " +
                            "This a different from the requested type of '" + typeof(TValue).AssemblyQualifiedName + "'.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected extension value type: " + valueType.AssemblyQualifiedName);
                    }
                }
            }
            else
            {
                return extension.DefaultValue;
            }
        }

        /// <summary>
        /// Gets the value of the specified repeated extension or null if it doesn't exist in this set
        /// </summary>
        public static RepeatedField<TValue> Get<TTarget, TValue>(ref ExtensionSet<TTarget> set, RepeatedExtension<TTarget, TValue> extension) where TTarget : IExtendableMessage<TTarget>
        {
            if (TryGetValue(ref set, extension, out var value))
            {
                if (value is RepeatedExtensionValue<TValue> extensionValue)
                {
                    return extensionValue.GetValue();
                }
                else
                {
                    var valueType = value.GetType().GetTypeInfo();
                    if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(RepeatedExtensionValue<>))
                    {
                        var storedType = valueType.GenericTypeArguments[0];
                        throw new InvalidOperationException(
                            "The stored extension value has a type of '" + storedType.AssemblyQualifiedName + "'. " +
                            "This a different from the requested type of '" + typeof(TValue).AssemblyQualifiedName + "'.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected extension value type: " + valueType.AssemblyQualifiedName);
                    }
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the value of the specified repeated extension, registering it if it doesn't exist
        /// </summary>
        public static RepeatedField<TValue> GetOrInitialize<TTarget, TValue>(ref ExtensionSet<TTarget> set, RepeatedExtension<TTarget, TValue> extension) where TTarget : IExtendableMessage<TTarget>
        {
            IExtensionValue value;
            if (set == null)
            {
                value = extension.CreateValue();
                set = new ExtensionSet<TTarget>();
                set.ValuesByNumber.Add(extension.FieldNumber, value);
            }
            else
            {
                if (!set.ValuesByNumber.TryGetValue(extension.FieldNumber, out value))
                {
                    value = extension.CreateValue();
                    set.ValuesByNumber.Add(extension.FieldNumber, value);
                }
            }

            return ((RepeatedExtensionValue<TValue>)value).GetValue();
        }

        /// <summary>
        /// Sets the value of the specified extension. This will make a new instance of ExtensionSet if the set is null.
        /// </summary>
        public static void Set<TTarget, TValue>(ref ExtensionSet<TTarget> set, Extension<TTarget, TValue> extension, TValue value) where TTarget : IExtendableMessage<TTarget>
        {
            ProtoPreconditions.CheckNotNullUnconstrained(value, nameof(value));

            IExtensionValue extensionValue;
            if (set == null)
            {
                extensionValue = extension.CreateValue();
                set = new ExtensionSet<TTarget>();
                set.ValuesByNumber.Add(extension.FieldNumber, extensionValue);
            }
            else
            {
                if (!set.ValuesByNumber.TryGetValue(extension.FieldNumber, out extensionValue))
                {
                    extensionValue = extension.CreateValue();
                    set.ValuesByNumber.Add(extension.FieldNumber, extensionValue);
                }
            }

            ((ExtensionValue<TValue>)extensionValue).SetValue(value);
        }

        /// <summary>
        /// Gets whether the value of the specified extension is set
        /// </summary>
        public static bool Has<TTarget, TValue>(ref ExtensionSet<TTarget> set, Extension<TTarget, TValue> extension) where TTarget : IExtendableMessage<TTarget>
        {
            return TryGetValue(ref set, extension, out var _);
        }

        /// <summary>
        /// Clears the value of the specified extension
        /// </summary>
        public static void Clear<TTarget, TValue>(ref ExtensionSet<TTarget> set, Extension<TTarget, TValue> extension) where TTarget : IExtendableMessage<TTarget>
        {
            if (set == null)
            {
                return;
            }
            set.ValuesByNumber.Remove(extension.FieldNumber);
            if (set.ValuesByNumber.Count == 0)
            {
                set = null;
            }
        }

        /// <summary>
        /// Clears the value of the specified extension
        /// </summary>
        public static void Clear<TTarget, TValue>(ref ExtensionSet<TTarget> set, RepeatedExtension<TTarget, TValue> extension) where TTarget : IExtendableMessage<TTarget>
        {
            if (set == null)
            {
                return;
            }
            set.ValuesByNumber.Remove(extension.FieldNumber);
            if (set.ValuesByNumber.Count == 0)
            {
                set = null;
            }
        }

        /// <summary>
        /// Tries to merge a field from the coded input, returning true if the field was merged.
        /// If the set is null or the field was not otherwise merged, this returns false.
        /// </summary>
        public static bool TryMergeFieldFrom<TTarget>(ref ExtensionSet<TTarget> set, CodedInputStream stream) where TTarget : IExtendableMessage<TTarget>
        {
            ParseContext.Initialize(stream, out var ctx);
            try
            {
                return TryMergeFieldFrom<TTarget>(ref set, ref ctx);
            }
            finally
            {
                ctx.CopyStateTo(stream);
            }
        }

        /// <summary>
        /// Tries to merge a field from the coded input, returning true if the field was merged.
        /// If the set is null or the field was not otherwise merged, this returns false.
        /// </summary>
        public static bool TryMergeFieldFrom<TTarget>(ref ExtensionSet<TTarget> set, ref ParseContext ctx) where TTarget : IExtendableMessage<TTarget>
        {
            var lastFieldNumber = WireFormat.GetTagFieldNumber(ctx.LastTag);

            if (set != null && set.ValuesByNumber.TryGetValue(lastFieldNumber, out var extensionValue))
            {
                extensionValue.MergeFrom(ref ctx);
                return true;
            }
            else if (ctx.ExtensionRegistry != null && ctx.ExtensionRegistry.ContainsInputField(ctx.LastTag, typeof(TTarget), out var extension))
            {
                var value = extension.CreateValue();
                value.MergeFrom(ref ctx);
                set ??= new ExtensionSet<TTarget>();
                set.ValuesByNumber.Add(extension.FieldNumber, value);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Merges the second set into the first set, creating a new instance if first is null
        /// </summary>
        public static void MergeFrom<TTarget>(ref ExtensionSet<TTarget> first, ExtensionSet<TTarget> second) where TTarget : IExtendableMessage<TTarget>
        {
            if (second == null)
            {
                return;
            }
            if (first == null)
            {
                first = new ExtensionSet<TTarget>();
            }
            foreach (var pair in second.ValuesByNumber)
            {
                if (first.ValuesByNumber.TryGetValue(pair.Key, out var value))
                {
                    value.MergeFrom(pair.Value);
                }
                else
                {
                    var cloned = pair.Value.Clone();
                    first.ValuesByNumber[pair.Key] = cloned;
                }
            }
        }

        /// <summary>
        /// Clones the set into a new set. If the set is null, this returns null
        /// </summary>
        public static ExtensionSet<TTarget> Clone<TTarget>(ExtensionSet<TTarget> set) where TTarget : IExtendableMessage<TTarget>
        {
            if (set == null)
            {
                return null;
            }

            var newSet = new ExtensionSet<TTarget>();
            foreach (var pair in set.ValuesByNumber)
            {
                var cloned = pair.Value.Clone();
                newSet.ValuesByNumber[pair.Key] = cloned;
            }
            return newSet;
        }
    }

    /// <summary>
    /// Used for keeping track of extensions in messages. 
    /// <see cref="IExtendableMessage{T}"/> methods route to this set.
    /// 
    /// Most users will not need to use this class directly
    /// </summary>
    /// <typeparam name="TTarget">The message type that extensions in this set target</typeparam>
    public sealed class ExtensionSet<TTarget> where TTarget : IExtendableMessage<TTarget>
    {
        internal Dictionary<int, IExtensionValue> ValuesByNumber { get; } = new Dictionary<int, IExtensionValue>();

        /// <summary>
        /// Gets a hash code of the set
        /// </summary>
        public override int GetHashCode()
        {
            var ret = typeof(TTarget).GetHashCode();
            foreach (var field in ValuesByNumber)
            {
                // Use ^ here to make the field order irrelevant.
                var hash = field.Key.GetHashCode() ^ field.Value.GetHashCode();
                ret ^= hash;
            }
            return ret;
        }

        /// <summary>
        /// Returns whether this set is equal to the other object
        /// </summary>
        public override bool Equals(object other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            var otherSet = other as ExtensionSet<TTarget>;
            if (ValuesByNumber.Count != otherSet.ValuesByNumber.Count)
            {
                return false;
            }
            foreach (var pair in ValuesByNumber)
            {
                if (!otherSet.ValuesByNumber.TryGetValue(pair.Key, out var secondValue))
                {
                    return false;
                }
                if (!pair.Value.Equals(secondValue))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Calculates the size of this extension set
        /// </summary>
        public int CalculateSize()
        {
            var size = 0;
            foreach (var value in ValuesByNumber.Values)
            {
                size += value.CalculateSize();
            }
            return size;
        }

        /// <summary>
        /// Writes the extension values in this set to the output stream
        /// </summary>
        public void WriteTo(CodedOutputStream stream)
        {

            WriteContext.Initialize(stream, out var ctx);
            try
            {
                WriteTo(ref ctx);
            }
            finally
            {
                ctx.CopyStateTo(stream);
            }
        }

        /// <summary>
        /// Writes the extension values in this set to the write context
        /// </summary>
        [SecuritySafeCritical]
        public void WriteTo(ref WriteContext ctx)
        {
            foreach (var value in ValuesByNumber.Values)
            {
                value.WriteTo(ref ctx);
            }
        }

        internal bool IsInitialized()
        {
            return ValuesByNumber.Values.All(v => v.IsInitialized());
        }
    }
}
