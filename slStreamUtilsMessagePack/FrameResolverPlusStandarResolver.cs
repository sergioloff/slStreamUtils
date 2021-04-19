/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace slStreamUtilsMessagePack
{
    public class FrameResolverPlusStandarResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new FrameResolverPlusStandarResolver();

        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
        {
            FrameResolver.Instance,
            StandardResolver.Instance
        };

        private FrameResolverPlusStandarResolver() { }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        private static class Cache<T>
        {
            public static IMessagePackFormatter<T> Formatter = null;

            static Cache()
            {
                foreach (var resolver in Resolvers)
                {
                    var f = resolver.GetFormatter<T>();
                    if (f != null)
                    {
                        Formatter = f;
                        return;
                    }
                }
            }
        }
    }
}
