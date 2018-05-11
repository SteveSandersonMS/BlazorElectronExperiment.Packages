// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace WebAssembly
{
    internal static class Runtime
    {
        internal static IJavaScriptRuntime Current;
    }

    internal class MonoWebAssemblyRuntime : IJavaScriptRuntime
    {
        // The exact namespace, type, and method names must match the corresponding entry in
        // driver.c in the Mono distribution

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern TRes InvokeJS<T0, T1, T2, TRes>(out string exception, string funcName, T0 arg0, T1 arg1, T2 arg2);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern TRes InvokeJSArray<TRes>(out string exception, string funcName, params object[] args);

        TRes IJavaScriptRuntime.InvokeJS<T0, T1, T2, TRes>(out string exception, string funcName, T0 arg0, T1 arg1, T2 arg2)
        {
            return InvokeJS<T0, T1, T2, TRes>(out exception, funcName, arg0, arg1, arg2);
        }

        TRes IJavaScriptRuntime.InvokeJSArray<TRes>(out string exception, string funcName, params object[] args)
        {
            return InvokeJSArray<TRes>(out exception, funcName, args);
        }
    }

    /// <summary>
    /// Invokes JS
    /// </summary>
    public interface IJavaScriptRuntime
    {
        /// <summary>
        /// Invokes JS
        /// </summary>
        TRes InvokeJS<T0, T1, T2, TRes>(out string exception, string funcName, T0 arg0, T1 arg1, T2 arg2);

        /// <summary>
        /// Invokes JS
        /// </summary>
        TRes InvokeJSArray<TRes>(out string exception, string funcName, params object[] args);
    }
}
