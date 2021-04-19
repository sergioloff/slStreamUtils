/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using MessagePack.Formatters;
using slStreamUtilsMessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace slStreamUtilsMessagePack
{
    public class FrameResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new FrameResolver();

        private FrameResolver() { }

        public IMessagePackFormatter<TArray> GetFormatter<TArray>()
        {
            return FormatterCache<TArray>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static IMessagePackFormatter<T> Formatter;
            static FormatterCache()
            {
                Type t = typeof(T);
                TypeInfo ti = t.GetTypeInfo();
                // Frame<T>
                if (ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(Frame<>))
                {
                    Type tInner = t.GetTypeInfo().GenericTypeArguments[0];
                    Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(
                        typeof(FrameItemFormatter<>).MakeGenericType(tInner), new object[] { });
                }
                // Frame<T>[]
                if (t.IsArray && t.HasElementType && t.GetArrayRank() == 1)
                {
                    Type tPI = t.GetElementType();
                    if (tPI.IsGenericType && tPI.GetGenericTypeDefinition() == typeof(Frame<>))
                    {
                        Type tInner = tPI.GetTypeInfo().GenericTypeArguments[0];
                        Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(
                            typeof(FrameArrayFormatter<,>).MakeGenericType(t, tInner), new object[] { });
                    }
                }
                // List<Frame<T>>
                if (ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type tPI = t.GetTypeInfo().GenericTypeArguments[0];
                    if (tPI.IsGenericType && tPI.GetGenericTypeDefinition() == typeof(Frame<>))
                    {
                        Type tInner = tPI.GetTypeInfo().GenericTypeArguments[0];
                        Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(
                            typeof(FrameListFormatter<,>).MakeGenericType(t, tInner), new object[] { });
                    }
                }
            }
        }
    }
}
