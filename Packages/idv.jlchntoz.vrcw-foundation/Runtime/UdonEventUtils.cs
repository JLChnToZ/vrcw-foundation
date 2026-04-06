using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor.Events;
#endif
using VRC.Udon;
using UdonSharp;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Utility methods for working with Udon behaviours and events.
    /// </summary>
    public static class UdonEventUtils {
        const BindingFlags instancePublicNonPublic = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo backingUdonBehaviourField;
        static readonly MethodInfo sendEventMethod, sendMessageMethod;
        static readonly ConditionalWeakTable<MonoBehaviour, UnityAction<string>> sendEventActionCache;

        static UdonEventUtils() {
            sendEventActionCache = new ConditionalWeakTable<MonoBehaviour, UnityAction<string>>();
            backingUdonBehaviourField = typeof(UdonSharpBehaviour)
                .GetField("_udonSharpBackingUdonBehaviour", instancePublicNonPublic);
            sendEventMethod = typeof(UdonBehaviour)
                .GetMethod(nameof(UdonBehaviour.SendCustomEvent), instancePublicNonPublic);
            sendMessageMethod = typeof(Component)
                .GetMethod(nameof(Component.SendMessage), instancePublicNonPublic, null, new[] { typeof(string) }, null);
        }

        static UnityAction<string> CreateSendEventAction(MonoBehaviour instance) {
            if (instance is UdonBehaviour ub) {
            } else if (instance is UdonSharpBehaviour usb)
                ub = backingUdonBehaviourField.GetValue(usb) as UdonBehaviour;
            else
                return Delegate.CreateDelegate(typeof(UnityAction<string>), instance, sendMessageMethod, false) as UnityAction<string>;
            return ub == null ? null : Delegate.CreateDelegate(typeof(UnityAction<string>), ub, sendEventMethod, false) as UnityAction<string>;
        }

        /// <summary>
        /// Gets a <see cref="UnityAction{String}"/> that can be used to send events to the specified <see cref="MonoBehaviour"/> instance.
        /// </summary>
        /// <param name="instance">The <see cref="MonoBehaviour"/> instance to get the send event action for.</param>
        /// <returns>A <see cref="UnityAction{String}"/> that can be used to send events to the specified instance, or <c>null</c> if no compatible method is found.</returns>
        /// <remarks>
        /// If the instance is an <see cref="UdonBehaviour"/> or an <see cref="UdonSharpBehaviour"/>,
        /// the returned action will use <see cref="UdonBehaviour.SendCustomEvent"/> to send events.
        /// Otherwise, it will use <see cref="Component.SendMessage(string)"/> to send events.
        /// </remarks>
        public static UnityAction<string> GetSendEventAction(this MonoBehaviour instance) =>
            sendEventActionCache.GetValue(instance, CreateSendEventAction);

        /// <summary>
        /// Adds a listener to a <see cref="UnityEventBase"/> that sends an event to the specified <see cref="MonoBehaviour"/> instance when invoked.
        /// </summary>
        /// <param name="unityEvent">The <see cref="UnityEventBase"/> to add the listener to.</param>
        /// <param name="target">The <see cref="MonoBehaviour"/> instance to send events to when the listener is invoked.</param>
        /// <param name="eventName">The name of the event to send to the instance when the listener is invoked.</param>
        /// <remarks>
        /// If <paramref name="target"/> has a compatible method for sending events
        /// (either <see cref="UdonBehaviour.SendCustomEvent"/> or <see cref="Component.SendMessage(string)"/>),
        /// the listener will be added to the <paramref name="unityEvent"/> to send the specified event to the instance when invoked.
        /// </remarks>
        public static void AddUdonEventListener(this UnityEventBase unityEvent, MonoBehaviour target, string eventName) {
            var action = target.GetSendEventAction();
            if (action == null) {
                Debug.LogError($"Failed to bind event: {target} does not have a compatible SendMessage or SendCustomEvent method.");
                return;
            }
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                UnityEventTools.AddStringPersistentListener(unityEvent, action, eventName);
                return;
            }
#endif
            Debug.LogWarning($"Binding events at runtime is not supported in the editor. Attempting to bind {eventName} on {target}.");
        }
    }
}