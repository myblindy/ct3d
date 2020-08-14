using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ct3d.Support
{
    static class EnumerableExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> generator) =>
            dict.TryGetValue(key, out var val) ? val : (dict[key] = generator(key));
    }
}
