using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Polyfills for compatibility with older versions of Unity and .NET.
    /// </summary>
    public static class Polyfills {
#if !NETSTANDARD2_1
        /// <summary>
        /// (Added in .NET Standard 2.1) Check if the string contains the specified value.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="comparationType">The comparison type.</param>
        /// <returns><c>true</c> if the string contains the value; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this string s, string value, StringComparison comparationType) =>
            s.IndexOf(value, comparationType) >= 0;

        /// <summary>
        /// (Added in .NET Standard 2.1) Try to pop an item from the stack.
        /// </summary>
        /// <typeparam name="T">The type of the item in the stack.</typeparam>
        /// <param name="stack">The stack to pop from.</param>
        /// <param name="result">The popped item.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPop<T>(this Stack<T> stack, out T result) {
            if (stack.Count == 0) {
                result = default;
                return false;
            }
            result = stack.Pop();
            return true;
        }

        /// <summary>
        /// (Added in .NET Standard 2.1) Try to dequeue an item from the queue.
        /// </summary>
        /// <typeparam name="T">The type of the item in the queue.</typeparam>
        /// <param name="queue">The queue to dequeue from.</param>
        /// <param name="result">The dequeued item.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryDequeue<T>(this Queue<T> queue, out T result) {
            if (queue.Count == 0) {
                result = default;
                return false;
            }
            result = queue.Dequeue();
            return true;
        }
#endif

#if !UNITY_2021_3_OR_NEWER
        /// <summary>
        /// (Added in Unity 2021.3) Get the position and rotation of the transform.
        /// </summary>
        /// <param name="transform">The transform to get the position and rotation from.</param>
        /// <param name="position">The position of the transform.</param>
        /// <param name="rotation">The rotation of the transform.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetPositionAndRotation(this Transform transform, out Vector3 position, out Quaternion rotation) {
            position = transform.position;
            rotation = transform.rotation;
        }

        /// <summary>
        /// (Added in Unity 2021.3) Get the local position and rotation of the transform.
        /// </summary>
        /// <param name="transform">The transform to get the local position and rotation from.</param>
        /// <param name="localPosition">The local position of the transform.</param>
        /// <param name="localRotation">The local rotation of the transform.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetLocalPositionAndRotation(this Transform transform, out Vector3 localPosition, out Quaternion localRotation) {
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
        }

        /// <summary>
        /// (Added in Unity 2021.3) Set the local position and rotation of the transform.
        /// </summary>
        /// <param name="transform">The transform to set the local position and rotation to.</param>
        /// <param name="localPosition">The local position to set.</param>
        /// <param name="localRotation">The local rotation to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLocalPositionAndRotation(this Transform transform, Vector3 localPosition, Quaternion localRotation) {
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
        }
#endif
    }
}