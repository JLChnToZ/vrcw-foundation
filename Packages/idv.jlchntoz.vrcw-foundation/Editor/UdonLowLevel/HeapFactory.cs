using System;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace JLChnToZ.VRC.Foundation.UdonLowLevel {
    internal class HeapFactory : IUdonHeapFactory {
        public uint DefaultHeapSize { get; set; }

        public HeapFactory(uint defaultHeapSize = 512U) => DefaultHeapSize = defaultHeapSize;

        public IUdonHeap ConstructUdonHeap() => new UdonHeap(DefaultHeapSize);

        IUdonHeap IUdonHeapFactory.ConstructUdonHeap(uint heapSize) => new UdonHeap(Math.Max(heapSize, DefaultHeapSize));
    }
}