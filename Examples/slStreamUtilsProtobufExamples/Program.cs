/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace slStreamUtilsProtobufExamples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string tmpPath = Path.GetTempPath();
            string fileName1 = Path.Combine(tmpPath, "BlogExamplesFile1_PB.dat");
            string fileName2 = Path.Combine(tmpPath, "BlogExamplesFile2_PB.dat");

            ProtobufCollectionSerializerExamples.Original_UnknownLengthArray_Write(fileName1);
            await ProtobufCollectionSerializerExamples.New_UnknownLengthArray_WriteAsync(fileName2);
            Console.WriteLine($"{nameof(ProtobufCollectionSerializerExamples)}.{nameof(ProtobufCollectionSerializerExamples.Original_UnknownLengthArray_Write)} -> {fileName1}");
            Console.WriteLine($"{nameof(ProtobufCollectionSerializerExamples)}.{nameof(ProtobufCollectionSerializerExamples.New_UnknownLengthArray_WriteAsync)} -> {fileName2}");
            X[] PB_ar1 = ProtobufCollectionDeserializerExamples.Original_UnknownLengthArray_Read(fileName1).ToArray();
            X[] PB_ar2 = await ProtobufCollectionDeserializerExamples.New_UnknownLengthArray_ReadAsync(fileName2).ToArrayAsync();
            Console.WriteLine($"{fileName1} -> {nameof(ProtobufCollectionDeserializerExamples)}.{nameof(ProtobufCollectionDeserializerExamples.Original_UnknownLengthArray_Read)}");
            Console.WriteLine($"{fileName2} -> {nameof(ProtobufCollectionDeserializerExamples)}.{nameof(ProtobufCollectionDeserializerExamples.New_UnknownLengthArray_ReadAsync)}");
            Console.WriteLine($"Arrays are equal={PB_ar1.SequenceEqual(PB_ar2)}");
            Console.WriteLine();
        }

        private static bool FilesAreEqual(string fileName1, string fileName2)
        {
            try
            {
                return File.ReadAllBytes(fileName1).SequenceEqual(File.ReadAllBytes(fileName2));
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }
    }
}
