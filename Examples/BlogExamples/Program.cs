/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlogExamples
{

    class Program
    {
        static async Task Main(string[] args)
        {
            string tmpPath = Path.GetTempPath();
            string fileName1 = Path.Combine(tmpPath, "BlogExamplesFile1.dat");
            string fileName2 = Path.Combine(tmpPath, "BlogExamplesFile2.dat");
            string fileName3 = Path.Combine(tmpPath, "BlogExamplesFile3.dat");

            await BufferedStreamWriterExamples.Original_Simple_WriteAsync(fileName1);
            await BufferedStreamWriterExamples.New_Simple_WriteAsync(fileName2);
            await DelayedWriterExamples.New_Simple_WriteAsync(fileName3);
            Console.WriteLine($"{nameof(BufferedStreamWriterExamples)}.{nameof(BufferedStreamWriterExamples.Original_Simple_WriteAsync)} -> {fileName1}");
            Console.WriteLine($"{nameof(BufferedStreamWriterExamples)}.{nameof(BufferedStreamWriterExamples.New_Simple_WriteAsync)} -> {fileName2}");
            Console.WriteLine($"{nameof(DelayedWriterExamples)}.{nameof(DelayedWriterExamples.New_Simple_WriteAsync)} -> {fileName3}");
            Console.WriteLine($"Files are equal={FilesAreEqual(fileName1, fileName2)} & {FilesAreEqual(fileName2, fileName3)}");
            Console.WriteLine();

            byte[] res1 = await BufferedStreamReaderExamples.Original_Simple_ReadAsync(fileName1);
            byte[] res2 = await BufferedStreamReaderExamples.New_Simple_ReadAsync(fileName2);
            byte[] res3 = await PreFetchExamples.New_Simple_ReadAsync(fileName3);
            Console.WriteLine($"{fileName1} -> {nameof(BufferedStreamReaderExamples)}.{nameof(BufferedStreamReaderExamples.Original_Simple_ReadAsync)}");
            Console.WriteLine($"{fileName2} -> {nameof(BufferedStreamReaderExamples)}.{nameof(BufferedStreamReaderExamples.New_Simple_ReadAsync)}");
            Console.WriteLine($"{fileName3} -> {nameof(PreFetchExamples)}.{nameof(PreFetchExamples.New_Simple_ReadAsync)}");
            Console.WriteLine($"Buffers are equal={Enumerable.SequenceEqual(res1, res2)} & {Enumerable.SequenceEqual(res2, res3)}");
            Console.WriteLine();

            await MsgPackCollectionSerializerExamples.Original_UnknownLengthArray_WriteAsync(fileName1);
            await MsgPackCollectionSerializerExamples.New_UnknownLengthArray_WriteAsync(fileName2);
            Console.WriteLine($"{nameof(MsgPackCollectionSerializerExamples)}.{nameof(MsgPackCollectionSerializerExamples.Original_UnknownLengthArray_WriteAsync)} -> {fileName1}");
            Console.WriteLine($"{nameof(MsgPackCollectionSerializerExamples)}.{nameof(MsgPackCollectionSerializerExamples.New_UnknownLengthArray_WriteAsync)} -> {fileName2}");
            X[] MP_ar1 = await MsgPackCollectionDeserializerExamples.Original_UnknownLengthArray_ReadAsync(fileName1).ToArrayAsync();
            X[] MP_ar2 = await MsgPackCollectionDeserializerExamples.New_UnknownLengthArray_ReadAsync(fileName2).ToArrayAsync();
            Console.WriteLine($"{fileName1} -> {nameof(MsgPackCollectionDeserializerExamples)}.{nameof(MsgPackCollectionDeserializerExamples.Original_UnknownLengthArray_ReadAsync)}");
            Console.WriteLine($"{fileName2} -> {nameof(MsgPackCollectionDeserializerExamples)}.{nameof(MsgPackCollectionDeserializerExamples.New_UnknownLengthArray_ReadAsync)}");
            Console.WriteLine($"Arrays MP_are equal={Enumerable.SequenceEqual(MP_ar1, MP_ar2)}");
            Console.WriteLine();

            ProtobufCollectionSerializerExamples.Original_UnknownLengthArray_Write(fileName1);
            await ProtobufCollectionSerializerExamples.New_UnknownLengthArray_WriteAsync(fileName2);
            Console.WriteLine($"{nameof(ProtobufCollectionSerializerExamples)}.{nameof(ProtobufCollectionSerializerExamples.Original_UnknownLengthArray_Write)} -> {fileName1}");
            Console.WriteLine($"{nameof(ProtobufCollectionSerializerExamples)}.{nameof(ProtobufCollectionSerializerExamples.New_UnknownLengthArray_WriteAsync)} -> {fileName2}");
            X[] PB_ar1 = ProtobufCollectionDeserializerExamples.Original_UnknownLengthArray_Read(fileName1).ToArray();
            X[] PB_ar2 = await ProtobufCollectionDeserializerExamples.New_UnknownLengthArray_ReadAsync(fileName2).ToArrayAsync();
            Console.WriteLine($"{fileName1} -> {nameof(ProtobufCollectionDeserializerExamples)}.{nameof(ProtobufCollectionDeserializerExamples.Original_UnknownLengthArray_Read)}");
            Console.WriteLine($"{fileName2} -> {nameof(ProtobufCollectionDeserializerExamples)}.{nameof(ProtobufCollectionDeserializerExamples.New_UnknownLengthArray_ReadAsync)}");
            Console.WriteLine($"Arrays are equal={Enumerable.SequenceEqual(PB_ar1, PB_ar2)}");
            Console.WriteLine();



        }

        private static bool FilesAreEqual(string fileName1, string fileName2)
        {
            try
            {
                return Enumerable.SequenceEqual(File.ReadAllBytes(fileName1), File.ReadAllBytes(fileName2));
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }
    }
}
