using System;
using System.Collections.Generic;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Graph;
using VRC.Udon.Editor;

namespace JLChnToZ.VRC.Foundation.UdonLowLevel {
    public static class TypeHelper {
        static readonly Dictionary<Type, string> typeNames = new Dictionary<Type, string>();
        static readonly Dictionary<VariableName, Type> predefinedVariableTypes = new Dictionary<VariableName, Type> {
            [UdonBehaviour.ReturnVariableName] = typeof(object),
        };

        static TypeHelper() {
            foreach (var def in UdonEditorManager.Instance.GetNodeDefinitions()) {
                if (def.fullName.StartsWith("Type_")) {
                    if (!def.fullName.EndsWith("Ref") && !typeNames.ContainsKey(def.type))
                        typeNames[def.type] = def.fullName.Substring(5);
                    continue;
                }
                if (def.fullName.StartsWith("Event_")) {
                    var eventName = $"{char.ToLower(def.fullName[6])}{def.fullName.Substring(7)}";
                    foreach (var parameter in def.parameters)
                        if (parameter.parameterType == UdonNodeParameter.ParameterType.OUT)
                            predefinedVariableTypes[$"{eventName}{char.ToUpper(parameter.name[0])}{parameter.name.Substring(1)}"] = parameter.type;
                    continue;
                }
            }
            // IUdonEventReceiver is mapped to UnityEngine.Object, therefore we need to override it here.
            typeNames[typeof(IUdonEventReceiver)] = "VRCUdonCommonInterfacesIUdonEventReceiver";
            typeNames[typeof(IUdonEventReceiver[])] = "VRCUdonCommonInterfacesIUdonEventReceiverArray";
        }

        public static string GetUdonTypeName(this Type type, bool declareType = false) {
            if (!typeNames.TryGetValue(type, out var typeName)) return "SystemObject";
            if (declareType)
                switch (typeName) {
                    case "VRCUdonCommonInterfacesIUdonEventReceiver": return "VRCUdonUdonBehaviour";
                    case "VRCUdonCommonInterfacesIUdonEventReceiverArray": return "VRCUdonUdonBehaviourArray";
                }
            return typeName;
        }

        public static Type GetPredefinedType(this VariableName varName) {
            predefinedVariableTypes.TryGetValue(varName, out var type);
            return type;
        }

        public static bool IsTypeAssignable(Type from, Type to) {
            if (to == null || to == typeof(void)) return from == null || !from.IsValueType;
            return from.IsAssignableFrom(to);
        }

        public static bool IsTypeCompatable(Type from, Type to) {
            if (from == null || from == typeof(void)) return to == null || !to.IsValueType;
            if (to == null || to == typeof(void)) return from == null || !from.IsValueType;
            return from.IsAssignableFrom(to) || to.IsAssignableFrom(from);
        }
    }
}