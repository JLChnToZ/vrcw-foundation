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
#else
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using JLChnToZ.VRC.Foundation.ThirdParties.LitJson;
#endif

using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace JLChnToZ.VRC.Foundation.Editors {
    /// <summary>
    /// Self updater for VPM based packages.
    /// </summary>
    public class PackageSelfUpdater {
        static PackageSelfUpdater selfInstance;
        static GUIContent infoContent;
        static EditorI18N i18n;
#if !VPM_RESOLVER_INCLUDED
        static Dictionary<string, UniTask<Dictionary<string, (Semver release, Semver preRelease)>>> listingCache = new Dictionary<string, UniTask<Dictionary<string, (Semver, Semver)>>>();
        static Dictionary<string, UniTask<(Semver release, Semver preRelease)>> versionCache = new Dictionary<string, UniTask<(Semver, Semver)>>();
#endif
        readonly string listingsID, listingsURL;
        readonly string packageName, packageDisplayName, packageVersion;
        string availableVersion;
        bool isInstalledManually;
        bool enableSelfCheck = true;

        /// <summary>
        /// The self instance of <see cref="PackageSelfUpdater"/>,
        /// which handles the self-update of the package containing this class.
        /// </summary>
        public static PackageSelfUpdater SelfInstance {
            get {
                if (selfInstance == null) {
                    selfInstance = new PackageSelfUpdater(
                        typeof(PackageSelfUpdater).Assembly,
                        "idv.jlchntoz.xtlcdn-listing",
                        "https://xtlcdn.github.io/vpm/index.json"
                    );
                    selfInstance.CheckInstallationInBackground();
                }
                return selfInstance;
            }
        }

        /// <summary>
        /// The package name.
        /// </summary>
        public string PackageName => string.IsNullOrEmpty(packageDisplayName) ? "Unknown Package" : packageDisplayName;

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
        /// Whether to enable self-check for updates.
        /// </summary>
        public bool EnableSelfCheck {
            get => enableSelfCheck;
            set => enableSelfCheck = value;
        }

        /// <summary>
        /// Event triggered when the version is refreshed.
        /// </summary>
        public event Action OnVersionRefreshed;

#if !VPM_RESOLVER_INCLUDED
        static UniTask<(Semver release, Semver preRelease)> GetVersions(string listingsURL, string packageName) {
            if (!versionCache.TryGetValue(listingsURL, out var task))
                versionCache[listingsURL] = task = GetVersionsCore(listingsURL, packageName).Preserve();
            return task;
        }

        static async UniTask<(Semver release, Semver preRelease)> GetVersionsCore(string listingsURL, string packageName) {
            var listing = await GetListing(listingsURL);
            if (!listing.TryGetValue(packageName, out var versions)) return default;
            return versions;
        }

        static UniTask<Dictionary<string, (Semver release, Semver preRelease)>> GetListing(string listingsURL) {
            if (!listingCache.TryGetValue(listingsURL, out var task))
                listingCache[listingsURL] = task = GetListingCore(listingsURL).Preserve();
            return task;
        }

        static async UniTask<Dictionary<string, (Semver release, Semver preRelease)>> GetListingCore(string listingsURL) {
            await UniTask.SwitchToMainThread();
            var req = UnityWebRequest.Get(listingsURL);
            var results = new Dictionary<string, (Semver, Semver)>();
            await req.SendWebRequest();
            var result = JsonMapper.ToObject(req.downloadHandler.text);
            if (!result.ContainsKey("packages")) throw new Exception("Invalid listings repository format.");
            foreach (DictionaryEntry package in result["packages"] as IDictionary) {
                var packageName = package.Key.ToString();
                if (!(package.Value is JsonData value) ||
                    !value.IsObject ||
                    !value.ContainsKey("versions"))
                    continue;
                var versions = value["versions"];
                Semver release = default, preRelease = default;
                foreach (var version in versions.Keys)
                    if (Semver.TryParse(version, out var semver)) {
                        if (!semver.IsPrerelease && semver > release)
                            release = semver;
                        if (semver > preRelease)
                            preRelease = semver;
                    }
                results[packageName] = (release, preRelease);
            }
            return results;
        }
#endif

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
                if (string.IsNullOrWhiteSpace(packageDisplayName))
                    packageDisplayName = packageName;
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
            QueryVersionAndCheck().Forget();
        }

        bool IsPackageInVPM() {
            try {
                if (string.IsNullOrEmpty(packageName)) return false;
#if VPM_RESOLVER_INCLUDED
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
                if (!vpmManifest.IsObject) return false;
                if (vpmManifest.ContainsKey("dependencies")) {
                    var dependencies = vpmManifest["dependencies"];
                    if (dependencies.IsObject && dependencies.ContainsKey(packageName)) return true;
                }
                if (vpmManifest.ContainsKey("locked")) {
                    var lockedDependencies = vpmManifest["locked"];
                    if (lockedDependencies.IsObject && lockedDependencies.ContainsKey(packageName)) return true;
                }
                return false;
#endif
            } catch {
                return false;
            }
        }

        async UniTask QueryVersionAndCheck() {
            try {
                if (!isInstalledManually) {
#if VPM_RESOLVER_INCLUDED
                    Semver packageSemver = packageVersion, latestVersion = default;
                    bool isPrerelease = packageSemver.IsPrerelease;
                    foreach (var version in Resolver.GetAllVersionsOf(packageName))
                        if (Semver.TryParse(version, out var semver) && semver > latestVersion && (isPrerelease || !semver.IsPrerelease))
                            latestVersion = semver;
                    if (latestVersion > packageSemver)
                        availableVersion = latestVersion.ToString();
#else
                    var (latestRelease, lastestPreRelease) = await GetVersions(listingsURL, packageName);
                    Semver currentVersion = packageVersion;
                    if (currentVersion.IsPrerelease && lastestPreRelease > currentVersion)
                        availableVersion = lastestPreRelease.ToString();
                    else if (latestRelease > currentVersion)
                        availableVersion = latestRelease.ToString();
#endif
                }
            } catch (Exception e) {
                Debug.LogError($"Failed to check for updates: {e.Message}");
            }
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
                return;
            }
            if (!string.IsNullOrEmpty(availableVersion)) {
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                    var infoContent = GetInfoContent("PackageSelfUpdater.update_available", availableVersion, packageDisplayName);
                    EditorGUILayout.LabelField(infoContent, EditorStyles.wordWrappedLabel);
#if VPM_RESOLVER_INCLUDED
                    if (GUILayout.Button(i18n.GetLocalizedContent("PackageSelfUpdater.update_available:confirm"), GUILayout.ExpandWidth(false)))
                        ConfirmAndUpdate();
#endif
                }
                return;
            }
            if (enableSelfCheck && this != selfInstance) SelfInstance.DrawUpdateNotifier();
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