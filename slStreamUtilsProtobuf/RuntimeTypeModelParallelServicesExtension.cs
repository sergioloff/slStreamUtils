/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using slStreamUtils;
using System;

namespace slStreamUtilsProtobuf
{
    public static class RuntimeTypeModelParallelServicesExtension
    {
        public static void RegisterParallelServices<T>(this ProtoBuf.Meta.RuntimeTypeModel rtm)
        {
            rtm.Add(typeof(ParallelServices_ListWrapper<T>));
            rtm.Add(typeof(ParallelServices_ArrayWrapper<T>));
        }

        internal static void SetupParallelServices<T>(this ProtoBuf.Meta.TypeModel model)
        {
            if (!model.IsDefined(typeof(ParallelServices_ListWrapper<T>)) ||
                !model.IsDefined(typeof(ParallelServices_ArrayWrapper<T>)))
            {
                if (model is ProtoBuf.Meta.RuntimeTypeModel rtmodel)
                {
                    try
                    {
                        rtmodel.Add(typeof(ParallelServices_ListWrapper<T>));
                        rtmodel.Add(typeof(ParallelServices_ArrayWrapper<T>));
                    }
                    catch (Exception e)
                    {
                        throw new StreamSerializationException($"Failed to register parallel types in given model. Suggestion: check if the model is frozen. If it was a pre-compiled model, make sure to call {nameof(RuntimeTypeModelParallelServicesExtension)}.{nameof(RuntimeTypeModelParallelServicesExtension.RegisterParallelServices)} before compilation", e);
                    }
                }
                else
                {
                    throw new StreamSerializationException($"Parallel types not present in given model. Suggestion: Initialize the parallel functionality by calling {nameof(RuntimeTypeModelParallelServicesExtension)}.{nameof(RuntimeTypeModelParallelServicesExtension.RegisterParallelServices)}");
                }
            }
        }
    }

}
