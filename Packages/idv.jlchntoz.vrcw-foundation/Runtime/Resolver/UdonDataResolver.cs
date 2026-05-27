using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using VRC.SDKBase;
using VRC.SDK3.Data;
using UdonSharp;

using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public static class DataResolver {
        static readonly ConditionalWeakTable<Type, FieldInfo[]> typeFieldsCache = new ConditionalWeakTable<Type, FieldInfo[]>();
        static readonly FieldInfo udonSharpBackingField = typeof(UdonSharpBehaviour).GetField("_udonSharpBackingUdonBehaviour", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        public static DataToken ToVRCToken(object o) {
            if (o == null) return default;
            if (o is DataToken token) return token;
            if (o is DataDictionary dict) return dict;
            if (o is DataList list) return list;
            if (o is UdonSharpBehaviour usb) return udonSharpBackingField.GetValue(usb) as UnityObject;
            if (o is UnityObject uobj) return uobj;
            switch (Convert.GetTypeCode(o)) {
                case TypeCode.Empty:
                case TypeCode.DBNull: return default;
                case TypeCode.Boolean: return Convert.ToBoolean(o);
                case TypeCode.Byte: return Convert.ToByte(o);
                case TypeCode.Char: return Convert.ToChar(o);
                case TypeCode.Double:
                case TypeCode.Decimal: return Convert.ToDouble(o);
                case TypeCode.Int16: return Convert.ToInt16(o);
                case TypeCode.Int32: return Convert.ToInt32(o);
                case TypeCode.Int64: return Convert.ToInt64(o);
                case TypeCode.SByte: return Convert.ToSByte(o);
                case TypeCode.Single: return Convert.ToSingle(o);
                case TypeCode.String: return Convert.ToString(o);
                case TypeCode.UInt16: return Convert.ToUInt16(o);
                case TypeCode.UInt32: return Convert.ToUInt32(o);
                case TypeCode.UInt64: return Convert.ToUInt64(o);
                case TypeCode.DateTime: return Convert.ToDateTime(o).ToString("o");
            }
            if (o is IDictionary idict) {
                dict = new DataDictionary();
                foreach (DictionaryEntry kv in idict)
                    dict[ToVRCToken(kv.Key)] = ToVRCToken(kv.Value);
                return dict;
            }
            if (o is IEnumerable array) {
                list = new DataList();
                foreach (var item in array)
                    list.Add(ToVRCToken(item));
                return list;
            }
            if (o is StringBuilder sb) return sb.ToString();
            if (o is Uri uri) return uri.ToString();
            if (o is Regex regex) return regex.ToString();
            if (o is VRCUrl url) return new DataToken(url);
            var type = o.GetType();
            if (!typeFieldsCache.TryGetValue(type, out var fields)) {
                if (!type.IsClass && !type.IsValueType)
                    throw new ArgumentException($"Unsupported type: {type.FullName}");
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                typeFieldsCache.Add(type, fields);
            }
            dict = new DataDictionary();
            foreach (var field in fields) {
                if (field.IsNotSerialized) continue;
                dict[field.Name] = ToVRCToken(field.GetValue(o));
            }
            return dict;
        }
    }
}