using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ct3d.Support
{
    static class Extensions
    {
        public static bool Between(this double val, double min, double max) => val >= min && val <= max;
        public static bool Between(this float val, float min, float max) => val >= min && val <= max;

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
                list.Add(item);
        }

        public static void AddRange<T>(this ISet<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
                list.Add(item);
        }
    }
}
