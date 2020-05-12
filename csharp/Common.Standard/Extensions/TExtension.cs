using System.Linq;

namespace Common.Standard.Extensions
{
    public static class TExtension
    {
        /// <summary>
        /// Returns true if value in params.
        /// Usage: if (value.In(1, 2))
        /// </summary>
        public static bool In<T>(this T obj, params T[] args)
        {
            return args.Contains(obj);
        }
    }
}
