using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using AnonymizationLibrary;
using System.Diagnostics;

namespace E2EFileInterpreter
{
    class Program
    {
        static Int64 position;
        static int count;

        // May be rename dataChunksToProcess to addresses of data chunks.
        static Stack<Tuple<UInt32, uint>> dataChunksToProcess = new Stack<Tuple<uint, uint>>();

        static string filePath = "/Users/christopheraneke/Downloads/ASLAM01T.E2E"/*"/tmp/ASLAM01TAnonymized.E2E"*/;

        static int Given_name_entry_length { get; set; }
        static int Surname_entry_length { get; set; }
        static int Patient_identifier_entry_length { get; set; }
        static int Full_name_of_operator_entry_length { get; set; }

        // Global image data
        public static double[] pixelImageData;

        static async Task Main(string[] args)
        {
            List<object> list = new List<object>();


            //System.Collections.
            //await HeaderAsync("aaa");
            // Test Header read
            // N.B. Looks like IAsyncEnumerable makes the program await the entire foreach loop instead of awaiting the iterator method.
            await foreach (var item in HeaderAsync(Program.filePath/*"/tmp/ASLAM01TAnonymized.E2E"*//*"/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"*/, 0))
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
            await foreach (var item in MainDirectoryAsync(Program.filePath/*"/tmp/ASLAM01TAnonymized.E2E"*//*"/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"*/))
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

            object obj = new MainDirectory();

            // Throws InvalidCastException. Find out why?
            //MainDirectory obj2 = (MainDirectory) new object();

            MainDirectory mainDirectory = new MainDirectory(list[0], list[1], list[2], list[3], list[4], list[5], list[6], list[7]);

            // test
            string filePath = "/Users/christopheraneke/Downloads/ASLAM01T.E2E"/*"/tmp/ASLAM01TAnonymized.E2E"*//*"/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"*/;

            await TraverseListOfDirectoryChunks(mainDirectory.current, /*filePath*/Program.filePath);
            /*
             * I should probably remove this comment.
            // I tried finding the offset, assuming that the structure of the e2e file described by uocte holds but with an offset, but
            // it is futile and like searching for a needle in a haystack, never mind that there might not be any offset and the structure
            // of the e2e file described by uocte is not for this particular uocte file, may be because the structure described by uocte is
            // outdated. */

            Dictionary<string, Dictionary<string, object>> chunks = await ReadDataChunksAsync(filePath: Program.filePath/*filePath*/);

            // Print test
            await PrintBytesInFileAsync(filePath);

            string patient_identifier = (String) chunks["chunk 47"]["patient_identifier"];

            Randomizer randomizer = new Randomizer(patient_identifier.Replace("\0", string.Empty));
            randomizer.shuffleCharacters();

            string anonymized_patient_identifier = randomizer.randomizedMrn;

            string pseudo_given_name = "Patient";

            string[] familyNames = null;

            try
            {
                familyNames = await File.ReadAllLinesAsync("/Users/christopheraneke/Projects/E2EFileInterpreter/E2EFileInterpreter/" +
                                "family_names.txt");
            } catch (Exception)
            {

            }

            Random randomNumberGenerator = new Random();

            string anonymized_surname = familyNames[randomNumberGenerator.Next(maxValue: familyNames.Length)];

            string anonymized_full_name_of_operator = "Mrs Camera Operator";

            patient_identifier = patient_identifier.Replace("\0", "\\0");

            // Calls to Replace() method must come after the four properties ending with length are assigned a value, in order to
            // avoid breaking the PadWithNullCharacters method.
            SearchAndReplace(patient_identifier, ((string) chunks["chunk 47"]["given_name"]).Replace("\0", "\\0"),
                ((String) chunks["chunk 47"]["surname"]).Replace("\0", "\\0"),
                ((String) chunks["chunk 5"]["full_name_of_operator"]).Replace("\0", "\\0"), anonymized_patient_identifier,
                pseudo_given_name, anonymized_surname, anonymized_full_name_of_operator);
        }

        public static async IAsyncEnumerable<object> HeaderAsync(string filePath, Int64 positionWithinStream)
        {
            using (FileStream dataSourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096,
                                                        true))
            {
                count += 1;
                // The Test below shows that the position within a stream starts from 0.
                //Int64 test = dataSourceStream.Position;

                dataSourceStream.Position = positionWithinStream;

                byte[] buffer = new byte[0x10000];
                int numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 12);

                byte[] array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                // N.B. a set is just an array
                Char[] testArray = Encoding.Unicode.GetChars(array);
                Char[] testArray2 = Encoding.BigEndianUnicode.GetChars(array);
                Char[] testArray3 = Encoding.ASCII.GetChars(array);

                // Why does UTF8.GetChars(array) return only 5 characters when array is an array of 12 bytes and UTF8 means that a byte
                // is one character?
                Char[] testArray4 = Encoding.UTF8.GetChars(array);
                Char[] testArray5 = Encoding.UTF32.GetChars(array);
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

                UInt32 testConverted = (UInt32)num;
                // Big endian is like a mirror image of little endian, and vice versa.
                UInt32 testConverted2 = (UInt32) (array[3] << 24) | (UInt32) (array[2] << 16) | (UInt32) (array[1] << 8) | array[0];
                UInt32 testConverted3 = (UInt32)((array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0]);
                yield return (UInt32) num;

                // u16 is 2 bytes

                /*
                int maxBytes = 54;

                if(count > 1)
                {
                    maxBytes = 57;
                }*/

                numRead = await dataSourceStream.ReadAsync(buffer, offset: 0, 18 /*54*/ /*maxBytes*/);
                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                /*
                 * Deprecated code
                // Gets a UTF16 number string
                string utf16NumberString = Encoding.Unicode.GetString(array);

                // Convert the UTF16 number string into a utf16 number
                ushort utf16Number = UInt16.Parse(utf16NumberString);*/

