using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E2EFileInterpreter
{
    class Program
    {
        static Int64 position;
        static async Task Main(string[] args)
        {
            List<object> list = new List<object>();


            //System.Collections.
            //await HeaderAsync("aaa");
            // Test Header read
            // N.B. Looks like IAsyncEnumerable makes the program await the entire foreach loop instead of awaiting the iterator method.
            await foreach (var item in HeaderAsync("/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E", 0))
            {
                if (item is UInt16[])
                {
                    Action<ushort[]> print = (ushort[] array) =>
                    {
                        for (int index = 0; index < array.Length; index++) Console.WriteLine(
array[index]);
                    };

                    print(item as ushort[]);
                }
                else
                    Console.WriteLine(item/*nameof(item)*/);
                //Console.WriteLine(item is ushort[]);

                list.Add(item);
            }

            Header header = new Header(list[0] as string, (uint)list[1], list[2] as ushort[], (UInt16)list[3]);

            list.Clear();
            await foreach (var item in MainDirectoryAsync("/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"))
            {
                if (item is UInt16[])
                {
                    Action<ushort[]> print = (numbers) =>
                    {
                        foreach (ushort number in numbers)
                        {
                            Console.WriteLine(number);
                        }
                    };
                } else
                {
                    Console.WriteLine(item);
                }

                list.Add(item);

            }
        }

        public static async IAsyncEnumerable<object> HeaderAsync(string filePath, Int64 positionWithinStream)
        {
            using (FileStream dataSourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096,
                                                        true))
            {
                // The Test below shows that the position within a stream starts from 0.
                //Int64 test = dataSourceStream.Position;

                dataSourceStream.Position = positionWithinStream;

                byte[] buffer = new byte[0x10000];
                int numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 12);

                byte[] array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                //var subset = from element in array select "" + element;

                //Decoder decoder = UTF8Encoding.UTF8.GetDecoder();
                //yield return decoder.

                yield return Encoding.UTF8.GetString(array) /*Encoding.ASCII.GetString(array)*/;

                numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 4);
                array = new byte[numRead];

                // No need to clear buffer because we only copy the new byte values we read into the buffer, not any of the old byte values
                // that are still in the byte array.
                Array.Copy(buffer, array, length: 4);

                //byte[] test = new byte[4];
                char[] test2 = Encoding.UTF8.GetChars(array);

                // bytes at offset 12 to 15 should be a 32 bit integer.
                string decodedWithUTF8 = Encoding.UTF8.GetString(array);
                string decodedText = Encoding.UTF32.GetString(array);
                int a = 'd';
                float b = 'd';
                UInt32 c = 'd';

                Int32 num = (array[3] << 24) | (array[2] << 16) | (array[1] << 8) | (array[0]);

                //yield return float.Parse(Encoding.UTF32.GetString(array)/*Encoding.UTF8.GetString(array)*/);
                yield return num;

                // u16 is 2 bytes
                numRead = await dataSourceStream.ReadAsync(buffer, offset: 0, 18);
                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                /*
                 * Deprecated code
                // Gets a UTF16 number string
                string utf16NumberString = Encoding.Unicode.GetString(array);

                // Convert the UTF16 number string into a utf16 number
                ushort utf16Number = UInt16.Parse(utf16NumberString);*/

                UInt16 num2 = (ushort) ((array[1] << 8) | (array[0]));
                UInt16[] numbers = new UInt16[] { (ushort) ((array[17] << 136) | (array[16] << 128)),
                    (ushort) ((array[15] << 120) | (array[14] << 112)),
                    (ushort) ((array[13] << 104) | (array[12] << 96)), (ushort) ((array[11] << 88) | (array[10] << 80)),
                    (ushort) ((array[9] << 72) | (array[8] << 64)), (ushort) ((array[7] << 56) | (array[6] << 48)),
                    (ushort) ((array[5] << 40) | (array[4] << 32)), (ushort) ((array[3] << 24) | (array[2] << 16)),
                    (ushort) ((array[1] << 8) | (array[0])) };

                yield return numbers;
                /*
                 * UInt16 num2 = (UInt16) ((array[17] << 136) | (array[16] << 128) | (array[15] << 120) | (array[14] << 112) | (array[13] << 104) |
                    (array[12] << 96) | (array[11] << 88) | (array[10] << 80) | (array[9] << 72) | (array[8] << 64) | (array[7] << 56) |
                    (array[6] << 48) | (array[5] << 40) | (array[4] << 32) | (array[3] << 24) | (array[2] << 16) | (array[1] << 8) |
                    (array[0]));
                 */

                //yield return utf16Number;

                // ReadAsync automatically advances the position within the current stream by the number of bytes read.
                numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 2);

                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                // without explicitly converting the 16 bit integer that is evaluated from the expression below, does C Sharp pad
                // the 16 bit integers with zeros at the least significant position (i.e. rightmost position) to make up 32 binary digits?
                // If so, then casting from int32 to UInt16 simply removes those 16 padded zeros that were added to the righmost position
                // of the 16 bit integer.
                UInt16 positiveNumber = (UInt16) ((array[1] << 8) | (array[0]));

                yield return positiveNumber;
                /*
                 * Deprecated code
                utf16NumberString = Encoding.Unicode.GetString(array);

                utf16Number = UInt16.Parse(utf16NumberString);

                yield return utf16Number;*/

                // Assume that the position of bytes in a stream starts from 1.
                position = dataSourceStream.Position;
            }


        }

        public static async IAsyncEnumerable<Object> MainDirectoryAsync(String filePath)
        {
            // I don't want to reuse a stream because I want the read operations (i.e. HeadersAsync, MainDirectoryAsync) to run
            // asynchronously

            // return "2"; does not work
            //yield return "2";

            await foreach (var item in HeaderAsync("/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E", Program.position))
            {
                yield return item;
            }

            List<FileStream> fileStreams = new List<FileStream>();

            try
            {
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, true);
                fileStream.Position = position + 1;
                fileStreams.Add(fileStream);

                byte[] buffer = new byte[0x1000];

                int numRead = await fileStream.ReadAsync(buffer, 0, 4);

                byte[] array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                // Note: that UInt32 would be better because numEntries should never be negative.
                Int32 numEntries = (array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0];

                yield return numEntries;

                numRead = await fileStream.ReadAsync(buffer, 0, count: 4);

                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                // After bitwise shifting is complete, the least significant byte array[0] is now at the rightmost position (i.e. the
                // least significant position) in big endian.
                UInt32 current = (UInt32)((array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0]);

                numRead = await fileStream.ReadAsync(buffer, 0, 4);

                array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                UInt32 value = (UInt32)((array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0]);

                yield return value;

                numRead = await fileStream.ReadAsync(buffer, 0, 4);

                array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                // Remember that an integer is a group of bytes. So here the 32 bit integer is a group of 4 bytes.
                UInt32 unknown = (UInt32)((array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0]);

                yield return unknown;

            } finally
            {
                // Update position before disposing the Stream
                Program.position = fileStreams[0].Position;

                fileStreams[0].Dispose();
            }

        }

        private static void Clear(byte[] arr, int index)
        {
            if (index == arr.Length)
            {
                return;
            }

            arr[0] = 0;

            Clear(arr, index + 1);
        }
    }
}
