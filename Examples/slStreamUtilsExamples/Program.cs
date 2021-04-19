/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace slStreamUtilsExamples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string tmpPath = Path.GetTempPath();
            string fileName1 = Path.Combine(tmpPath, "BlogExamplesFile1.dat");
            string fileName2 = Path.Combine(tmpPath, "BlogExamplesFile2.dat");
            string fileName3 = Path.Combine(tmpPath, "BlogExamplesFile3.dat");

            await BufferedStreamWriterExamples.Original_WriteAsync(fileName1);
            await BufferedStreamWriterExamples.New_WriteAsync(fileName2);
            await DelayedWriterExamples.New_WriteAsync(fileName3);
            Console.WriteLine($"{nameof(BufferedStreamWriterExamples)}.{nameof(BufferedStreamWriterExamples.Original_WriteAsync)} -> {fileName1}");
            Console.WriteLine($"{nameof(BufferedStreamWriterExamples)}.{nameof(BufferedStreamWriterExamples.New_WriteAsync)} -> {fileName2}");
            Console.WriteLine($"{nameof(DelayedWriterExamples)}.{nameof(DelayedWriterExamples.New_WriteAsync)} -> {fileName3}");
            Console.WriteLine($"Files are equal={FilesAreEqual(fileName1, fileName2)} & {FilesAreEqual(fileName2, fileName3)}");
            Console.WriteLine();

            byte[] res1 = await BufferedStreamReaderExamples.Original_ReadAsync(fileName1);
            byte[] res2 = await BufferedStreamReaderExamples.New_ReadAsync(fileName2);
            byte[] res3 = await PreFetchExamples.New_ReadAsync(fileName3);
            Console.WriteLine($"{fileName1} -> {nameof(BufferedStreamReaderExamples)}.{nameof(BufferedStreamReaderExamples.Original_ReadAsync)}");
            Console.WriteLine($"{fileName2} -> {nameof(BufferedStreamReaderExamples)}.{nameof(BufferedStreamReaderExamples.New_ReadAsync)}");
            Console.WriteLine($"{fileName3} -> {nameof(PreFetchExamples)}.{nameof(PreFetchExamples.New_ReadAsync)}");
            Console.WriteLine($"Buffers are equal={res1.SequenceEqual(res2)} & {res2.SequenceEqual(res3)}");
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
