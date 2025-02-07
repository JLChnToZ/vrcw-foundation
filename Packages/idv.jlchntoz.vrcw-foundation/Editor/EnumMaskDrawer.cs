using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JLChnToZ.VRC.Foundation.Editors {
    /// <summary>
    /// A property drawer for enum mask.
    /// </summary>
    /// <remarks>
    /// This adds support for <c>[EnumMask]</c> attribute in Unity's shader properties.
    /// </remarks>
    public class EnumMaskDrawer : MaterialPropertyDrawer {
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

        public EnumMaskDrawer(string[] names) =>
            GetFilteredNamesAndValues(names, out enumNames, out enumValues);

        public EnumMaskDrawer(string typeOrName) {
            if (!string.IsNullOrWhiteSpace(typeOrName))
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var type = assembly.GetType(typeOrName, false);
                    if (type == null || !type.IsEnum) continue;
                    var rawValues = Enum.GetValues(type);
                    var nameList = new List<string>(rawValues.Length);
                    var valueList = new List<int>(rawValues.Length);
                    for (int i = 0; i < rawValues.Length; i++) {
                        var enumValue = rawValues.GetValue(i);
                        int intValue = Convert.ToInt32(enumValue);
                        if (intValue == 0) continue;
                        nameList.Add(Enum.GetName(type, enumValue));
                        valueList.Add(intValue);
                        if (nameList.Count >= 32) break;
                    }
                    enumNames = nameList.ToArray();
                    enumValues = valueList.ToArray();
                    return;
                }
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
            using (var changeCheck = new EditorGUI.ChangeCheckScope()) {
                newMask = EditorGUI.MaskField(position, label, mask, enumNames);
                if (!changeCheck.changed) return;
            }
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