using System;
using Cysharp.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using JLChnToZ.VRC.Foundation.I18N;
using JLChnToZ.VRC.Foundation.I18N.Editors;

#if VPM_RESOLVER_INCLUDED
using System.Text;
using VRC.PackageManagement.Core;
using VRC.PackageManagement.Core.Types;
using VRC.PackageManagement.Core.Types.Packages;
using VRC.PackageManagement.Resolver;

using SemanticVersion = SemanticVersioning.Version;
#else
using System.IO;
using JLChnToZ.VRC.Foundation.ThirdParties.LitJson;
#endif

using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace JLChnToZ.VRC.Foundation.Editors {
    /// <summary>
    /// Self updater for VPM based packages.
    /// </summary>
    public class PackageSelfUpdater {
        static GUIContent infoContent;
        static EditorI18N i18n;
        readonly string listingsID, listingsURL;
        readonly string packageName, packageDisplayName, packageVersion;
        string availableVersion;
        bool isInstalledManually;

        /// <summary>
        /// The package name.
        /// </summary>
        public string PackageName => packageDisplayName ?? "Unknown Package";

        /// <summary>
        /// The current version of the package.
        /// </summary>
        public string CurrentVersion => packageVersion;

        /// <summary>
        /// The available version of the package.
        /// </summary>
        public string AvailableVersion => availableVersion;

        /// <summary>
        /// Whether the package is installed manually, not via VPM tools such as VCC.
        /// </summary>
        public bool IsInstalledManually => isInstalledManually;

        /// <summary>
        /// Event triggered when the version is refreshed.
        /// </summary>
        public event Action OnVersionRefreshed;

        /// <summary>
        /// Create a new instance of <see cref="PackageSelfUpdater"/>.
        /// </summary>
        /// <param name="assembly">The assembly contained in the package.</param>
        /// <param name="listingsID">The ID of the listings repository.</param>
        /// <param name="listingsURL">The URL of the listings repository.</param>
        public PackageSelfUpdater(Assembly assembly, string listingsID, string listingsURL) :
            this(PackageManagerPackageInfo.FindForAssembly(assembly), listingsID, listingsURL) { }

        /// <summary>
        /// Create a new instance of <see cref="PackageSelfUpdater"/>.
        /// </summary>
        /// <param name="packageInfo">The package info.</param>
        /// <param name="listingsID">The ID of the listings repository.</param>
        /// <param name="listingsURL">The URL of the listings repository.</param>
        public PackageSelfUpdater(PackageManagerPackageInfo packageInfo, string listingsID, string listingsURL) {
            if (i18n == null) i18n = EditorI18N.Instance;
            if (packageInfo != null) {
                packageName = packageInfo.name;
                packageDisplayName = packageInfo.displayName;
                packageVersion = packageInfo.version;
            }
            availableVersion = "";
            this.listingsID = listingsID;
            this.listingsURL = listingsURL;
        }

        /// <summary>
        /// Check the installation status in background.
        /// </summary>
        public void CheckInstallationInBackground() => UniTask.RunOnThreadPool(CheckInstallation).Forget();

        /// <summary>
        /// Check the installation status.
        /// </summary>
        public void CheckInstallation() {
            isInstalledManually = !IsPackageInVPM();
            CheckInstallationCallback().Forget();
        }

        bool IsPackageInVPM() {
            try {
                if (string.IsNullOrEmpty(packageName)) return false;
                #if VPM_RESOLVER_INCLUDED
                var allVersions = Resolver.GetAllVersionsOf(packageName);
                if (allVersions.Count > 0 && new SemanticVersion(allVersions[0]) > new SemanticVersion(packageVersion))
                    availableVersion = allVersions[0];
                var manifest = VPMProjectManifest.Load(Resolver.ProjectDir);
                return manifest.locked.ContainsKey(packageName) || manifest.dependencies.ContainsKey(packageName);
                #else
                // VPM resolver unavailable, fallback to manual detection.
                var vpmManifestPath = Path.GetFullPath("../Packages/vpm-manifest.json", Application.dataPath);
                if (!File.Exists(vpmManifestPath)) return false;
                JsonData vpmManifest;
                using (var file = File.OpenRead(vpmManifestPath))
                using (var reader = new StreamReader(file))
                    vpmManifest = JsonMapper.ToObject(new JsonReader(reader));
                if (!vpmManifest.IsObject || !vpmManifest.ContainsKey("dependencies")) return false;
                var dependencies = vpmManifest["dependencies"];
                return dependencies.IsObject && dependencies.ContainsKey(packageName);
                #endif
            } catch {
                return false;
            }
        }

        async UniTask CheckInstallationCallback() {
            await UniTask.SwitchToMainThread();
            OnVersionRefreshed?.Invoke();
        }

        /// <summary>
        /// Migrate to use VPM tools and update the package.
        /// </summary>
        /// <remarks>
        /// This method requires user interaction.
        /// </remarks>
        public void ResolveInstallation() {
            #if VPM_RESOLVER_INCLUDED
            if (!Repos.UserRepoExists(listingsID) && !Repos.AddRepo(new Uri(listingsURL)))
                return;
            #endif
            ConfirmAndUpdate();
        }

        /// <summary>
        /// Update the package.
        /// </summary>
        /// <remarks>
        /// This method requires user interaction.
        /// </remarks>
        public void ConfirmAndUpdate() {
            #if VPM_RESOLVER_INCLUDED
            if (string.IsNullOrEmpty(packageName)) {
                Debug.LogError("Unable to find package name.");
                return;
            }
            var version = availableVersion;
            if (string.IsNullOrEmpty(version)) {
                var allVersions = Resolver.GetAllVersionsOf(packageName);
                if (allVersions.Count == 0) {
                    Debug.LogError($"Unable to find any version of {packageName}.");
                    return;
                }
                version = allVersions[0];
            }
            var vrcPackage = Repos.GetPackageWithVersionMatch(packageName, version);
            var dependencies = Resolver.GetAffectedPackageList(vrcPackage);
            var sb = new StringBuilder();
            foreach (var dependency in dependencies)
                sb.AppendLine($"- {dependency}");
            if (i18n.DisplayLocalizedDialog2("PackageSelfUpdater.update_confirm", sb))
                UpdateUnchecked(vrcPackage).Forget();
            #else
            if (!isInstalledManually) return;
            switch (i18n.DisplayLocalizedDialog3("PackageSelfUpdater.update_message_no_vcc")) {
                case 1: Application.OpenURL("https://vcc.docs.vrchat.com/"); break;
                case 2: Application.OpenURL("https://vcc.docs.vrchat.com/vpm/migrating"); break;
            }
            Debug.LogError("Unable to update package. Please migrate your project to Creator Companion first.");
            #endif
        }

        /// <summary>
        /// Draw the update notifier to anywhere you want.
        /// This must be called inside an editor GUI block.
        /// </summary>
        public void DrawUpdateNotifier() {
            if (isInstalledManually) {
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                    var infoContent = GetInfoContent("PackageSelfUpdater.update_message", packageDisplayName);
                    EditorGUILayout.LabelField(infoContent, EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button(i18n.GetLocalizedContent("PackageSelfUpdater.update_message:confirm"), GUILayout.ExpandWidth(false)))
                        ResolveInstallation();
                }
            }
            if (!string.IsNullOrEmpty(availableVersion)) {
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                    var infoContent = GetInfoContent("PackageSelfUpdater.update_available", availableVersion);
                    EditorGUILayout.LabelField(infoContent, EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button(i18n.GetLocalizedContent("PackageSelfUpdater.update_available:confirm"), GUILayout.ExpandWidth(false)))
                        ConfirmAndUpdate();
                }
            }
        }

        #if VPM_RESOLVER_INCLUDED
        async UniTask UpdateUnchecked(IVRCPackage package) {
            await UniTask.Delay(500);
            Resolver.ForceRefresh();
            try {
                EditorUtility.DisplayProgressBar(
                    i18n.GetOrDefault("PackageSelfUpdater.update_progress:title"),
                    string.Format(i18n.GetOrDefault("PackageSelfUpdater.update_progress:content"), package.Id),
                    0
                );
                new UnityProject(Resolver.ProjectDir).UpdateVPMPackage(package);
            } finally {
                EditorUtility.ClearProgressBar();
            }
            Resolver.ForceRefresh();
        }
        #endif

        static GUIContent GetInfoContent(string text, params object[] args) {
            if (infoContent == null) infoContent = EditorGUIUtility.IconContent("console.infoicon");
            var content = i18n.GetLocalizedContent(text, args);
            content.image = infoContent.image;
            return content;
        }
    }
}