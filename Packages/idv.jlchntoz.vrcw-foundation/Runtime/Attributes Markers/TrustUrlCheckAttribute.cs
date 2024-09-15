using UnityEngine;

namespace JLChnToZ.VRC.Foundation {
    public enum TrustedUrlTypes {
        UnityVideo,
        AVProDesktop,
        AVProAndroid,
        AVProIOS,
        ImageUrl,
        StringUrl,
    }

    public class TrustUrlCheckAttribute : PropertyAttribute {
        public readonly TrustedUrlTypes type;

        public TrustUrlCheckAttribute(TrustedUrlTypes type) {
            this.type = type;
        }
    }
}