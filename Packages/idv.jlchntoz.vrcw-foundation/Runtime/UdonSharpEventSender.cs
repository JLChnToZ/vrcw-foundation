using System;
using UnityEngine;
using VRC.SDKBase;
using UdonSharp;
using VRC.SDK3.Data;
using VRC.Udon;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Attach this attribute to a field of <see cref="UdonSharpEventSender"/> based componet to bind the event on build.
    /// </summary>
    /// <remarks>
    /// This is equivalent to calling <see cref="UdonSharpEventSender._AddListener"/> in runtime but done in build time.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class BindUdonSharpEventAttribute : Attribute {
        internal readonly string[] bindEventNames;

        /// <summary>
        /// Bind the event sender to the target field.
        /// </summary>
        /// <param name="bindEventNames">
        /// The names of the events to bind.
        /// If no event names are provided, all events will be bound.<br/>
        /// Recommended to provide event names when you have many events but only a few of them need to be listened,
        /// as this can reduce the number of events sent and improve performance.
        /// </param>
        public BindUdonSharpEventAttribute(params string[] bindEventNames) {
            this.bindEventNames = bindEventNames;
        }
    }

    public abstract class UdonSharpEventSender : UdonSharpBehaviour {
        /// <summary>
        /// All callback listeners.
        /// </summary>
        [SerializeField] protected UdonSharpBehaviour[] targets;
        /// <summary>
        /// Whether to log the events sent.
        /// </summary>
        [SerializeField] protected bool logEvents = false;
        [HideInInspector] public DataDictionary namedTargets;

        /// <summary>
        /// Add a listener to the event.
        /// </summary>
        /// <param name="callback">The callback listener.</param>
        public void _AddListener(UdonSharpBehaviour callback) {
            if (!Utilities.IsValid(callback)) return;
            if (!Utilities.IsValid(targets)) {
                targets = new UdonSharpBehaviour[] { callback };
                return;
            }
            if (Array.IndexOf(targets, callback) >= 0) return;
            var temp = new UdonSharpBehaviour[targets.Length + 1];
            Array.Copy(targets, temp, targets.Length);
            temp[targets.Length] = callback;
            targets = temp;
        }

        /// <summary>
        /// Add a listener to the event with a specific event name.
        /// The event will be sent when <see cref="SendEvent(string)"/> is called with the same event name.
        /// </summary>
        /// <param name="callback">The callback listener.</param>
        /// <param name="eventName">The event name.</param>
        /// <remarks>
        /// This can reduces the number of event sent and improve performance when you have many events
        /// but only a few of them need to be listened, as the event will only be sent to the listeners that are interested in the event.
        /// </remarks>
        public void _AddListener(UdonSharpBehaviour callback, string eventName) {
            if (!Utilities.IsValid(callback)) return;
            if (!Utilities.IsValid(namedTargets)) namedTargets = new DataDictionary();
            DataToken eventNameToken = eventName;
            if (!namedTargets.TryGetValue(eventNameToken, out var dt)) {
                namedTargets[eventNameToken] = callback;
                return;
            }
            DataList dtList;
            switch (dt.TokenType) {
                case TokenType.Reference:
                    if (dt == callback) return;
                    dtList = new DataList();
                    dtList.Add(dt);
                    dtList.Add(callback);
                    namedTargets[eventNameToken] = dtList;
                    return;
                case TokenType.DataList:
                    dtList = dt.DataList;
                    if (dtList.Contains(callback)) return;
                    dtList.Add(callback);
                    return;
            }
        }

        /// <summary>
        /// Send an event to all targets.
        /// </summary>
        /// <param name="name">The event name.</param>
        protected void SendEvent(string name) {
            if (logEvents) Debug.Log($"[{GetUdonTypeName()}] Send Event {name}");
            if (Utilities.IsValid(namedTargets) && namedTargets.TryGetValue(name, out var dt))
                switch (dt.TokenType) {
                    case TokenType.Reference:
                        var target = (UdonBehaviour)dt.Reference;
                        if (Utilities.IsValid(target)) target.SendCustomEvent(name);
                        break;
                    case TokenType.DataList:
                        var dtList = dt.DataList;
                        for (int i = 0, count = dtList.Count; i < count; i++) {
                            var targetI = (UdonBehaviour)dtList[i].Reference;
                            if (Utilities.IsValid(targetI)) targetI.SendCustomEvent(name);
                        }
                        break;
                }
            if (Utilities.IsValid(targets))
                foreach (var ub in targets)
                    if (Utilities.IsValid(ub))
                        ub.SendCustomEvent(name);
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        /// <summary>
        /// Merge the targets of the singletons.
        /// </summary>
        /// <param name="singletons">The singletons to merge.</param>
        /// <remarks>
        /// This is for use in combination of <see cref="ISingleton.Merge(T[])"/> and is for editor only.
        /// </remarks>
        protected static void MergeTargets(UdonSharpEventSender[] singletons) {
            UdonSharpEventSender first = null;
            int count = 0;
            foreach (var singleton in singletons) {
                if (singleton == null) continue;
                if (singleton.targets == null) continue;
                if (first == null) first = singleton;
                count += singleton.targets.Length;
            }
            if (count < 1) return;
            var targets = new UdonSharpBehaviour[count];
            count = 0;
            foreach (var singleton in singletons) {
                if (singleton == null) continue;
                if (singleton.targets == null) continue;
                Array.Copy(singleton.targets, 0, targets, count, singleton.targets.Length);
                count += singleton.targets.Length;
            }
            first.targets = targets;
        }
#endif
    }
}