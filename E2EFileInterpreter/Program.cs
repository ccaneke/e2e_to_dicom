using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using AnonymizationLibrary;
using System.Diagnostics;
using Dicom;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using System.Drawing.Drawing2D;
//using System.Windows;
//using System.Drawing;


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

        private static byte[][] _raw_Fundus_Images_As_array;
        public static byte[][] Raw_Fundus_Images_As_Array
        {
            get
            { return _raw_Fundus_Images_As_array; }
            set
            { _raw_Fundus_Images_As_array = value; }
        }

        static UInt32 imageHeight;
        static uint imageWidth;

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
                }
                else
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

            Dictionary<string, Dictionary<string, object>> chunks = await ReadDataChunksAsync(filePath: Program.filePath/*filePath*/);

            // Print test
            //await PrintBytesInFileAsync(filePath);

            string patient_identifier = (String)chunks["chunk 47"]["patient_identifier"];

            Randomizer randomizer = new Randomizer(patient_identifier.Replace("\0", string.Empty));
            randomizer.shuffleCharacters();

            string anonymized_patient_identifier = randomizer.randomizedMrn;

            string pseudo_given_name = "Patient";

            string[] familyNames = null;

            try
            {
                familyNames = await File.ReadAllLinesAsync("/Users/christopheraneke/Projects/E2EFileInterpreter/E2EFileInterpreter/" +
                                "family_names.txt");
            }
            catch (Exception)
            {

            }

            Random randomNumberGenerator = new Random();

            string anonymized_surname = familyNames[randomNumberGenerator.Next(maxValue: familyNames.Length)];

            string anonymized_full_name_of_operator = "Mrs Camera Operator";

            patient_identifier = patient_identifier.Replace("\0", "\\0");

            // Calls to Replace() method must come after the four properties ending with length are assigned a value, in order to
            // avoid breaking the PadWithNullCharacters method.
            SearchAndReplace(patient_identifier, ((string)chunks["chunk 47"]["given_name"]).Replace("\0", "\\0"),
                ((String)chunks["chunk 47"]["surname"]).Replace("\0", "\\0"),
                ((String)chunks["chunk 5"]["full_name_of_operator"]).Replace("\0", "\\0"), anonymized_patient_identifier,
                pseudo_given_name, anonymized_surname, anonymized_full_name_of_operator);

            await CreateDicomAsync<string>(pseudo_given_name, anonymized_surname, anonymized_patient_identifier);
            //await
            //CreateDicomAsync<string>(pseudo_given_name, anonymized_surname, patientNumber: anonymized_patient_identifier);
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

                Array.Copy(buffer, array, length: 4);

                //byte[] test = new byte[4];
                char[] test2 = Encoding.UTF8.GetChars(array);

                // Test.
                string decodedWithUTF8 = Encoding.UTF8.GetString(array);
                string decodedText = Encoding.UTF32.GetString(array);
                int a = 'd';
                float b = 'd';
                UInt32 c = 'd';

                Int32 num = (array[3] << 24) | (array[2] << 16) | (array[1] << 8) | (array[0]);

                //yield return float.Parse(Encoding.UTF32.GetString(array)/*Encoding.UTF8.GetString(array)*/);

                UInt32 testConverted = (UInt32)num;
                // Big endian is like a mirror image of little endian, and vice versa.
                UInt32 testConverted2 = (UInt32)(array[3] << 24) | (UInt32)(array[2] << 16) | (UInt32)(array[1] << 8) | array[0];
                UInt32 testConverted3 = (UInt32)((array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0]);
                yield return (UInt32)num;

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

                UInt16 num2 = (ushort)((array[1] << 8) | (array[0]));


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

                UInt16 positiveNumber = (UInt16)((array[1] << 8) | array[0]);


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

            await foreach (var item in HeaderAsync(filePath/*"/tmp/ASLAM01TAnonymized.E2E"*//*"/Users/christopheraneke/Downloads/SAMPLE_OCT.E2E"*/, position /*positionWithinStream*//*Program.position + 36*/))
            {
                yield return item;
            }

            List<FileStream> fileStreams = new List<FileStream>();

            try
            {
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, true);


                fileStream.Position = position /*+ 1*/;
                fileStreams.Add(fileStream);

                byte[] buffer = new byte[0x1000];

                int numRead = await fileStream.ReadAsync(buffer, 0, 4);

                byte[] array = new byte[numRead];

                Array.Copy(buffer, array, numRead);

                Int32 numEntries = (array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0];

                yield return (UInt32)numEntries;

                numRead = await fileStream.ReadAsync(buffer, 0, count: 4);

                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);


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

                UInt32 unknown = (UInt32)((array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0]);

                yield return unknown;

            }
            finally
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


            do
            {
                // Seek to the next directory chunk
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

            // In case there is more than one fundus image I need a list to hold all images:
            List<byte[]> rawFundusImages = new List<byte[]>();



            // In case there is more than one tomogram slice image I need a list to hold all images:
            List<double[]> rawTomogramSliceImages = new List<double[]>();

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
                    // Calls to Replace must come after remembering the length of entries of interest, in order to avoid breaking the
                    // function PadWithNullCharacters
                    givenName = givenName.Replace("\0", "\\0");
                    surname = surname.Replace("\0", "\\0");*/
                }
                else if (type == 10)
                {
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
                    // This must come last to avoid breaking the function PadWithNullCharacters
                    fullNameOfOperator = fullNameOfOperator.Replace("\0", "\\0");*/
                }
                else if (type == 0x0000000B)
                {
                    numRead = await sourceStream.ReadAsync(buffer, 0, 14);
                    byte[] fourteenBytes = new byte[numRead];

                    Array.Copy(buffer, fourteenBytes, numRead);

                    string unknownInLaterality = Encoding.UTF8.GetString(fourteenBytes);

                    numRead = await sourceStream.ReadAsync(buffer, 0, 1);
                    byte oneByte = buffer[0];

                    char laterality = (char)oneByte;

                }
                else if (type == 0x40000000)
                {
                    UInt32 sizeOfImage = await GetU32EntryAsync(sourceStream, buffer);
                    UInt32 typeOfImage = await GetU32EntryAsync(sourceStream, buffer);
                    uint unknownInImageData = await GetU32EntryAsync(sourceStream, buffer);
                    uint width = await GetU32EntryAsync(sourceStream, buffer);
                    UInt32 height = await GetU32EntryAsync(sourceStream, buffer);
                    int test4 = (int)height * (int)width;
                    int test5 = (int)(height * width);
                    int typeOfImageTest1 = 0x02010201; // fundus
                    int typeOfImageTest2 = 0x02200201; // tomogram
                    var typeOfImageHexDataTest = 0x02010201; // fundus

                    imageWidth = width;
                    imageHeight = height;

                    if (ind == 0)
                    {
                        // Use larger array when the number of bytes height * wdith (i.e. 380928) is being written to the buffer.
                        buffer = new byte[0x100000];
                        numRead = await sourceStream.ReadAsync(buffer, 0, (int)height * (int)width);

                        // No need for bit shift because each element of array is just a byte
                        byte[] raw_fundus_image = new byte[height * width];
                        Array.Copy(buffer, raw_fundus_image, numRead);

                        rawFundusImages.Add(raw_fundus_image);
                    }
                    else
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

                            for (int currentIndex = 0; currentIndex </*=*/ bitArray.Length; currentIndex += 16)
                            {
                                //BitConverter.ToSingle()
                                //new byte[2].Window
                                // Tests
                                countOfStacks += 1;

                                bool[] floatingPointRepresentation = new bool[16];

                                //int a = (int) true;

                                //const int end = index;
                                int end = currentIndex + 16;

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

                                IEnumerable<int> exponentAsBinary = exponent.Select<bool, int>((x) =>
                                {
                                    int bit = Convert.ToInt32(x);
                                    return bit;
                                });

                                // Initialize delegate with a method
                                DPlaceHolder2 dPlaceHolder2 = ConvertToNumberString;

                                string mantissaAsNumberString = dPlaceHolder2(mantissaAsBinaryNumber, 0);
                                string exponentAsNumberString = dPlaceHolder2(exponentAsBinary, 0);


                                double significand = 1 + Convert.ToInt32(mantissaAsNumberString, 2) / Math.Pow(x: 2, y: 10);

                                DReverse dReverse = ReverseString;
                                Int32 testExponent = Convert.ToInt32(exponentAsNumberString, fromBase: 2);
                                Int32 test2Exponent = Convert.ToInt32(value: dReverse(exponentAsNumberString)/*(string)*/ /*exponentAsNumberString.Reverse<char>().ToString()*/, 2);

                                // IEnumerble<char> is less derived than String so assignment compatibility does not work.
                                string testIEnumerableToString = /*exponentAsNumberString.Reverse<char>().ToString();*/ dReverse(exponentAsNumberString);

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

            Raw_Fundus_Images_As_Array = rawFundusImages.ToArray();

            return chunks;

        }

        public delegate /*R*/void DPlaceHolder<in A/*, in B*//*, out R*/>(A a/*, B b*/ );

        public delegate string DPlaceHolder2(IEnumerable<int> sequence, int index);

        public static string ConvertToNumberString(IEnumerable<int> seq, int index)
        // Note: Make sure this method does not reverse the string
        {

            if ((Int64)index == seq.LongCount<int>())
            {
                return "";
            }

            return seq.ElementAt<int>(index) + ConvertToNumberString(seq, index + 1);
        }

        public delegate string DReverse(string str);


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

            for (int i = 0, j = 0; j < bytes.Length - 1; j += 2, i++)
            {
                arrayOfUInt16[i] = BitConverter.ToUInt16(bytes, j);
            }

            return arrayOfUInt16;
        }

        public static void SearchAndReplace(string pattern1, string pattern2, string pattern3, string pattern4, string str1, string str2,
                                                        string str3, string str4)
        {

            Tuple<string, string, String, String> quadruple = PadWithNullCharacters(str1, str2, str3, str4);


            string newFile = /*GetDirectory(filePath)*/"/tmp/" + GetFileName(filePath).Insert(GetFileName(filePath).Length - 4, "Copy");

            File.Copy(sourceFileName: filePath, destFileName: /*GetDirectory(filePath) +
                GetFileName(filePath).Insert(startIndex: GetFileName(filePath).Length - 4, value: "Copy")*/newFile);

            string test2 = $"-i -pe \'s/{pattern1}/{str1}/; s/{pattern2}/{str2}/; s/{pattern3}/{str3}/; s/{pattern4}/{str4}/\'" + " " + newFile;


            string argument = $"-i -pe \"s/{pattern1}/{quadruple.Item1/*str1*/}/; s/{pattern2}/{quadruple.Item2/*str2*/}/; s/{pattern3}/{quadruple.Item3/*str3*/}/; s/{pattern4}/{quadruple.Item4/*str4*/}/\"" + " " + newFile;

            Console.WriteLine("HERE IS THE VALUE OF THE VARIABLE ARGUMENT:");
            Console.WriteLine(argument);

            ProcessStartInfo processStartInfo = new ProcessStartInfo(fileName: /*"sed"*/"perl"/*"/usr/bin/perl"*/, /*$"-i -pe \'s/{pattern1}/{str1}/; s/{pattern2}/{str2}/; " +
                $"s/{pattern3}/{str3}/; s/{pattern4}/{str4}/\'" + " " + newFile*//*test*/argument);

            processStartInfo.UseShellExecute = true;

            Process perl = new Process();

            perl.StartInfo = processStartInfo;

            perl.Start();
        }

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
            UInt32 numTrailingNullCharsInNewPatientIdentifierEntry = (UInt32)(Patient_identifier_entry_length - str1.Length);
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

            for (int count = 1; count <= numTrailingNullCharsInNewSurnameEntry; count++)
            {
                str3 += "\\0";
            }

            for (int count = 1; count <= numTrailingNullCharsInFullNameOfOperatorEntry; count++)
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

        public static /*void*/ async Task CreateDicomAsync<A>(/*string*/A firstName, A/*string*/ lastName, /*string*/ A patientNumber)
        {

            /*DicomDataset dicomElements = new DicomDataset(internalTransferSyntax: DicomTransferSyntax.ExplicitVRBigEndian)
            {
                {DicomTag.DoubleFloatPixelData, pixelImageData },
                {DicomTag.PatientName, $"{firstName} {lastName}" },
                {DicomTag.PatientID, patientNumber },
                {DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage},
                {DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID()},
                {DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID()},
                {DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() }
            };*/

            //DicomDataset dicomElements = new DicomDataset(/*internalTransferSyntax: DicomTransferSyntax.ExplicitVRBigEndian*/)
            //{
            //    {DicomTag.PixelData, Raw_Fundus_Images_As_Array[0]},
            //    {DicomTag.PatientName, $"{firstName} {lastName}" },
            //    {DicomTag.PatientID, patientNumber },
            //    {DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage},
            //    {DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID()},
            //    {DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID()},
            //    {DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() }
            //};

            //DicomFile dicomFile = new DicomFile(dataset: dicomElements);

            //await dicomFile.SaveAsync("/tmp/convertedE2E.dcm");

            // The above way of converting image data to dicom file does not work, use bitmap and DicomObjects instead.

            MemoryStream ms = new MemoryStream(Raw_Fundus_Images_As_Array[0]);
            //ms.to
            MemoryStream ms2 = new MemoryStream();
            await ms2.WriteAsync(Raw_Fundus_Images_As_Array[0], 0, Raw_Fundus_Images_As_Array[0].Length);

            int width = (int)imageWidth/*100*/;
            int height = (int)imageHeight/*100*/;
            var output = new Bitmap(width: width, height: height);

            DMovingWindow createFundusImage = MovingWindow;

            createFundusImage(output);

            //var rect = new Rectangle(x: 0, y: 0, width: width, height: height);
            ////output.
            ////output.pi
            ////Color.FromArgb(1).Get
            //var bmpData = output.LockBits(rect: rect, flags: ImageLockMode.ReadWrite, format: /*output.PixelFormat*/ PixelFormat./*Format16bppGrayScale*/);
            //// memory pointer points to the bitmap
            //var ptr = bmpData.Scan0;
            //Marshal.Copy(source: Raw_Fundus_Images_As_Array[0], startIndex: 0, destination: ptr,
            //                                                                        length: Raw_Fundus_Images_As_Array[0].Length);
            //output.UnlockBits(bitmapdata: bmpData);

            output.Save(filename: "/tmp/fundus_image4.bmp", format: ImageFormat.Bmp);
            //output.Save(filename: "/tmp/fileToImport.jpg", format: ImageFormat.Jpeg);

            //File.WriteAllBytes(path: "/tmp/newFile.bmp", bytes: Raw_Fundus_Images_As_Array[0]);
            //Image image = Image.FromStream(stream: ms/*, useEmbeddedColorManagement: false, validateImageData: true*/);
            //image.

            // Windows Extension types are not supported on MacOS.
            //ImageConverter converter = new ImageConverter();
            //Image img = (Image)converter.ConvertFrom(value: Raw_Fundus_Images_As_Array[0]);
            //Ptr   
            //Bitmap bitmap = new Bitmap(/*original: image*/ms);
            //bitmap.SetPixel()
        }

        public delegate void DMovingWindow(Bitmap bitmap);

        public static void MovingWindow(Bitmap bitmap)
        {
            // Note: that when I am loop through all Fundus images in Raw_Fundus_Images_As_Array I will need to replace index 0 in
            // Raw_Fundus_Images_As_Array[0] with an outer loop variable that represents the indexes of Raw_Fundus_Images_As_Array.

            int n = 1;
            // start
            uint step = 0;

            for (int i = 0; i < imageHeight /** imageWidth*/; i++, n += 1)
            {

                for (uint j = step, x = 0; j < imageWidth * n; j++, x++)
                {
                    //new ColorPalette()
                    Color pixelColor = bitmap.GetPixel((int)x, i);
                    Color newColor = Color.FromArgb(/*pixelColor.R, 0, 0*//*alpha: Raw_Fundus_Images_As_Array[0][j],*/ red: Raw_Fundus_Images_As_Array[0][j], green: Raw_Fundus_Images_As_Array[0][j], blue: Raw_Fundus_Images_As_Array[0][j]);
                    bitmap.SetPixel(x: /*(int)j*/ (int)x, y: i, color: newColor);
                }

                step += imageWidth;
            }
        }

    }
}
