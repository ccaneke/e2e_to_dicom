﻿using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using AnonymizationLibrary;

namespace E2EFileInterpreter
{
    class Program
    {
        static Int64 position;
        static int count;

        // May be rename dataChunksToProcess to addresses of data chunks.
        static Stack<Tuple<UInt32, uint>> dataChunksToProcess = new Stack<Tuple<uint, uint>>();

        static async Task Main(string[] args)
        {
            List<object> list = new List<object>();


            //System.Collections.
            //await HeaderAsync("aaa");
            // Test Header read
            // N.B. Looks like IAsyncEnumerable makes the program await the entire foreach loop instead of awaiting the iterator method.
            await foreach (var item in HeaderAsync("/Users/christopheraneke/Downloads/ASLAM01T.E2E"/*"/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"*/, 0))
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
            await foreach (var item in MainDirectoryAsync("/Users/christopheraneke/Downloads/ASLAM01T.E2E"/*"/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"*/))
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
            string filePath = "/Users/christopheraneke/Downloads/ASLAM01T.E2E"/*"/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"*/;

            await TraverseListOfDirectoryChunks(mainDirectory.current, filePath);
            /*
             * I should probably remove this comment.
            // I tried finding the offset, assuming that the structure of the e2e file described by uocte holds but with an offset, but
            // it is futile and like searching for a needle in a haystack, never mind that there might not be any offset and the structure
            // of the e2e file described by uocte is not for this particular uocte file, may be because the structure described by uocte is
            // outdated. */

            Dictionary<string, Dictionary<string, object>> chunks = await ReadDataChunksAsync(filePath: filePath);

            // Print test
            await PrintBytesInFileAsync(filePath);

            string patient_identifier = (String) chunks["chunk 47"]["patient_identifier"];

            Randomizer randomizer = new Randomizer(patient_identifier);
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
            await foreach (var item in HeaderAsync("/Users/christopheraneke/Downloads/ASLAM01T.E2E"/*"/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"*/, position /*positionWithinStream*//*Program.position + 36*/))
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
                if (type == 0x00000009)
                {
                    numRead = await sourceStream.ReadAsync(buffer, 0, 31);
                    byte[] thirtyOneBytes = new byte[numRead];

                    Array.Copy(buffer, thirtyOneBytes, numRead);

                    string givenName = Encoding.UTF8.GetString(thirtyOneBytes);

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
                } else if (type == 10)
                {
                    // After the chunk structure described above there are 16 bytes between the last entry in the chunk structure (i.e.
                    // unknown5). And I am assuming that the 16 bytes are an array of 16 bit integers.
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
                }

                // Stack.Pop() removes an element so add 1 to the count of elements in the stack.
                chunks.Add("chunk " + (dataChunksToProcess.Count + 1), chunk);
            }

            return chunks;
            
        }

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
    }
}
