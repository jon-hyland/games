using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class ActionExtension
    {
        public static void InvokeFromTask(this Action action)
        {
            if (action != null)
                Task.Factory.StartNew(() => action.Invoke(), new CancellationToken());
        }

        public static void InvokeFromTask<T>(this Action<T> action, T data)
        {
            if (action != null)
                Task.Factory.StartNew(() => action.Invoke(data), new CancellationToken());
        }

        public static void InvokeFromTask<T1, T2>(this Action<T1, T2> action, T1 data1, T2 data2)
        {
            if (action != null)
                Task.Factory.StartNew(() => action.Invoke(data1, data2), new CancellationToken());
        }

        public static void InvokeFromTask<T1, T2, T3>(this Action<T1, T2, T3> action, T1 data1, T2 data2, T3 data3)
        {
            if (action != null)
                Task.Factory.StartNew(() => action.Invoke(data1, data2, data3), new CancellationToken());
        }

        public static void InvokeFromTask<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> action, T1 data1, T2 data2, T3 data3, T4 data4)
        {
            if (action != null)
                Task.Factory.StartNew(() => action.Invoke(data1, data2, data3, data4), new CancellationToken());
        }
    }
}
