using System;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>Calls each handler on its own, so one throwing handler does not stop the rest.</summary>
    internal static class EventInvoker
    {
        public static void Invoke(Action handlers)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static void Invoke<T>(Action<T> handlers, T arg)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(arg);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static void Invoke<T1, T2>(Action<T1, T2> handlers, T1 arg1, T2 arg2)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<T1, T2> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(arg1, arg2);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
