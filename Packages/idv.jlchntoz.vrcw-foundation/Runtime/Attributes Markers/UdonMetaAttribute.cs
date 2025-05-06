using System;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Assigns a UdonSharp field with a specific metadata on current UdonBehaviour.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class UdonMetaAttribute : PropertyAttribute {
        /// <summary>
        /// The type of metadata that can be assigned to a UdonSharp field.
        /// </summary>
        public UdonMetaAttributeType Type { get; private set; }
        public UdonMetaAttribute(UdonMetaAttributeType type) => Type = type;
    }

    /// <summary>
    /// Defines the type of metadata that can be assigned to a UdonSharp field.
    /// </summary>
    public enum UdonMetaAttributeType {
        /// <summary>
        /// This will set the field to the network ID of the current game object.
        /// </summary>
        /// <remarks>
        /// This field must be an <c>int</c> type.
        /// This value is not accurate if current object is instaniated (cloned) in runtime,
        /// including use of <see cref="global::VRC.SDK3.Components.VRCPlayerObject"/>.
        /// </remarks>
        NetworkID,
        /// <summary>
        /// It will sets the field to <c>true</c> if current object is set to not be synced over network.
        /// </summary>
        /// <remarks>This field must be a <see cref="bool"/> type.</remarks>
        NetworkSyncModeNone,
        /// <summary>
        /// It will sets the field to <c>true</c> if current object is set to be continuously (auto) synced over network.
        /// </summary>
        /// <remarks>This field must be a <see cref="bool"/> type.</remarks>
        NetworkSyncModeContinuous,
        /// <summary>
        /// It will sets the field to <c>true</c> if current object is set to be manually synced over network.
        /// </summary>
        /// <remarks>This field must be a <see cref="bool"/> type.</remarks>
        NetworkSyncModeManual,
        /// <summary>
        /// It will sets the field to correponding integer value of sync mode of current object.
        /// </summary>
        /// <remarks>
        /// This field must be a <see cref="int"/> type.
        /// The value is one of the integer value in <see cref="global::VRC.SDKBase.Networking.SyncType"/>.
        /// </remarks>
        NetworkSyncMode,
        /// <summary>
        /// This will set the field to the build time of current scene.
        /// </summary>
        /// <remarks>
        /// This field must be a <see cref="DateTime"/> or <see cref="string"/> type.
        /// </remarks>
        BuiltTimeStamp,
        /// <summary>
        /// This will set the field to the world blueprint ID of current scene.
        /// </summary>
        /// <remarks>
        /// This field must be a <see cref="string"/> type.
        /// </remarks>
        WorldBlueprintID,
    }
}