using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UdonSharp;

namespace JLChnToZ.VRC.Foundation.Editors {
    /// <summary>
    /// A customizable factory for creating UdonSharp assets in the project window.
    /// </summary>
    public sealed class ProjectWindowUdonSharpAssetFactory : EndNameEditAction {
        static readonly Encoding encoding = new UTF8Encoding(false);
        static readonly Regex classNameAllowedChars = new Regex(@"[^a-zA-Z0-9_]+", RegexOptions.Compiled);
        const string defaultTemplate = @"using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UdonSharp;

public class #CLASS# : UdonSharpBehaviour {
    void Start() {
        
    }
}
";
        string template;

        ProjectWindowUdonSharpAssetFactory() { }

        /// <summary> Create an U# class asset in the project window.</summary>
        /// <param name="typeName">The name of the class. Spaces are not allowed.</param>
        /// <param name="template">The template of the class. If null or empty, a default template will be used.</param>
        /// <remarks>
        /// The template should contain a `#CLASS#` placeholder which will be replaced with the class name.
        /// </remarks>
        public static void CreateAsset(string typeName, string template = null) {
            var action = CreateInstance<ProjectWindowUdonSharpAssetFactory>();
            action.template = template;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0, action, $"{typeName}.cs",
                AssetPreview.GetMiniTypeThumbnail(typeof(MonoScript)),
                null
            );
        }

        public override void Action(int instanceId, string pathName, string resourceFile) {
            string name;
            while (true) {
                name = Path.GetFileNameWithoutExtension(pathName);
                var newName = classNameAllowedChars.Replace(name, "");
                if (string.Equals(name, newName, StringComparison.OrdinalIgnoreCase)) break;
                pathName = AssetDatabase.GenerateUniqueAssetPath(
                    Path.Combine(Path.GetDirectoryName(pathName), $"{newName}.cs").Replace('\\', '/')
                );
            }
            File.WriteAllText(
                pathName,
                (string.IsNullOrWhiteSpace(template) ? defaultTemplate : template).Replace("#CLASS#", name),
                encoding
            );
            AssetDatabase.ImportAsset(pathName, ImportAssetOptions.ForceSynchronousImport);
            var assetFileName = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(pathName, ".asset"));
            var newAsset = CreateInstance<UdonSharpProgramAsset>();
            newAsset.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(pathName);
            AssetDatabase.CreateAsset(newAsset, assetFileName);
            AssetDatabase.Refresh();
        }
    }
}
