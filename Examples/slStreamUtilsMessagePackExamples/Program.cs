/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace slStreamUtilsMessagePackExamples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string tmpPath = Path.GetTempPath();
            string fileName1 = Path.Combine(tmpPath, "BlogExamplesFile1_MP.dat");
            string fileName2 = Path.Combine(tmpPath, "BlogExamplesFile2_MP.dat");

            await MsgPackSerializerExamples.Original_UnknownLengthArray_WriteAsync(fileName1);
            await MsgPackSerializerExamples.New_UnknownLengthArray_WriteAsync(fileName2);
            Console.WriteLine($"{nameof(MsgPackSerializerExamples)}.{nameof(MsgPackSerializerExamples.Original_UnknownLengthArray_WriteAsync)} -> {fileName1}");
            Console.WriteLine($"{nameof(MsgPackSerializerExamples)}.{nameof(MsgPackSerializerExamples.New_UnknownLengthArray_WriteAsync)} -> {fileName2}");
            X[] MP_ar1 = await MsgPackDeserializerExamples.Original_UnknownLengthArray_ReadAsync(fileName1).ToArrayAsync();
            X[] MP_ar2 = await MsgPackDeserializerExamples.New_UnknownLengthArray_ReadAsync(fileName2).ToArrayAsync();
            Console.WriteLine($"{fileName1} -> {nameof(MsgPackDeserializerExamples)}.{nameof(MsgPackDeserializerExamples.Original_UnknownLengthArray_ReadAsync)}");
            Console.WriteLine($"{fileName2} -> {nameof(MsgPackDeserializerExamples)}.{nameof(MsgPackDeserializerExamples.New_UnknownLengthArray_ReadAsync)}");
            Console.WriteLine($"Arrays equal={MP_ar1.SequenceEqual(MP_ar2)}");
            Console.WriteLine();

            await MsgPackSerializerExamples.Original_KnownLengthArray_WriteAsync(fileName1);
            await MsgPackSerializerExamples.New_KnownLengthArray_WriteAsync(fileName2);
            Console.WriteLine($"{nameof(MsgPackSerializerExamples)}.{nameof(MsgPackSerializerExamples.Original_KnownLengthArray_WriteAsync)} -> {fileName1}");
            Console.WriteLine($"{nameof(MsgPackSerializerExamples)}.{nameof(MsgPackSerializerExamples.New_KnownLengthArray_WriteAsync)} -> {fileName2}");
            ArrayX MP_obj1 = await MsgPackDeserializerExamples.Original_KnownLengthArray_ReadAsync(fileName1);
            ArrayX MP_obj2 = await MsgPackDeserializerExamples.New_KnownLengthArray_ReadAsync(fileName2);
            Console.WriteLine($"{fileName1} -> {nameof(MsgPackDeserializerExamples)}.{nameof(MsgPackDeserializerExamples.Original_KnownLengthArray_ReadAsync)}");
            Console.WriteLine($"{fileName2} -> {nameof(MsgPackDeserializerExamples)}.{nameof(MsgPackDeserializerExamples.New_KnownLengthArray_ReadAsync)}");
            Console.WriteLine($"Objects are equal={MP_obj1.Equals(MP_obj2)}");
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
