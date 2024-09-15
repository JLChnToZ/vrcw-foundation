using UnityEngine;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// The types of trusted URL.
    /// </summary>
    public enum TrustedUrlTypes {
        /// <summary>This URL is for Unity built-in Video Player.</summary>
        UnityVideo,
        /// <summary>This URL is for AVPro Video Player on Desktop.</summary>
        AVProDesktop,
        /// <summary>This URL is for AVPro Video Player on Android/Quest.</summary>
        AVProAndroid,
        /// <summary>This URL is for AVPro Video Player on iOS.</summary>
        AVProIOS,
        /// <summary>This URL is for image loader.</summary>
        ImageUrl,
        /// <summary>This URL is for string loader.</summary>
        StringUrl,
    }

    /// <summary>
    /// Attach this attribute to a <c>VRCUrl</c> field to indicate that it should be checked for trusted URL.
    /// </summary>
    /// <remarks>
    /// This will also check if the URL is valid and supported by the target usage.
    /// </remarks>
    public class TrustUrlCheckAttribute : PropertyAttribute {
        public readonly TrustedUrlTypes type;

        /// <summary>
        /// Create a new instance of <c>TrustUrlCheckAttribute</c>.
        /// </summary>
        /// <param name="type">The type of trusted URL.</param>
        public TrustUrlCheckAttribute(TrustedUrlTypes type) {
            this.type = type;
        }
    }
}