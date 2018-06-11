﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using WebAssembly;

namespace MonoSanityClient
{
    public static class Examples
    {
        static Examples()
        {
            Runtime.Current = new MonoWebAssemblyRuntime();
        }

        public static string AddNumbers(int a, int b)
            => (a + b).ToString();

        public static string RepeatString(string str, int count)
        {
            var result = new StringBuilder();

            for (var i = 0; i < count; i++)
            {
                result.Append(str);
            }

            return result.ToString();
        }

        public static void TriggerException(string message)
        {
            throw new InvalidOperationException(message);
        }

        public static string EvaluateJavaScript(string expression)
        {
            // For tests that call this method, we'll exercise the 'InvokeJSArray' code path
            var result = Runtime.Current.InvokeJSArray<string>(out var exceptionMessage, "evaluateJsExpression", expression, null, null);
            if (exceptionMessage != null)
            {
                return $".NET got exception: {exceptionMessage}";
            }

            return $".NET received: {(result ?? "(NULL)")}";
        }

        public static string CallJsNoBoxing(int numberA, int numberB)
        {
            // For tests that call this method, we'll exercise the 'InvokeJS' code path
            // since that doesn't box the params
            var result = Runtime.Current.InvokeJS<int, int, object, int>(out var exceptionMessage, "divideNumbersUnmarshalled", numberA, numberB, null);
            if (exceptionMessage != null)
            {
                return $".NET got exception: {exceptionMessage}";
            }

            return $".NET received: {result}";
        }

        public static string GetRuntimeInformation()
            => $"OSDescription: '{RuntimeInformation.OSDescription}';"
            + $" OSArchitecture: '{RuntimeInformation.OSArchitecture}';"
            + $" IsOSPlatform(WEBASSEMBLY): '{RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY"))}'";
    }
}