                UInt16 num2 = (ushort) ((array[1] << 8) | (array[0]));

                // To convert a byte array to an array of unsigned 16 bit integers you must convert every two bytes as you traverse the
                // byte array from index 0 to Length - 1, not how I wrongly tried to convert a byte array to a UInt16[] shown below
                //UInt16[] numbers = new UInt16[] { (ushort) ((array[17] << 136) | (array[16] << 128)),
                //    (ushort) ((array[15] << 120) | (array[14] << 112)),
                //    (ushort) ((array[13] << 104) | (array[12] << 96)), (ushort) ((array[11] << 88) | (array[10] << 80)),
                //    (ushort) ((array[9] << 72) | (array[8] << 64)), (ushort) ((array[7] << 56) | (array[6] << 48)),
                //    (ushort) ((array[5] << 40) | (array[4] << 32)), (ushort) ((array[3] << 24) | (array[2] << 16)),
                    //(ushort) ((array[1] << 8) | (array[0])) };

                //BitConverter.ToUInt16(va)

                // When optimizing code may be initialize u16Array with a null value
                UInt16[] u16Array = new ushort[9];

                /*
                if (count > 1)
                {
                    u16Array = new ushort[29];
                }*/

                Action<byte[], UInt16[]> convert = (arr, arr2) =>
                {
                    for (int index = 0, j = 0; index < arr.Length; index += 2, j++)
                    {

                        arr2[j] = BitConverter.ToUInt16(arr, startIndex: index);
                        /*
                        if (arr.Length == 54)
                        {
                            arr2[j] = BitConverter.ToUInt16(arr, startIndex: index);
                        } else if (arr.Length == 57)
                        {
                            if (index != arr.Length - 1)
                            {
                                u16Array[j] = BitConverter.ToUInt16(arr, index);
                            }
                            else
                            {
                                // Avoids ArgumentException
                                u16Array[j] = 2019;
                            }

                        }*/
                    }
                };

                convert(array, u16Array);

                yield return u16Array;

                //yield return numbers;
                /*
                 * UInt16 num2 = (UInt16) ((array[17] << 136) | (array[16] << 128) | (array[15] << 120) | (array[14] << 112) | (array[13] << 104) |
                    (array[12] << 96) | (array[11] << 88) | (array[10] << 80) | (array[9] << 72) | (array[8] << 64) | (array[7] << 56) |
                    (array[6] << 48) | (array[5] << 40) | (array[4] << 32) | (array[3] << 24) | (array[2] << 16) | (array[1] << 8) |
                    (array[0]));
                 */

                //yield return utf16Number;

                //int bytesToRead = 2;

                //if (Program.count > 1)
                //{
                //    bytesToRead = /*1*/0;
                //}

                // ReadAsync automatically advances the position within the current stream by the number of bytes read.
                numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 2 /*bytesToRead*/);

                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                // without explicitly converting the 16 bit integer that is evaluated from the expression below, does C Sharp pad
                // the 16 bit integers with zeros at the least significant position (i.e. rightmost position) to make up 32 binary digits?
                // If so, then casting from int32 to UInt16 simply removes those 16 padded zeros that were added to the righmost position
                // of the 16 bit integer.

                UInt16 positiveNumber = (UInt16) ((array[1] << 8) | array[0]);

                
                //UInt16 positiveNumber;
                //if (bytesToRead == 2)
                //{
                //    /*UInt16*/ positiveNumber = (UInt16) ((array[1] << 8) | (array[0]));

                //} else
                //{
                //    // Just returning 0 when reading main_directory, ideally should return null
                //    positiveNumber = 0/*(UInt16)array[0]*/;
                //}
                //UInt16 testA = (UInt16)array[0];

                yield return positiveNumber;
                /*
                 * Deprecated code
                utf16NumberString = Encoding.Unicode.GetString(array);

                utf16Number = UInt16.Parse(utf16NumberString);

                yield return utf16Number;*/

