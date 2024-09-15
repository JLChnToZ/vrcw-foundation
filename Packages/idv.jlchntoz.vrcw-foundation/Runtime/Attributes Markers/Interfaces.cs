using UdonSharp;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// A singleton interface for UdonSharpBehaviour.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    /// You will need to implement this within <c>#IF !COMPILER_UDONSHARP</c> block or the UdonSharp compiler will throw error.
    /// </remarks>
    public interface ISingleton<T> where T : UdonSharpBehaviour, ISingleton<T> {
        /// <summary>
        /// This method will be called when pre-processor gathers all other instances and before removing them.
        /// </summary>
        /// <param name="others">All other instances of this singleton.</param>
        /// <remarks>
        /// Put your merge logic here.
        /// </remarks>
        void Merge(T[] others);
    }

    /// <summary>
    /// Indicates this component is a pre-processor with priority.
    /// </summary>
    public interface IPrioritizedPreProcessor {
        /// <summary>
        /// The priority of this pre-processor.
        /// </summary>
        int Priority { get; }
    }

    /// <summary>
    /// Indicates this component is a pre-processor that will process itself.
    /// </summary>
    public interface ISelfPreProcess : IPrioritizedPreProcessor {
        /// <summary>
        /// Put your pre-process logic here.
        /// </summary>
        void PreProcess();
    }
}