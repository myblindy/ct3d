using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ct3d.Support
{
    class EnumArray<TKey, TValue> : IEnumerable<TValue> where TKey : unmanaged, Enum
    {
        readonly TValue[] mapping = new TValue[(int)(object)Enum.GetValues<TKey>().Max()];

        public unsafe void Add(TKey key, TValue value) => mapping[*(int*)&key] = value;

        public unsafe TValue this[TKey key] { get => mapping[*(int*)&key]; set => mapping[*(int*)&key] = value; }

        public IEnumerator<TValue> GetEnumerator() => mapping.Cast<TValue>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
