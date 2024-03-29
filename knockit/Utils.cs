﻿using System;

namespace knockit
{
    internal class Utils
    {
        public static void RaiseEvent<T>(EventHandler<T> handler, object sender, T eventArgs)
            where T : EventArgs
        {
            if (handler != null)
            {
                handler(sender, eventArgs);
            }
        }
    }
}
