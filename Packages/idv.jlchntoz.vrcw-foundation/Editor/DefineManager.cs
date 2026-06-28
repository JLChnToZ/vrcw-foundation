using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;

namespace JLChnToZ.VRC.Foundation.Editors {
    static class DefineManager {
        static readonly StringComparer defineComparer = StringComparer.Ordinal;

        [InitializeOnLoadMethod]
        static void OnLoad() {
            var appDomain = AppDomain.CurrentDomain;
            var assemblies = appDomain.GetAssemblies();
            var declares = new HashSet<DeclareInfo>();
            foreach (var assembly in assemblies) GetDeclareInfosFromAssembly(declares, assembly);
            if (declares.Count > 0) DeclareDefines(declares);
            appDomain.AssemblyLoad += OnAssemblyLoad;
        }

        static void GetDeclareInfosFromAssembly(HashSet<DeclareInfo> results, Assembly assembly) {
            var assemblyAttributes = assembly.GetCustomAttributes(typeof(DeclareDefineBaseAttribute), true);
            foreach (var o in assemblyAttributes)
                if (o is DeclareDefineAttribute declareAttr)
                    results.Add(new DeclareInfo(declareAttr.DefineName, true));
                else if (o is DeclareUndefineAttribute undefineAttr)
                    results.Add(new DeclareInfo(undefineAttr.DefineName, false));
        }

        static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args) {
            var declares = new HashSet<DeclareInfo>();
            GetDeclareInfosFromAssembly(declares, args.LoadedAssembly);
            if (declares.Count == 0) return;
            EditorApplication.delayCall += () => DeclareDefines(declares);
        }

        static void DeclareDefines(IEnumerable<DeclareInfo> infos) {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var defines = new HashSet<string>(definesString.Split(';', StringSplitOptions.RemoveEmptyEntries), defineComparer);
            bool changed = false;
            foreach (var info in infos)
                changed |= info.isDefine ? defines.Add(info.defineName) : defines.Remove(info.defineName);
            if (!changed) return;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
        }

        readonly struct DeclareInfo : IEquatable<DeclareInfo> {
            public readonly string defineName;
            public readonly bool isDefine;

            public DeclareInfo(string defineName, bool isDefine) {
                this.defineName = defineName;
                this.isDefine = isDefine;
            }

            public bool Equals(DeclareInfo other) =>
                defineComparer.Equals(defineName, other.defineName) &&
                isDefine == other.isDefine;

            public override bool Equals(object obj) => obj is DeclareInfo other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(defineComparer.GetHashCode(defineName), isDefine);

            override public string ToString() => $"{(isDefine ? "Define" : "Undefine")} {defineName}";
        }
    }
}