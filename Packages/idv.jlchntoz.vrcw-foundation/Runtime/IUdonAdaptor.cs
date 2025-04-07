using UnityEngine;
using VRC.Udon;
using UdonSharp;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Allow declared preprocessor attributes for <see cref="UdonSharpBehaviour"/> usable in editor-only <see cref="MonoBehaviour"/>.
    /// </summary>
    public interface IUdonAdaptor {
        /// <summary>
        /// The target <see cref="UdonBehaviour"/> of this <see cref="MonoBehaviour"/>.
        /// </summary>
        UdonBehaviour TargetBehaviour { get; }

        /// <summary>
        /// The target <see cref="UdonSharpBehaviour"/> of this <see cref="MonoBehaviour"/>.
        /// </summary>
        UdonSharpBehaviour TargetUdonSharpBehaviour { get; }
    }
}