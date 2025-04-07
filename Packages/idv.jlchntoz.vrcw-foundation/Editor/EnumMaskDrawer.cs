using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace JLChnToZ.VRC.Foundation.Editors {
    /// <summary>
    /// A property drawer for enum mask.
    /// </summary>
    /// <remarks>
    /// This adds support for <c>[EnumMask]</c> attribute in Unity's shader properties.
    /// </remarks>
    public class EnumMaskDrawer : MaterialPropertyDrawer {
        static readonly Dictionary<string, (string[] names, int[] values)> enumCache = new Dictionary<string, (string[], int[])>();
        const int MAX_SAFE_FLOAT = (1 << 23) - 1;
        readonly string[] enumNames;
        readonly int[] enumValues;

        static void GetFilteredNamesAndValues(string[] enumNames, out string[] filteredNames, out int[] filteredValues) {
            if (enumNames != null && enumNames.Length > 0) {
                var nameList = new List<string>(enumNames.Length);
                var valueList = new List<int>(enumNames.Length);
                for (int i = 0; i < enumNames.Length; i++) {
                    if (string.IsNullOrWhiteSpace(enumNames[i]) || enumNames[i] == "_") continue;
                    nameList.Add(enumNames[i]);
                    valueList.Add(1 << i);
                }
                if (nameList.Count > 0) {
                    filteredNames = nameList.ToArray();
                    filteredValues = valueList.ToArray();
                    return;
                }
            }
            filteredNames = Array.Empty<string>();
            filteredValues = Array.Empty<int>();
        }

        static bool GetTypedNamesAndValues(string typeName, out string[] names, out int[] values) {
            if (string.IsNullOrWhiteSpace(typeName)) {
                names = null;
                values = null;
                return false;
            }
            if (enumCache.TryGetValue(typeName, out var cached)) {
                names = cached.names;
                values = cached.values;
                return true;
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) 
                if (GetTypedNamesAndValues(assembly.GetType(typeName, false), out names, out values)) {
                    enumCache[typeName] = (names, values);
                    return true;
                }
            names = null;
            values = null;
            return false;
        }

        static bool GetTypedNamesAndValues(Type type, out string[] names, out int[] values) {
            if (type == null || !type.IsEnum) {
                names = null;
                values = null;
                return false;
            }
            var rawValues = Enum.GetValues(type);
            var typeCode = Type.GetTypeCode(type);
            var nameList = new List<string>(rawValues.Length);
            var valueList = new List<int>(rawValues.Length);
            var set = new HashSet<int>(rawValues.Length);
            for (int i = 0; i < rawValues.Length; i++) {
                var enumValue = rawValues.GetValue(i);
                int intValue;
                switch (typeCode) {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32: intValue = Convert.ToInt32(enumValue); break;
                    case TypeCode.UInt32: intValue = unchecked((int)Convert.ToUInt32(enumValue)); break;
                    case TypeCode.Int64: intValue = unchecked((int)Convert.ToInt64(enumValue)); break;
                    case TypeCode.UInt64: intValue = unchecked((int)Convert.ToUInt64(enumValue)); break;
                    default: continue;
                }
                // Ignore duplicate values or "None" value
                if (intValue == 0 || !set.Add(intValue)) continue;
                var name = Enum.GetName(type, enumValue);
                var matchingMember = type.GetField(name);
                if (matchingMember != null) {
                    if (matchingMember.GetCustomAttribute<ObsoleteAttribute>() != null) continue;
                    var displayName = matchingMember.GetCustomAttribute<InspectorNameAttribute>();
                    if (displayName != null && !string.IsNullOrWhiteSpace(displayName.displayName))
                        name = displayName.displayName;
                }
                nameList.Add(name);
                valueList.Add(intValue);
            }
            // If flag count still more than 32, sacrifice some of them,
            // starting from the ones with the most bits set,
            // which likely to be shorthands of common combinations,
            // including something like "All".
            if (nameList.Count > 32) {
                int count = nameList.Count;
                var skip = new bool[count];
                var bitCount = new int[count];
                set.Clear();
                // Calculate bit count for each value
                for (int i = 0; i < count; i++) {
                    int value = valueList[i];
                    value = (value & 0x55555555) + ((value >> 1) & 0x55555555);
                    value = (value & 0x33333333) + ((value >> 2) & 0x33333333);
                    value = (value & 0x0f0f0f0f) + ((value >> 4) & 0x0f0f0f0f);
                    value = (value & 0x00ff00ff) + ((value >> 8) & 0x00ff00ff);
                    value = (value & 0x0000ffff) + ((value >> 16) & 0x0000ffff);
                    bitCount[i] = value;
                    set.Add(i);
                }
                var bc = new int[set.Count];
                set.CopyTo(bc);
                Array.Sort(bc);
                for (int bci = bc.Length - 1, bcc = bitCount.Length; count > 32 && bci >= 0; ) {
                    int i = Array.LastIndexOf(bitCount, bc[bci], bcc - 1);
                    if (i < 0) {
                        bci--;
                        bcc = bitCount.Length;
                    } else {
                        skip[i] = true;
                        count--;
                        bcc = i;
                    }
                }
                for (int i = skip.Length - 1; i >= 0; i--)
                    if (skip[i]) {
                        nameList.RemoveAt(i);
                        valueList.RemoveAt(i);
                    }
            }
            names = nameList.ToArray();
            values = valueList.ToArray();
            return true;
        }

        public EnumMaskDrawer(string[] names) =>
            GetFilteredNamesAndValues(names, out enumNames, out enumValues);

        public EnumMaskDrawer(string typeOrName) {
            if (!GetTypedNamesAndValues(typeOrName, out enumNames, out enumValues))
                GetFilteredNamesAndValues(new[] { typeOrName }, out enumNames, out enumValues);
        }

        #region Constructor Fixed Argument Count Overloads
        public EnumMaskDrawer(string name1, string name2
        ) : this(new[] { name1, name2 }) { }
        public EnumMaskDrawer(string name1, string name2, string name3
        ) : this(new[] { name1, name2, name3 }) { }
        public EnumMaskDrawer(string name1, string name2, string name3, string name4
        ) : this(new[] { name1, name2, name3, name4 }) { }
        public EnumMaskDrawer(string name1, string name2, string name3, string name4, string name5
        ) : this(new[] { name1, name2, name3, name4, name5 }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6
        ) : this(new[] { name1, name2, name3, name4, name5, name6 }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7
        ) : this(new[] { name1, name2, name3, name4, name5, name6, name7 }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8
        ) : this(new[] { name1, name2, name3, name4, name5, name6, name7, name8 }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9
        ) : this(new[] { name1, name2, name3, name4, name5, name6, name7, name8, name9 }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10
        ) : this(new[] { name1, name2, name3, name4, name5, name6, name7, name8, name9, name10 }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11
        ) : this(new[] { name1, name2, name3, name4, name5, name6, name7, name8, name9, name10, name11 }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14, string name15
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14, name15
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14, string name15,
            string name16
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14, name15, name16
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14, string name15,
            string name16, string name17
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14, name15, name16, name17
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14, string name15,
            string name16, string name17, string name18
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14, name15, name16, name17, name18
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14, string name15,
            string name16, string name17, string name18, string name19
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14, name15, name16,
        name17, name18, name19
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14, string name15,
            string name16, string name17, string name18, string name19, string name20
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14, name15, name16,
        name17, name18, name19, name20
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14, string name15,
            string name16, string name17, string name18, string name19, string name20,
            string name21
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14, name15, name16,
        name17, name18, name19, name20, name21
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14, string name15,
            string name16, string name17, string name18, string name19, string name20,
            string name21, string name22
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14, name15, name16,
        name17, name18, name19, name20, name21, name22
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14, string name15,
            string name16, string name17, string name18, string name19, string name20,
            string name21, string name22, string name23
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14, name15, name16,
        name17, name18, name19, name20, name21, name22, name23
        }) { }
        public EnumMaskDrawer(
            string name1, string name2, string name3, string name4, string name5,
            string name6, string name7, string name8, string name9, string name10,
            string name11, string name12, string name13, string name14, string name15,
            string name16, string name17, string name18, string name19, string name20,
            string name21, string name22, string name23, string name24
        ) : this(new[] {
        name1, name2, name3, name4, name5, name6, name7, name8,
        name9, name10, name11, name12, name13, name14, name15, name16,
        name17, name18, name19, name20, name21, name22, name23, name24
        }) { }
        #endregion

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor) {
            if (enumValues.Length == 0) {
                editor.DefaultShaderProperty(position, prop, label);
                return;
            }
            int value;
            try {
                switch (prop.type) {
                    case MaterialProperty.PropType.Float:
                    case MaterialProperty.PropType.Range:
                        value = (int)prop.floatValue;
                        break;
                    case MaterialProperty.PropType.Int:
                        value = prop.intValue;
                        break;
                    default:
                        editor.DefaultShaderProperty(position, prop, label);
                        return;
                }
            } catch {
                value = 0;
            }
            int mask = 0, newMask;
            for (int i = 0; i < enumValues.Length; i++)
                if ((value & enumValues[i]) == enumValues[i])
                    mask |= 1 << i;
            MaterialEditor.BeginProperty(position, prop);
            using (var changeCheck = new EditorGUI.ChangeCheckScope()) {
                newMask = EditorGUI.MaskField(position, label, mask, enumNames);
                if (!changeCheck.changed) return;
            }
            MaterialEditor.EndProperty();
            if (newMask == 0) value = 0;
            else {
                mask ^= newMask;
                for (int i = 0; i < enumValues.Length; i++) {
                    int bit = 1 << i;
                    if ((mask & bit) == 0) continue;
                    if ((newMask & bit) == bit)
                        value |= enumValues[i];
                    else
                        value &= ~enumValues[i];
                }
            }
            switch (prop.type) {
                case MaterialProperty.PropType.Float:
                case MaterialProperty.PropType.Range:
                    prop.floatValue = value & MAX_SAFE_FLOAT;
                    break;
                case MaterialProperty.PropType.Int:
                    prop.intValue = value;
                    break;
            }
        }
    }
}