                position = dataSourceStream.Position;
            }


        }

        public static async IAsyncEnumerable<Object> MainDirectoryAsync(String filePath)
        {
            // I don't want to reuse a stream because I want the read operations (i.e. HeadersAsync, MainDirectoryAsync) to run
            // asynchronously

            // return "2"; does not work
            //yield return "2";

            //// No need to start reading at a position of 72 because 9x 0xffff is now 57 instead of 18
            //long positionWithinStream = /*position + */ 36;

            // Todo: Replace hard coded file path with variable or parameter.
            await foreach (var item in HeaderAsync(filePath/*"/tmp/ASLAM01TAnonymized.E2E"*//*"/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"*/, position /*positionWithinStream*//*Program.position + 36*/))
            {
                yield return item;
            }

            List<FileStream> fileStreams = new List<FileStream>();

            try
            {
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, true);

            // After reading a byte, ReadAsync advances the position within the stream by the number of bytes read and continues
            // reading bytes from the next byte. So the current position within a stream is always the position that follows the last
            // byte that was read by the ReadAsync method. Therefore to continue reading bytes from where ReadAsync left off there is
            // no need to add 1 to the position within the current stream.

                fileStream.Position = position /*+ 1*/;
                fileStreams.Add(fileStream);

                byte[] buffer = new byte[0x1000];

                int numRead = await fileStream.ReadAsync(buffer, 0, 4);

                byte[] array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                // numEntries is the number of data chunks to process in the second pass.
                // Note: that UInt32 would be better because numEntries should never be negative.
                Int32 numEntries = (array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0];

                yield return (UInt32) numEntries;

                numRead = await fileStream.ReadAsync(buffer, 0, count: 4);

                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                // After bitwise shifting is complete, the least significant byte array[0] is now at the rightmost position (i.e. the
                // least significant position) in big endian.
                UInt32 current = (UInt32)((array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0]);

                yield return current;

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

        public static async Task TraverseListOfDirectoryChunks(Int64 currentPosition, string filePath)
        {
            FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

            // Note: Consider putting the variables magic3, version etc. in a collection because if I want to write bytes to a new file
            // after anonymizing specific bytes then having the variables in a collection would be useful to get the bytes from the data
            // types string, UInt32 etc.

            // Update: The explanation below is incorrect since prev is 0 in the first iteration of the do-while statement, so current
            // changes to zero therefore the do block only executes once. The correct understanding is that numEntries is the number of
            // directory chunks. There are 512 directory chunks to traverse.
            // Gets the number (i.e. chunks) of (directory) entries in a single linked list and adds the Tuple<start, size> to the stack.
            // When the while condition is reached, if current is not zero, the do block repeats by seeking to the current value of
            // current, continuing to get the number of entries in each directory chunk, and add (i.e. push) the Tuple<start, size> to the
            // stack.
            // Note: The Main directory is split into chunks, the do-while statement represents each directory chunk.
            do
            {
                // Seek to the next directory chunk
                // SeekOrigin.Begin means the value of current starting from the beginning of the stream.
                long currentPositionWithinStream = sourceStream.Seek(/*mainDirectory.current*/ currentPosition /*mainDirectory.numEntries*/, origin: SeekOrigin.Begin);

                long positionTest = sourceStream.Position;

                byte[] buffer = new byte[0x1000];
                int numRead = await sourceStream.ReadAsync(buffer, 0, count: 12 /*10*//*16*/ /*73*/ /*89*/);

                byte[] array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                //string testString = BitConverter.ToString(array);

                //char charTest = BitConverter.ToChar(array, startIndex: 0);

                //string testString2 = Encoding.ASCII.GetString(array);
                //string testString3 = Encoding.UTF8.GetString(array);
                //string testString4 = Encoding.UTF32.GetString(array);

                string magic3 = Encoding.UTF8.GetString(array);

                numRead = await sourceStream.ReadAsync(buffer, 0, 4);

                array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                UInt32 version = (UInt32)(array[3] << 24) | (UInt32)(array[2] << 16) | (UInt32)(array[1] << 8) | (UInt32)array[0];

                numRead = await sourceStream.ReadAsync(buffer, 0, 18);

                array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                ushort[] arrayOf16BitIntegers = new UInt16[9];

                Action<byte[], UInt16[]> convert = (arr1, arr2) =>
                {
                    for (int index = 0, j = 0; index < arr2.Length; index++, j += 2)
                    {
                        arr2[index] = BitConverter.ToUInt16(arr1, startIndex: j);
                    }
                };

                convert(array, arrayOf16BitIntegers);

                numRead = await sourceStream.ReadAsync(buffer, 0, 2);

                array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                UInt16 unknown = (UInt16)((array[1] << 8) | array[0]);

                numRead = await sourceStream.ReadAsync(buffer, 0, 4);

                array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                // numEntries is the number of entries in each directory chunk
                uint numEntries = (uint)(array[3] << 24) | (uint)(array[2] << 16) | (UInt32)(array[1] << 8) | (UInt32)array[0];

                numRead = await sourceStream.ReadAsync(buffer, 0, 4);
                array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                UInt32 unknown2 = (ushort)((array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0]);

                numRead = await sourceStream.ReadAsync(buffer, 0, 4);

                array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                UInt32 prev = (uint)(array[3] << 24) | (uint)(array[2] << 16) | (uint)(array[1] << 8) | (UInt32)array[0];

                numRead = await sourceStream.ReadAsync(buffer, 0, 4);
                array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                UInt32 unknown3 = (UInt32)(array[3] << 24) | (uint)(array[2] << 16) | (uint)(array[1] << 8) | (UInt32)array[0];

                // Iterate over the number of entries in each directory chunk.
                for (; numEntries >= 1; numEntries--)
                {
                    // start is the starting position of a chunk in the binary file.
                    var pos = await GetU32EntryAsync(sourceStream, buffer);
                    var start = await GetU32EntryAsync(sourceStream, buffer);
                    var size = await GetU32EntryAsync(sourceStream, buffer);
                    var unknown4 = await GetU32EntryAsync(sourceStream, buffer);
                    var patient_id = await GetU32EntryAsync(sourceStream, buffer);
                    var study_id = await GetU32EntryAsync(sourceStream, buffer);
                    var series_id = await GetU32EntryAsync(sourceStream, buffer);
                    var slice_id = await GetU32EntryAsync(sourceStream, buffer);
                    var unknown5 = await GetU16EntryAsync(sourceStream, buffer);
                    var unknown6 = await GetU16EntryAsync(sourceStream, buffer);
                    var type = await GetU32EntryAsync(sourceStream, buffer);
                    var unknown7 = await GetU32EntryAsync(sourceStream, buffer);

                    if (start > pos)
                    {
                        dataChunksToProcess.Push(new Tuple<UInt32, UInt32>(start, size));
                    }
                }

                currentPosition = prev;
            } while (currentPosition != 0);

        }

        public static async Task<UInt32> GetU32EntryAsync(FileStream sourceStream, byte[] buffer/*, byte[] fourBytes*/)
        {
            int numRead = await sourceStream.ReadAsync(buffer, 0, 4);

            byte[] fourBytes = new byte[numRead];

            Array.Copy(buffer, fourBytes, numRead);

            UInt32 number = (UInt32)(fourBytes[3] << 24) | (UInt32)(fourBytes[2] << 16) | (uint)(fourBytes[1] << 8) | (uint)fourBytes[0];

            return number;
        }

        public static async Task<ushort> GetU16EntryAsync(FileStream sourceStream, byte[] buffer)
        {
            int numRead = await sourceStream.ReadAsync(buffer, 0, 2);
            byte[] twoBytes = new byte[numRead];

            Array.Copy(buffer, twoBytes, numRead);

            ushort number = (UInt16)((twoBytes[1] << 8) | twoBytes[0]);

            return number;
        }

        private static async Task</*Hashtable*/Dictionary<string, /*object*/Dictionary<string, object>>> ReadDataChunksAsync(string filePath)
        {
            // Dictionary<string, object> should be able to achieve the same result as using a hashtable
            /*Hashtable chunk = new Hashtable();*/

            Dictionary<string, /*object*/Dictionary<string, object>> chunks =
                                                                    new Dictionary<string, /*object*/ Dictionary<string, object>>();

            FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            byte[] buffer = new byte[0x1000];

            for (int index = /*0*/dataChunksToProcess.Count - 1; index >= 0 /*< dataChunksToProcess.Count*/; index--/*index++*/)
            {
                Dictionary<string, object> chunk = new Dictionary<string, object>();
                int startingPositionOfChunk = (int)dataChunksToProcess./*Peek().Item1*/Pop().Item1;
                long positionWithinStream = sourceStream.Seek(startingPositionOfChunk, SeekOrigin.Begin);

                int numRead = await sourceStream.ReadAsync(buffer, 0, 12);
                byte[] twelveBytes = new byte[numRead];

                Array.Copy(buffer, twelveBytes, numRead);

                string magic4 = Encoding.UTF8.GetString(twelveBytes);

                chunk.Add("magic4", magic4);

                UInt32 unknown1 = await GetU32EntryAsync(sourceStream, buffer);
                uint unknown2 = await GetU32EntryAsync(sourceStream, buffer);
                uint pos = await GetU32EntryAsync(sourceStream, buffer);
                uint size = await GetU32EntryAsync(sourceStream, buffer);
                uint unknown3 = await GetU32EntryAsync(sourceStream, buffer);
                uint patient_id = await GetU32EntryAsync(sourceStream, buffer);
                UInt32 study_id = await GetU32EntryAsync(sourceStream, buffer);
                UInt32 series_id = await GetU32EntryAsync(sourceStream, buffer);
                UInt32 slice_id = await GetU32EntryAsync(sourceStream, buffer);
                ushort ind = await GetU16EntryAsync(sourceStream, buffer);
                ushort unknown4 = await GetU16EntryAsync(sourceStream, buffer);
                uint type = await GetU32EntryAsync(sourceStream, buffer);
                uint unknown5 = await GetU32EntryAsync(sourceStream, buffer);

                chunk["unknown1"] = unknown1;
                chunk[nameof(unknown2)] = unknown2;
                chunk["pos"] = pos;
                chunk[nameof(size)] = size;
                chunk[nameof(unknown3)] = unknown3;
                chunk["patient_id"] = patient_id;
                chunk["study_id"] = study_id;
                chunk["series_id"] = series_id;
                chunk[nameof(slice_id)] = slice_id;
                chunk["ind"] = ind;
                chunk["unknown4"] = unknown4;
                chunk["type"] = type;
                chunk[nameof(unknown5)] = unknown5;

                // If type == 0x00000009 read additional patient info.
                // Test:
                uint test = 0x00000009;
                uint test2 = 0x0000000B;
                uint test3 = 0x0000000b;
                if (type == 0x00000009)
                {
                    numRead = await sourceStream.ReadAsync(buffer, 0, 31);
                    byte[] thirtyOneBytes = new byte[numRead];

                    Array.Copy(buffer, thirtyOneBytes, numRead);

                    string givenName = Encoding.UTF8.GetString(thirtyOneBytes);

                    // Test
                    int testLength = givenName.Length;


                    numRead = await sourceStream.ReadAsync(buffer, 0, 66);

                    byte[] sixtySixBytes = new byte[numRead];

                    Array.Copy(buffer, sixtySixBytes, numRead);

                    string surname = Encoding.UTF8.GetString(sixtySixBytes);


                    uint birthdate = await GetU32EntryAsync(sourceStream, buffer);

                    /*
                    numRead = await sourceStream.ReadAsync(buffer, 0, 1);
                    byte oneByte = buffer[0];

                    // A character can represent an unsigned integer https://www.tamasoft.co.jp/en/general-info/unicode-decimal.html,
                    // or in other words, an unsigned integer can be represented by a character.
                    char sex = (char)oneByte;*/

                    // The patient identifier (i.e. hospital number) a thirty character string (i.e. u8[30]) that is padded with white
                    // space. The first character of the patient identifier is the sex of the patient (i.e. 'M' or 'F'). So extract
                    // patient identifier instead of just the sex of the patient.
                    // Note: that a Null byte (i.e. decimal byte value 0) as a character is represented as a white space character
                    // https://www.tamasoft.co.jp/en/general-info/unicode-decimal.html.

                    numRead = await sourceStream.ReadAsync(buffer, 0, 30);
                    byte[] thirtyBytes = new byte[numRead];

                    Array.Copy(buffer, thirtyBytes, numRead);

                    string patientIdentifier = Encoding.UTF8.GetString(thirtyBytes);

                    chunk["given_name"] = givenName;
                    chunk.Add(nameof(surname), surname);
                    chunk.Add("birthdate", birthdate);
                    //chunk[nameof(sex)] = sex;
                    chunk.Add("patient_identifier", patientIdentifier);

                    // Remember the length of entries of interest.
                    Given_name_entry_length = thirtyOneBytes.Length;
                    Surname_entry_length = sixtySixBytes.Length;
                    Patient_identifier_entry_length = thirtyBytes.Length;

                    /*
                     * Note: This can also be used to stop C Sharp from removing null characters when I convert bytes to string
                    // Calls to Replace must come after remembering the length of entries of interest, in order to avoid breaking the
                    // function PadWithNullCharacters
                    givenName = givenName.Replace("\0", "\\0");
                    surname = surname.Replace("\0", "\\0");*/
                } else if (type == 10)
                {
                    // After the chunk structure described above there are 16 bytes between the last entry in the chunk structure (i.e.
                    // unknown5) and the camera operator name. And I am assuming that the 16 bytes are an array of 16 bit integers.
                    // Note: that the chunk structure defined above is a total of 60 bytes, from magic4 to unknown5.
                    numRead = await sourceStream.ReadAsync(buffer, 0, 16);
                    byte[] sixteenBytes = new byte[numRead];

                    Array.Copy(buffer, sixteenBytes, numRead);

                    UInt16[] unknown6 = ToUInt16Array(sixteenBytes);

                    numRead = await sourceStream.ReadAsync(buffer, 0, 36);

                    byte[] thirtySixBytes = new byte[numRead];

                    // Only copy the bytes that were read.
                    Array.Copy(buffer, thirtySixBytes, numRead);

                    string fullNameOfOperator = Encoding.UTF8.GetString(thirtySixBytes);

                    chunk.Add(nameof(unknown6), unknown6);
                    chunk["full_name_of_operator"] = fullNameOfOperator;

                    // Record the length of an entry of interest.
                    Full_name_of_operator_entry_length = thirtySixBytes.Length;

                    /*
                    // This must come last to avoid braking the function PadWithNullCharacters
                    fullNameOfOperator = fullNameOfOperator.Replace("\0", "\\0");*/
                } else if (type == 0x0000000B)
                {
                    numRead = await sourceStream.ReadAsync(buffer, 0, 14);
                    byte[] fourteenBytes = new byte[numRead];

                    Array.Copy(buffer, fourteenBytes, numRead);

                    string unknownInLaterality = Encoding.UTF8.GetString(fourteenBytes);

                    numRead = await sourceStream.ReadAsync(buffer, 0, 1);
                    byte oneByte = buffer[0];

                    char laterality = (char) oneByte;
                    
                } else if (type == 0x40000000)
                {
                    UInt32 sizeOfImage = await GetU32EntryAsync(sourceStream, buffer);
                    UInt32 typeOfImage = await GetU32EntryAsync(sourceStream, buffer);
                    uint unknownInImageData = await GetU32EntryAsync(sourceStream, buffer);
                    uint width = await GetU32EntryAsync(sourceStream, buffer);
                    UInt32 height = await GetU32EntryAsync(sourceStream, buffer);
                    int test4 = (int)height * (int)width;
                    int test5 = (int)(height * width);

                    if (ind == 0)
                    {
                        // Use larger array when the number of bytes height * wdith (i.e. 380928) is being written to the buffer.
                        buffer = new byte[0x100000];
                        numRead = await sourceStream.ReadAsync(buffer, 0, (int) height * (int) width);

                        // No need for bit shift because each element of array is just a byte
                        byte[] raw_fundus_image = new byte[height * width];
                        Array.Copy(buffer, raw_fundus_image, numRead);
                    } else
                    {
                        //new BitArray().
                        //Math.floor
                        //BitConverter.

                        // Use larger array when the number of bytes height * width (i.e. 380928) is being written to the buffer.
                        buffer = new byte[0x100000];
                        numRead = await sourceStream.ReadAsync(buffer, 0, (int)(height * width));
                        byte[] tomogramImageData = new byte[numRead];

                        Array.Copy(buffer, tomogramImageData, numRead);

                        // Debugger shows that BitArray automatically converts bytes from little endian to big endian (i.e. BitArray uses
                        // big endian).
                        BitArray bits = new BitArray(tomogramImageData);

                        //List<float> realNumbers = new List<float>();
                        List<Double> realNumbers = new List<double>();

                        //Array array = new Array();
                        
                        int countOfStacks = 0;

                        // Initialize delegate type with a lambda expression.
                       DPlaceHolder<BitArray/*, int*//*, Object*/> dPlaceHolder = null;

                        dPlaceHolder = (BitArray bitArray/*,*/ /*int takes*/ /*int currentIndex*/) =>
                        {
                            // Switch the relational operator <= to < in order to stop the following System.ArgumentOutOfRangeException.
                            // currentIndex < 3047424 is correct because when currentIndex reaches 3047424 that means that it has taken the
                            // previous 16 indexes which includes 3047424 - 1 (i.e. Length - 1) which is the index of the last element in the
                            // collection named bits.:
                            // Unhandled exception. System.ArgumentOutOfRangeException: Index was out of range. Must be non - negative and
                            // less than the size of the collection. (Parameter 'index') Actual value was 3047424.
                            for (int currentIndex = 0; currentIndex </*=*/ bitArray.Length; currentIndex += 16)
                            {
                                //BitConverter.ToSingle()
                                //new byte[2].Window
                                // Tests
                                countOfStacks += 1;
                                //Console.WriteLine($"currentIndex: {currentIndex}, number of bits: {bits.Length}, number of loops: {countOfStacks}");
                                // Todo: This recurive function should work since 3047424 % 16 == 0 watch part 2 of that video tutorial of
                                // the visual studio debugger to find out when the stack overflows. Update: print statement above shows that
                                // the stack over flows after the 1080th function frame that is added to the function stack. So either
                                // recursion cannot handle this solution or I have to find a way to increase the size of the function stack
                                // by a lot.

                                // May be rename count to index

                                // At first gets the first sixteen elements i.e. elements 0 to 15, then the next sixteen elements, i.e. elements
                                // 16 to 32 

                                // Count jumps by 16 in each recursive call. No need to subtract 1 because currentIndex goes up by 16 so
                                // is always even. This conditional statement means as soon as the currentIndex, and since bitArray is an
                                // array of n bytes * 8 (i.e. each byte is 8 binary digits) therefore 16 should go into n * 8 binary digits.
                                // Correction: Since "uf16[height][width] raw tomogram slice image" (i.e. tomogramImageData is an array of
                                // 16 bits, or in other words since the BitArray object named bits is made up of multiple 16 bits, and each
                                // element of the BitArray object named bits is a bit) that means that 16 should go into the total number of
                                // elements in the BitArray object named bits (i.e. the number of elements in the BitArray object named bits
                                // is divisible by 16 which means that when currentIndex (which increases by 16 in each recursive call)
                                // reaches the same value as bitArray.Length that means that we have reached the end of the BitArray object
                                // named bits which also means that (via the for loops in this recursive function) we have accessed all
                                // elements in the BitArray object named bits.

                                //if (/*takes == bitArray.Length / 16*/currentIndex == bitArray.Length/* - 1*/)
                                //{
                                //    return;
                                //}

                                //BitArray takenBits = bitArray.CopyTo(arr)

                                bool[] floatingPointRepresentation = new bool[16];

                                //int a = (int) true;

                                //const int end = index;
                                int end = currentIndex + 16;
                                // Replace the parameter index used in the loop with the variable begin because the increment expression
                                // index++ increases the value of index which affects the recursion. The count of the List<Double> named
                                // realNumbers should be only sixteen times less than the parameter index, e.g. if realNumbers.Count is 4
                                // then index should be 64.
                                int begin = currentIndex;
                                for (int i = 0; /*index*/ begin < end/*index + 16*/; /*index++*/begin++, i++)
                                {
                                    //BitConverter.ToDouble()
                                    floatingPointRepresentation[i] = bitArray[/*index*/begin];

                                }

                                // 10 bit mantissa
                                bool[] mantissa = new bool[10];

                                // 6 bit exponent
                                bool[] exponent = new bool[6];

                                for (int i = 0; i < floatingPointRepresentation.Length; i++)
                                {
                                    if (i < 10)
                                    {
                                        mantissa[i] = floatingPointRepresentation[i];
                                    }
                                    else
                                    {
                                        // Subtract 10 to start from index 0 of the array named exponent.
                                        exponent[i - 10] = floatingPointRepresentation[i];
                                    }
                                }

                                //BitArray mantissaBits = new
                                // IEnumerable<T> is an iterator just like an iterator method, or iterator get accessor
                                // mantissaAsBinaryNumber should be a sequence of 1s and 0s.
                                IEnumerable<int> mantissaAsBinaryNumber = mantissa.Select<bool, int>((x) =>
                                {
                                    int bit = Convert.ToInt32(x);

                                    return bit;
                                });

                                //int b = 0b10111;
                                //Convert.ToDouble("11", 2);
                                Convert.ToInt32("1011", 2);
                                //Convert.ToInt32(mantissaAsBinaryNumber, 2);

                                // Attempting to convert mantissaAsBinaryNumber to Int32 below throws the exception System.InvalidCastException.
                                //int testingConversion = Convert.ToInt32(mantissaAsBinaryNumber);

                                IEnumerable<int> exponentAsBinary = exponent.Select<bool, int>((x) =>
                                {
                                    int bit = Convert.ToInt32(x);
                                    return bit;
                                });

                                //mantissaAsBinaryNumber.ToS
                                /*DPlaceHolder2 mantissaBinaryNumberString = (arg1, arg2) =>
                                {
                                    if ((long)arg2 == arg1.LongCount<int>() - 1)
                                    {
                                        return "";
                                    }

                                    // Recursion does not work for a lambda expression
                                    return arg1.ElementAt<int>(arg2) + mantissaBinaryNumberString(arg1, arg2 + 1);
                                }*/

                                // Initialize delegate with a method
                                DPlaceHolder2 dPlaceHolder2 = ConvertToNumberString;

                                string mantissaAsNumberString = dPlaceHolder2(mantissaAsBinaryNumber, 0);
                                string exponentAsNumberString = dPlaceHolder2(exponentAsBinary, 0);

                                // Add 1 to get the significand because the mantissa is the fractional part in the range 0 to 1, and the
                                // integer part of the significand (the coefficient?) is in the range 1 to 2.
                                // Order of Arithmetic operations means that / (i.e. division) operations are executed before + (i.e. addition)
                                // operations.
                                //float significand =
                                // Note: that dividing the mantissa as an integer (i.e. the mantissa in integer form) by Math.Pow(x: 2, y: 10)
                                // gets the mantissa into the form of a fraction which is the way the mantissa should be.
                                double significand = 1 + Convert.ToInt32(mantissaAsNumberString, 2) / Math.Pow(x: 2, y: 10);

                                // Not sure yet why 63 is subtracted from the exponent but I think it has something to do with the
                                // bias, and I'm not sure why the exponent is reversed again.
                                // Update on the same day: I think that the reason 63 is subtracted from the exponent is to make sure
                                // that the exponent is less than or equal to 0, so eMax (i.e. max exponent) is 0. In order words, I suspect
                                // that the reason that 63 is subtracted from the exponent is to make sure that we have a negative exponent
                                // because if you have a negative exponent then the decimal number moves to the left of the significand
                                // when you multiple the significand and the exponentiation b^n. e^63 must be the maximum exponent.


                                // Note: that since recursion does not work in a lamda expression I will have to move this whole block of code
                                // to a static recursive method.
                                // Note: that I will need to change the type of the List<T> named realNumbers to double since both the
                                // significand, and exponentiation I multiple to get a real number are doubles.

                                DReverse dReverse = ReverseString;
                                Int32 testExponent = Convert.ToInt32(exponentAsNumberString, fromBase: 2);
                                Int32 test2Exponent = Convert.ToInt32(value: dReverse(exponentAsNumberString)/*(string)*/ /*exponentAsNumberString.Reverse<char>().ToString()*/, 2);

                                // IEnumerble<char> is less derived than String so assignment compatibility does not work.
                                string testIEnumerableToString = /*exponentAsNumberString.Reverse<char>().ToString();*/ dReverse(exponentAsNumberString);

                                // Unbiased exponent, since the highest the exponent can be is 63, just subtract 63 from the exponent. This
                                // makes the exponent always negative so that the decimal point always "floats" to the left, never to the
                                // right.
                                Int32 exponentInDecimalSystem = Convert.ToInt32(dReverse(exponentAsNumberString), fromBase: 2) - 63;

                                double numericalValueOfFloatingPointRepresentationOfANumber = significand * Math.Pow(x: 2, y: exponentInDecimalSystem);

                                realNumbers.Add(numericalValueOfFloatingPointRepresentationOfANumber);

                                //Console.WriteLine("\n\rrealNumbers:");
                                //realNumbers.ForEach((x) => { Console.WriteLine(x); });

                                //dPlaceHolder(bitArray, currentIndex + 16);
                            }
                        };

                        dPlaceHolder(bits/*, 0*/);
                        pixelImageData = realNumbers.ToArray();
                    }
                }

                // Stack.Pop() removes an element so add 1 to the count of elements in the stack.
                chunks.Add("chunk " + (dataChunksToProcess.Count + 1), chunk);
            }

            return chunks;
            
        }

        public delegate /*R*/void DPlaceHolder<in A/*, in B*//*, out R*/>(A a/*, B b*/ );

        public delegate string DPlaceHolder2(IEnumerable<int> sequence, int index);

        public static string ConvertToNumberString(IEnumerable<int> seq, int index)
            // Note: Make sure this method does not reverse the string
        {
            // Generic parameter <int> (i.e. <TSource> means that the input source is the data from the IEnumerable from which
            // LongCount<> is called.
            if ((Int64)index == seq.LongCount<int>())
            {
                return ""; 
            }

            return seq.ElementAt<int>(index) + ConvertToNumberString(seq, index + 1);
        }

        public delegate string DReverse(string str);

        // Todo: Use position of bytes to find out which chunk contains the bytes that represent the camera operator, e.g. Jane C B Gray.
        // Update: "Jane C B Gray" starts at index 24094 which means that the string "Jane C B Gray" is in chunk 5 which has a type of 10,
        // the operator info "Jane C B Gray" is in chunk 5 because chunk 5 starts at the byte position 24018. Since the type 10 is unique
        // in all data chunks, therefore the condition if (chunk.type == 10) can be used to indicate the chunk with the camera operator
        // info.

        public static async Task PrintBytesInFileAsync(string filePath)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

            byte[] buffer = new byte[0x1000000];

            int numRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

            byte[] wholeFile = new byte[numRead];

            Array.Copy(buffer, wholeFile, numRead);

            for (int index = 0; index < wholeFile.Length; index++)
            {
                Console.WriteLine("Index: " + index + $", byte: {wholeFile[index]}");
            }
        }

        private static UInt16[] ToUInt16Array(byte[] bytes)
        {
            ushort[] arrayOfUInt16 = new UInt16[bytes.Length / 2];

            for(int i = 0, j = 0; j < bytes.Length - 1; j += 2, i++)
            {
                arrayOfUInt16[i] = BitConverter.ToUInt16(bytes, j);
            }

            return arrayOfUInt16;
        }

        public static void SearchAndReplace(string pattern1, string pattern2, string pattern3, string pattern4, string str1, string str2,
                                                        string str3, string str4)
        {   // Todo: Modify copy
            // Todo: Anonymize copy
            // Todo: This current SearchAndReplace method inserts the string into the binary file and shifts the bytes to the right
            // therefore changing the structure of the E2E file and adding bytes to it. To solve this problem may be use the entire
            // entry with padded null bytes as a pattern and replace this entire entry with a new entry that contains the replacement
            // string padded with the number of null bytes calculated from total bytes in the new entry minus the number of non-null bytes
            // at the end of the sequence of bytes in that entry. This way I am always replacing the same number of bytes so the size
            // of the E2E file should remain the same after the E2E file is anonymized.
            // Note: that perl uses the special character \0 to match a null character i.e. ^@ in a binary file.

            // Remove trailing white spaces.
            // Note: that a null byte is a white space character.
            // Update: Not sure whether a Null byte is a white space character, instead I think a null byte (i.e. the unsigned integer 0)
            // is just the special character \0 which means ___.
            /*pattern1 = pattern1.TrimEnd(trimChar: '\0');
            pattern2 = pattern2.TrimEnd('\0');
            pattern3 = pattern3.TrimEnd('\0');
            pattern4 = pattern4.TrimEnd('\0');*/

            Tuple<string, string, String, String> quadruple = PadWithNullCharacters(str1, str2, str3, str4);

            // Copy file
            //string fileName = GetDirectory(filePath);
            string newFile = GetDirectory(filePath) + GetFileName(filePath).Insert(GetFileName(filePath).Length - 4, "Copy");
            // -4 because of the three characters E 2 E and then subtract 1 to get the index, or in other words, -4 because you subtract
            // 1 to get the index and then the fourth index from the end of the string ASLAM01T.E2E is the character . which is where the
            // string "Copy" will be inserted, thereby shifting the character '.' one index to the right. 
            File.Copy(sourceFileName: filePath, destFileName: GetDirectory(filePath) +
                GetFileName(filePath).Insert(startIndex: GetFileName(filePath).Length - 4, value: "Copy"));

            // I think the warning "Can't find string terminator "'" anywhere before EOF at -e line 1." is because the second ' (i.e. the
            // string terminator ') is not on line 1 due to the concetenation. So put the whole string on one line.
            /*string test = $"-i -pe 's/{pattern1}/{str1}/; s/{pattern2}/{str2}/; " +
                $"s/{pattern3}/{str3}/; s/{pattern4}/{str4}/'" + " " + newFile;*/

            string test2 = $"-i -pe \'s/{pattern1}/{str1}/; s/{pattern2}/{str2}/; s/{pattern3}/{str3}/; s/{pattern4}/{str4}/\'" + " " + newFile;

            //string test3 = $"-i -pe \"s/{pattern1}/{quadruple.Item1}/\"" + " " + newFile;
            //string test3 = $"-i \"s/{pattern1}/{quadruple.Item1}/\"" + " " + newFile;
            //string test3 = $"-i -pe \"s/{pattern1}/{str1}/\"" + " " + newFile;
            //string test3 = @"-i -pe ""s/M12870\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0/HN4696\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0/""" + " " + newFile;

            /*
            // To solve the problem of C Sharp interpreting \0 you have to apply @ at the source of the string (i.e. when the string is
            // created).
            string x = @"M12870\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
            string y = @"HN4696\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

            string test3 = $"-i -pe \"s/{x}/{y}/\"" + " " + newFile;*/


            // Todo: Stop C Sharp from interpreting string that contains null byte characters.
            // Update: Done by using verbatim string which is then passed to perl.
            // Update2: The first update is wrong because @ can only be used for a string literal.
            string argument = $"-i -pe \"s/{pattern1}/{quadruple.Item1/*str1*/}/; s/{pattern2}/{quadruple.Item2/*str2*/}/; s/{pattern3}/{quadruple.Item3/*str3*/}/; s/{pattern4}/{quadruple.Item4/*str4*/}/\"" + " " + newFile;

            Console.WriteLine("HERE IS THE VALUE OF THE VARIABLE ARGUMENT:");
            Console.WriteLine(argument);

            // Process I want to start
            ProcessStartInfo processStartInfo = new ProcessStartInfo(fileName: /*"sed"*/"perl"/*"/usr/bin/perl"*/, /*$"-i -pe \'s/{pattern1}/{str1}/; s/{pattern2}/{str2}/; " +
                $"s/{pattern3}/{str3}/; s/{pattern4}/{str4}/\'" + " " + newFile*//*test*/argument);

            processStartInfo.UseShellExecute = true;

            // Rename the variable process to perl. Done
            Process perl = new Process();

            perl.StartInfo = processStartInfo;

            // Start the instance of a program specified by the Process.StartInfo property of this Process component (i.e. object), and
            // associate it with the process component (i.e. process object).
            perl.Start();
        }

        // Todo: Test whether g (i.e. global) is needed in the substitute commands.
        // Update: g (i.e. global) is not needed because without g the substitute command only replaces the first occurrence of the
        // matching pattern.

        public static string GetDirectory(string filePath)
        {
            string DirectoryPath = "";

            for (int index = filePath.Length - 1; index >= 0; index--)
            {
                if (filePath[index] == '/')
                {
                    DirectoryPath = filePath.Remove(index + 1);
                    break;
                }
            }

            return DirectoryPath;
        }

        public static string GetFileName(string filePath)
        {
            string fileName = "";

            for (int index = filePath.Length - 1; index >= 0; index--)
            {
                if (filePath[index] == '/')
                {
                    fileName = filePath.Substring(startIndex: index + 1);
                    break;
                }
            }

            return fileName;
        }

        public static Tuple<string, String, string, string> PadWithNullCharacters(string str1, string str2, String str3, string str4)
        {
            // Positional prameters must be in the order patient identifer, given name, surname, and full name of operator
            UInt32 numTrailingNullCharsInNewPatientIdentifierEntry = (UInt32) (Patient_identifier_entry_length - str1.Length);
            Int32 numTrailingNullCharsInNewGivenNameEntry = Given_name_entry_length - str2.Length;
            int numTrailingNullCharsInNewSurnameEntry = Surname_entry_length - str3.Length;
            uint numTrailingNullCharsInFullNameOfOperatorEntry = (UInt32)Full_name_of_operator_entry_length - (uint)str4.Length;

            for (int count = 1; count <= numTrailingNullCharsInNewPatientIdentifierEntry; count++)
            {
                str1 += "\\0";
            }

            for (int count = 1; count <= numTrailingNullCharsInNewGivenNameEntry; count++)
            {
                str2 += "\\0";
            }

            for (int count = 1; count <= numTrailingNullCharsInNewSurnameEntry;count++)
            {
                str3 += "\\0";
            }

            for (int count = 1; count <= numTrailingNullCharsInFullNameOfOperatorEntry;count++)
            {
                str4 += "\\0";
            }

            return new Tuple<string, string, String, String>(str1, str2, str3, str4);
        }

        public static string ReverseString(string str)
        {
            var strBuilder = new StringBuilder();

            for (int index = str.Length - 1; index >= 0; index--)
            {
                strBuilder.Append(str[index]);
            }

            return strBuilder.ToString();
        }

        // Todo: Read and interpret laterality, and image data (, and may be contour data too if it is necessary). Store all image data
        // in a data structure such as a list, array or stack (automatic growing may be useful). Then create a Dicom class that uses
        // a standard library that converts an array of image data to DICOM.

    }
}
