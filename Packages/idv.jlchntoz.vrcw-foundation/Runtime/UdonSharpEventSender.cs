﻿using System;
using UnityEngine;
using UdonSharp;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Attach this attribute to a field of <see cref="UdonSharpEventSender"/> based componet to bind the event on build.
    /// </summary>
    /// <remarks>
    /// This is equivalent to calling <see cref="UdonSharpEventSender._AddListener"/> in runtime but done in build time.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class BindUdonSharpEventAttribute : Attribute {}

    public abstract class UdonSharpEventSender : UdonSharpBehaviour {
        [SerializeField] protected UdonSharpBehaviour[] targets;

        public void _AddListener(UdonSharpBehaviour callback) {
            if (callback == null) return;
            if (targets == null) {
                targets = new UdonSharpBehaviour[] { callback };
                return;
            }
            if (Array.IndexOf(targets, callback) >= 0) return;
            var temp = new UdonSharpBehaviour[targets.Length + 1];
            Array.Copy(targets, temp, targets.Length);
            temp[targets.Length] = callback;
            targets = temp;
        }

        protected void SendEvent(string name) {
            if (targets == null) return;
            Debug.Log($"[{GetUdonTypeName()}] Send Event {name}");
            foreach (var ub in targets) if (ub != null) ub.SendCustomEvent(name);
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