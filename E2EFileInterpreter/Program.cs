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
        static async Task Main(string[] args)
        {
            //System.Collections.

            // Test Header read
            await foreach (var item in HeaderAsync("/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"))
            {
                Console.WriteLine(item);
            }
        }

        public static async IAsyncEnumerable<object> HeaderAsync(string filePath)
        {
            using (FileStream dataSourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096,
                                                        true))
            {
                byte[] buffer = new byte[0x10000];
                int numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 12);

                byte[] array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                //var subset = from element in array select "" + element;

                //Decoder decoder = UTF8Encoding.UTF8.GetDecoder();
                //yield return decoder.

                yield return Encoding.UTF8.GetString(array);

                numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 4);
                array = new byte[numRead];

                // No need to clear buffer because we only copy the new byte values we read into the buffer, not any of the old byte values
                // that are still in the byte array.
                Array.Copy(buffer, array, length: 4);

                yield return float.Parse(Encoding.UTF8.GetString(array));

                // u16 is 2 bytes
                numRead = await dataSourceStream.ReadAsync(buffer, offset: 0, 18);
                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                // Gets a UTF16 number string
                string utf16NumberString = Encoding.Unicode.GetString(array);

                // Convert the UTF16 number string into a utf16 number
                ushort utf16Number = UInt16.Parse(utf16NumberString);

                yield return utf16Number;

                // ReadAsync automatically advances the position within the current stream by the number of bytes read.
                numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 2);

                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                utf16NumberString = Encoding.Unicode.GetString(array);

                utf16Number = UInt16.Parse(utf16NumberString);

                yield return utf16Number;

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
