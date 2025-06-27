using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace JLChnToZ.VRC.Foundation.Editors {
    static class DefineManager {
        [InitializeOnLoadMethod]
        static void OnLoad() {
            var appDomain = AppDomain.CurrentDomain;
            var assemblies = appDomain.GetAssemblies();
            var attributes = new HashSet<DeclareDefineAttribute>();
            foreach (var assembly in assemblies) {
                var assemblyAttributes = assembly.GetCustomAttributes(typeof(DeclareDefineAttribute), false);
                if (assemblyAttributes.Length > 0) attributes.UnionWith(assemblyAttributes.Cast<DeclareDefineAttribute>());
            }
            if (attributes.Count > 0) DeclareDefines(attributes);
            appDomain.AssemblyLoad += OnAssemblyLoad;
        }

        static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args) {
            var assembly = args.LoadedAssembly.GetCustomAttributes(typeof(DeclareDefineAttribute), false);
            if (assembly.Length == 0) return;
            var attributes = assembly.Cast<DeclareDefineAttribute>();
            EditorApplication.delayCall += () => DeclareDefines(attributes);
        }

        static void DeclareDefines(IEnumerable<DeclareDefineAttribute> attributes) {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var defines = new HashSet<string>(definesString.Split(';', StringSplitOptions.RemoveEmptyEntries));
            bool changed = false;
            foreach (var attr in attributes)
                if (!string.IsNullOrWhiteSpace(attr.DefineName) && defines.Add(attr.DefineName))
                    changed = true;
            if (!changed) return;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
        }
    }
}