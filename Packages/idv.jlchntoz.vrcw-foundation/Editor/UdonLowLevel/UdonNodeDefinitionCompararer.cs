using System.Collections.Generic;
using VRC.Udon.Graph;

namespace JLChnToZ.VRC.Foundation.UdonLowLevel {
    internal class UdonNodeDefinitionCompararer : IComparer<UdonNodeDefinition>, IEqualityComparer<UdonNodeDefinition> {
        public static readonly UdonNodeDefinitionCompararer instance = new UdonNodeDefinitionCompararer();

        public int Compare(UdonNodeDefinition lhs, UdonNodeDefinition rhs) {
            if (lhs == rhs) return 0;
            if (lhs.parameters.Count != rhs.parameters.Count) return 0;
            int score = 0;
            for (int i = lhs.parameters.Count - 1; i >= 0; i--) {
                var lhsParam = lhs.parameters[i];
                var rhsParam = rhs.parameters[i];
                if (lhsParam.parameterType == UdonNodeParameter.ParameterType.OUT &&
                    rhsParam.parameterType == UdonNodeParameter.ParameterType.OUT)
                    continue;
                var lhsType = lhsParam.type;
                var rhsType = rhsParam.type;
                if (lhsType == rhsType) continue;
                if (lhsType.IsArray && rhsType.IsArray) {
                    score += lhsType.GetElementType().IsValueType ? rhsType.GetElementType().IsValueType ? 0 : -1 : 1;
                    continue;
                }
                if (lhsType.IsValueType || rhsType.IsValueType) {
                    score += lhsType.IsValueType ? rhsType.IsValueType ? 0 : -1 : 1;
                    continue;
                }
                if (TypeHelper.IsTypeAssignable(lhsType, rhsType)) {
                    while (rhsType != lhsType && rhsType != null) {
                        score++;
                        rhsType = rhsType.BaseType;
                    }
                    continue;
                }
                if (TypeHelper.IsTypeAssignable(rhsType, lhsType)) {
                    while (rhsType != lhsType && lhsType != null) {
                        score--;
                        lhsType = lhsType.BaseType;
                    }
                    continue;
                }
            }
            return score > 0 ? 1 : score < 0 ? -1 : 0;
        }
        
        public bool Equals(UdonNodeDefinition lhs, UdonNodeDefinition rhs) {
            if (lhs == null) return rhs != null;
            if (rhs == null || lhs.parameters.Count != rhs.parameters.Count) return false;
            for (int i = lhs.parameters.Count - 1; i >= 0; i--) {
                var lhsParam = lhs.parameters[i];
                var rhsParam = rhs.parameters[i];
                if (lhsParam.parameterType != rhsParam.parameterType)
                return false;
                if (lhsParam.parameterType == UdonNodeParameter.ParameterType.OUT)
                continue;
                if (lhsParam.type != rhsParam.type)
                return false;
            }
            return true;
        }

        public int GetHashCode(UdonNodeDefinition target) {
            if (target == null) return 0;
            int result = 0;
            for (int i = target.parameters.Count - 1; i >= 0; i--) {
                var parameter = target.parameters[i];
                if (parameter.parameterType == UdonNodeParameter.ParameterType.OUT) continue;
                result ^= parameter.type.GetHashCode();
            }
            return result;
        }
    }
}