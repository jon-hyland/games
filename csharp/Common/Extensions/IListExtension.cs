using System.Collections.Generic;

namespace Common.Extensions
{
    public static class IListExtension
    {
        public static T RemoveAndGet<T>(this IList<T> list, int index)
        {
            lock (list)
            {
                T value = list[index];
                list.RemoveAt(index);
                return value;
            }
        }

        public static T Dequeue<T>(this IList<T> list)
        {
            lock (list)
            {
                T value = list[0];
                list.RemoveAt(0);
                return value;
            }
        }

        public static T[] Dequeue<T>(this IList<T> list, int count)
        {
            lock (list)
            {
                if (count > list.Count)
                    count = list.Count;
                T[] value = new T[count];
                for (int i = 0; i < count; i++)
                {
                    value[i] = list[0];
                    list.RemoveAt(0);
                }
                return value;
            }
        }
    }
}
