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

using System.Dynamic;

using Randomizer = AnonymizationLibrary.Randomizer;

using System.Text.Encodings;
using System.Reflection;
using Newtonsoft.Json;

namespace E2EFileInterpreter
{
    
    public class Program
    {
        static Int64 position;
        static int count;

        static Stack<Tuple<UInt32, uint>> dataChunksToProcess = new Stack<Tuple<uint, uint>>();

        static int Given_name_entry_length { get; set; }
        static int Surname_entry_length { get; set; }
        static int Patient_identifier_entry_length { get; set; }
        static int Full_name_of_operator_entry_length { get; set; }

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

        private static double[][] _decimalValuesMappedBetween0And255;

        public static double[][] DecimalValuesMappedBetween0And255
        {
            get
            {
                return _decimalValuesMappedBetween0And255;
            }

            set
            {
                _decimalValuesMappedBetween0And255 = value;
            }
        }

        static List<ValueTuple<UInt32, uint>> dimensionsOfFundusImages = new List<ValueTuple<UInt32, uint>>();
        static List<ValueTuple<uint, UInt32>> dimensionsOfTomogramSliceImages = new List<ValueTuple<UInt32, uint>>();

        private static List<UInt16[]> scaledNumbers = new List<UInt16[]>();

        private static string e2eFilePath;
        private static string anonymizedE2eDirectory;
        private static string imagesDirectory;
        private static string dicomDirectory;

        public static string GuidString { get; set; }

        // A single .E2E file should be passed as an argument.
        /*private*/ public static async Task<Int32> Main(string[] args)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string configFilePath = Path.Combine(exeDir, "config.json");

            string settings = File.ReadAllText(configFilePath);

            Settings settingsObj = JsonConvert.DeserializeObject<Settings>(settings);

            string slash = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";

            settingsObj.AnonymizedE2eDirectory = settingsObj.AnonymizedE2eDirectory.EndsWith(slash) ? settingsObj.AnonymizedE2eDirectory : settingsObj.AnonymizedE2eDirectory + slash;
            settingsObj.DicomDirectory = settingsObj.DicomDirectory.EndsWith('/') || settingsObj.DicomDirectory.EndsWith('\\') ? settingsObj.DicomDirectory : settingsObj.DicomDirectory + slash;
            settingsObj.ImagesDirectory = settingsObj.ImagesDirectory.EndsWith(slash) ? settingsObj.ImagesDirectory : settingsObj.ImagesDirectory + slash;

            //e2eFilePath = settingsObj.SourceE2eFilePath;
            e2eFilePath = args[0];
            anonymizedE2eDirectory = settingsObj.AnonymizedE2eDirectory;
            imagesDirectory = settingsObj.ImagesDirectory;
            dicomDirectory = settingsObj.DicomDirectory;

            string copy = $"{anonymizedE2eDirectory}{GetFileName(e2eFilePath).Insert(startIndex: GetFileName(e2eFilePath).Length - 4, value: "Copy")}";
            if (File.Exists(path: copy))
            {
                File.Delete(copy);
            }

            try
            {
                new DirectoryInfo(imagesDirectory).GetFiles();
            } catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(imagesDirectory);
            }

            List<object> list = new List<object>();
            
            await foreach (var item in HeaderAsync(Program.e2eFilePath, 0))
            {
                list.Add(item);
            }

            Header header = new Header(list[0] as string, (uint)list[1], list[2] as ushort[], (UInt16)list[3]);

            list.Clear();
            await foreach (var item in MainDirectoryAsync(Program.e2eFilePath))
            {

                list.Add(item);

            }

            object obj = new MainDirectory();

            MainDirectory mainDirectory = new MainDirectory(list[0], list[1], list[2], list[3], list[4], list[5], list[6], list[7]);

            await TraverseListOfDirectoryChunks(mainDirectory.current, Program.e2eFilePath);

            Dictionary<string, Dictionary<string, object>> chunks = await ReadDataChunksAsync(filePath: Program.e2eFilePath);

            string patientInfoChunk = FindChunk(ChunkType.patientInfo, chunks);

            string patient_identifier = (String)chunks[patientInfoChunk]["patient_identifier"];

            Randomizer randomizer = new Randomizer(patient_identifier.Replace("\0", string.Empty));
            randomizer.shuffleCharacters();

            string anonymized_patient_identifier = randomizer.randomizedMrn;

            string pseudo_given_name = "Patient";

            string[] familyNames = null;

            string outputDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string familyNamesDirectoryPath = Path.Combine(outputDirectoryPath, $"Surnames{slash}");
            string familyNamesFilePath = new Uri(familyNamesDirectoryPath + "family_names.txt").LocalPath;

            try
            {
                familyNames = await File.ReadAllLinesAsync(familyNamesFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(nameof(outputDirectoryPath) + ": " + outputDirectoryPath);
                Console.WriteLine(nameof(familyNamesFilePath) + ": " + familyNamesFilePath); 
            }

            Random randomNumberGenerator = new Random();

            string anonymized_surname = familyNames[randomNumberGenerator.Next(maxValue: familyNames.Length)];

            string anonymized_full_name_of_operator = "Mrs Camera Operator";

            var operatorNameChunk = FindChunk(ChunkType.operatorInfo, chunks);

            Substitute(e2eFilePath, patient_identifier,(string) chunks[patientInfoChunk]["given_name"], (string) chunks[patientInfoChunk]["surname"],
                (String)chunks[operatorNameChunk]["full_name_of_operator"], anonymized_patient_identifier, pseudo_given_name,
                anonymized_surname, anonymized_full_name_of_operator);

            CreateDicom(pseudo_given_name, anonymized_surname, anonymized_patient_identifier, imagesDirectory);

            return (int) ExitCode.success;
        }

        public static async IAsyncEnumerable<object> HeaderAsync(string filePath, Int64 positionWithinStream)
        {
            using (FileStream dataSourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096,
                                                        true))
            {
                count += 1;

                dataSourceStream.Position = positionWithinStream;

                byte[] buffer = new byte[0x10000];
                int numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 12);

                byte[] array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                yield return Encoding.UTF8.GetString(array);

                numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 4);
                array = new byte[numRead];

                Array.Copy(buffer, array, length: 4);

                string decodedWithUTF8 = Encoding.UTF8.GetString(array);
                string decodedText = Encoding.UTF32.GetString(array);

                Int32 num = (array[3] << 24) | (array[2] << 16) | (array[1] << 8) | (array[0]);

                yield return (UInt32)num;
                
                numRead = await dataSourceStream.ReadAsync(buffer, offset: 0, 18 /*54*/ /*maxBytes*/);
                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                UInt16 num2 = (ushort)((array[1] << 8) | (array[0]));

                UInt16[] u16Array = new ushort[9];

                Action<byte[], UInt16[]> convert = (arr, arr2) =>
                {
                    for (int index = 0, j = 0; index < arr.Length; index += 2, j++)
                    {

                        arr2[j] = BitConverter.ToUInt16(arr, startIndex: index);
                        
                    }
                };

                convert(array, u16Array);

                yield return u16Array;

                numRead = await dataSourceStream.ReadAsync(buffer, 0, count: 2 /*bytesToRead*/);

                array = new byte[numRead];

                Array.Copy(buffer, array, length: numRead);

                UInt16 positiveNumber = (UInt16)((array[1] << 8) | array[0]);

                yield return positiveNumber;

                position = dataSourceStream.Position;
            }


        }

        public static async IAsyncEnumerable<Object> MainDirectoryAsync(String filePath)
        {
            

            await foreach (var item in HeaderAsync(filePath, position))
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
                long currentPositionWithinStream = sourceStream.Seek(currentPosition, SeekOrigin.Begin);

                long positionTest = sourceStream.Position;

                byte[] buffer = new byte[0x1000];
                int numRead = await sourceStream.ReadAsync(buffer, 0, 12);

                byte[] array = new byte[numRead];

                Array.Copy(buffer, array, numRead);


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

                
                for (; numEntries >= 1; numEntries--)
                {
                    
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

        private static async Task<Dictionary<string, Dictionary<string, object>>> ReadDataChunksAsync(string filePath)
        {
            

            Dictionary<string, Dictionary<string, object>> chunks =
                                                                    new Dictionary<string, Dictionary<string, object>>();

            FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            byte[] buffer = new byte[0x1000];

            
            List<byte[]> rawFundusImages = new List<byte[]>();

            List<double[]> rawTomogramSliceImages = new List<double[]>();

            List<double[]> MappedDecimalValuesForTomogramSlices = new List<double[]>();

            for (int index = dataChunksToProcess.Count - 1; index >= 0; index--)
            {
                Dictionary<string, object> chunk = new Dictionary<string, object>();
                int startingPositionOfChunk = (int)dataChunksToProcess.Pop().Item1;
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

                    Given_name_entry_length = thirtyOneBytes.Length;
                    Surname_entry_length = sixtySixBytes.Length;
                    Patient_identifier_entry_length = thirtyBytes.Length;
                }
                else if (type == 10)
                {
                    numRead = await sourceStream.ReadAsync(buffer, 0, 16);
                    byte[] sixteenBytes = new byte[numRead];

                    Array.Copy(buffer, sixteenBytes, numRead);

                    UInt16[] unknown6 = ToUInt16Array(sixteenBytes);

                    numRead = await sourceStream.ReadAsync(buffer, 0, 36);

                    byte[] thirtySixBytes = new byte[numRead];
                    
                    Array.Copy(buffer, thirtySixBytes, numRead);

                    string fullNameOfOperator = Encoding.UTF8.GetString(thirtySixBytes);

                    chunk.Add(nameof(unknown6), unknown6);
                    chunk["full_name_of_operator"] = fullNameOfOperator;
                    
                    Full_name_of_operator_entry_length = thirtySixBytes.Length;
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
                    
                    imageWidth = width;
                    imageHeight = height;

                    if (ind == 0)
                    {
                        
                        buffer = new byte[0x100000];
                        numRead = await sourceStream.ReadAsync(buffer, 0, (int)height * (int)width);

                        
                        byte[] raw_fundus_image = new byte[height * width];
                        Array.Copy(buffer, raw_fundus_image, numRead);

                        rawFundusImages.Add(raw_fundus_image);


                        dimensionsOfFundusImages.Add(new ValueTuple<UInt32, uint>(width, height));
                    } else
                    {

                        buffer = new byte[0x100000];
                        numRead = await sourceStream.ReadAsync(buffer, 0, (int)(height * width) * 2);
                        byte[] tomogramImageData = new byte[numRead];

                        Array.Copy(buffer, tomogramImageData, numRead);

                        
                        BitArray bits = new BitArray(tomogramImageData);

                        
                        List<Double> realNumbers = new List<double>();

                        

                        int countOfStacks = 0;

                        
                        DPlaceHolder<BitArray> dPlaceHolder = null;

                        dPlaceHolder = (BitArray bitArray) =>
                        {
                            for (int currentIndex = 0; currentIndex </*=*/ bitArray.Length; currentIndex += 16)
                            {
                                
                                countOfStacks += 1;

                                bool[] floatingPointRepresentation = new bool[16];

                                
                                int end = currentIndex + 16;

                                int begin = currentIndex;
                                for (int i = 0; /*index*/ begin < end/*index + 16*/; /*index++*/begin++, i++)
                                {
                                    
                                    floatingPointRepresentation[i] = bitArray[/*index*/begin];

                                }

                                bool[] mantissa = new bool[10];

                                bool[] exponent = new bool[6];

                                for (int i = 0; i < floatingPointRepresentation.Length; i++)
                                {
                                    if (i < 10)
                                    {
                                        mantissa[i] = floatingPointRepresentation[i];
                                    }
                                    else
                                    {
                                        
                                        exponent[i - 10] = floatingPointRepresentation[i];
                                    }
                                }

                                
                                IEnumerable<int> mantissaAsBinaryNumber = mantissa.Select<bool, int>((x) =>
                                {
                                    int bit = Convert.ToInt32(x);

                                    return bit;
                                });

                                
                                Convert.ToInt32("1011", 2);
                                

                                IEnumerable<int> exponentAsBinary = exponent.Select<bool, int>((x) =>
                                {
                                    int bit = Convert.ToInt32(x);
                                    return bit;
                                });

                                DPlaceHolder2 dPlaceHolder2 = ConvertToNumberString;

                                string mantissaAsNumberString = dPlaceHolder2(mantissaAsBinaryNumber, 0);
                                string exponentAsNumberString = dPlaceHolder2(exponentAsBinary, 0);


                                double significand = 1 + Convert.ToInt32(mantissaAsNumberString, 2) / Math.Pow(x: 2, y: 10);

                                DReverse dReverse = ReverseString;
                                Int32 testExponent = Convert.ToInt32(exponentAsNumberString, fromBase: 2);
                                Int32 test2Exponent = Convert.ToInt32(value: dReverse(exponentAsNumberString), 2);

                                string testIEnumerableToString = dReverse(exponentAsNumberString);

                                Int32 exponentInDecimalSystem = Convert.ToInt32(dReverse(exponentAsNumberString), fromBase: 2) - 63;

                                double numericalValueOfFloatingPointRepresentationOfANumber = significand * Math.Pow(x: 2, y: exponentInDecimalSystem);

                                realNumbers.Add(numericalValueOfFloatingPointRepresentationOfANumber);

                                
                            }
                        };

                        dPlaceHolder(bits/*, 0*/);


                        rawTomogramSliceImages.Add(realNumbers.ToArray<double>());
                        pixelImageData = realNumbers.ToArray();

                        dimensionsOfTomogramSliceImages.Add(new ValueTuple<uint, UInt32>(width, height));
                    }
                }

                
                chunks.Add("chunk " + (dataChunksToProcess.Count + 1), chunk);
            }

            Raw_Fundus_Images_As_Array = rawFundusImages.ToArray();

            foreach (double[] element in rawTomogramSliceImages)
            {
                double[] mappedDecimalValues = new double[element.Length];
                int index = 0;
                foreach (double d in MapToByte(element))
                {
                    mappedDecimalValues[index] = d;
                    index += 1;
                }
                // Test
                int test = index;

                MappedDecimalValuesForTomogramSlices.Add(mappedDecimalValues);
            }

            int testRun = 254;
            byte testRun2 = 254;

            int testRun3 = Convert.ToInt32(testRun2);

            
            byte testRun4 = Convert.ToByte(value: testRun);

            DecimalValuesMappedBetween0And255 = MappedDecimalValuesForTomogramSlices.ToArray<double[]>();

            return chunks;
        }

        public delegate void DPlaceHolder<in A>(A a);

        public delegate string DPlaceHolder2(IEnumerable<int> sequence, int index);

        public static string ConvertToNumberString(IEnumerable<int> seq, int index)
        
        {

            if ((Int64)index == seq.LongCount<int>())
            {
                return "";
            }

            return seq.ElementAt<int>(index) + ConvertToNumberString(seq, index + 1);
        }

        public delegate string DReverse(string str);


        public static IEnumerable<double> MapToByte(double[] doubles)
        {
            foreach (double d in doubles)
            {
                yield return 256 * Math.Pow(x: d, y: 1.0 / 2.4);
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
                } else if (filePath[index] == '\\')
                {
                    fileName = filePath.Substring(startIndex: index + 1);
                    break;
                }
            }

            return fileName;
        }

        public static Tuple<string, String, string, string> PadWithNullCharacters(string str1, string str2, String str3, string str4)
        {
            UInt32 numTrailingNullCharsInNewPatientIdentifierEntry = (UInt32)(Patient_identifier_entry_length - str1.Length);
            Int32 numTrailingNullCharsInNewGivenNameEntry = Given_name_entry_length - str2.Length;
            int numTrailingNullCharsInNewSurnameEntry = Surname_entry_length - str3.Length;
            uint numTrailingNullCharsInFullNameOfOperatorEntry = (UInt32)Full_name_of_operator_entry_length - (uint)str4.Length;

            for (int count = 1; count <= numTrailingNullCharsInNewPatientIdentifierEntry; count++)
            {
                str1 += "\0";
            }

            for (int count = 1; count <= numTrailingNullCharsInNewGivenNameEntry; count++)
            {
                str2 += "\0";
            }

            for (int count = 1; count <= numTrailingNullCharsInNewSurnameEntry; count++)
            {
                str3 += "\0";
            }

            for (int count = 1; count <= numTrailingNullCharsInFullNameOfOperatorEntry; count++)
            {
                str4 += "\0";
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

        public static void CreateDicom(string firstName, string lastName, string patientNumber, string imagesdirectory)
        {
            char slash = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\\' : '/';
            _ = Directory.CreateDirectory(imagesDirectory + slash + GuidString);

            for (int index = 0; index < DecimalValuesMappedBetween0And255.Length; index++)
            {
                ImageFromTomogramSliceImageBytes(index, imagesdirectory);
            }

            for (int index = 0; index < Raw_Fundus_Images_As_Array.Length; index++)
            {
                ImageFromFundusImageBytes(index, imagesdirectory);
            }

            BitmapToDicom.ImportImages(imagesdirectory, firstName, lastName, patientNumber, dicomDirectory, GuidString, e2eFilePath);
        }

        public static void ImageFromTomogramSliceImageBytes(int index, string imagesDirectory)
        {
            int width = (int)dimensionsOfTomogramSliceImages[index].Item1;
            int height = (int)dimensionsOfTomogramSliceImages[index].Item2;
            var output = new Bitmap(width: width, height: height);

            DMovingWindow createTomogramImage = MovingWindow;

            double testRun = 3.0;
            byte testRun2 = (byte)testRun;
            double[] testRun3 = new double[100];
            
            dynamic dyn = new ExpandoObject();
            dyn.ArrayOfImageData = DecimalValuesMappedBetween0And255;

            int i = 0;
            do
            {
                dyn.ArrayOfImageData[i] = InvertIntensity(dyn.ArrayOfImageData[i]);
                i++;
            } while (i < dyn.ArrayOfImageData.Length);

            createTomogramImage(output, index, dyn);
            Console.WriteLine("THE INDEX IS "+index);
            
            var tomogramSliceImageFile = $"{imagesDirectory}{Program.GuidString}/tomogramSliceImage{/*index += 1*/++index/*index++*/}.bmp";
            Bitmap resizedTomogramSliceImage = ResizeImage(output, 768, 768);
            resizedTomogramSliceImage.Save(tomogramSliceImageFile, ImageFormat.Bmp);
        }

        public static void ImageFromFundusImageBytes(int index, string imagesDirectory)
        {

            int fundusImageWidth = (int)dimensionsOfFundusImages[index].Item1;
            int fundusImageHeight = (int)dimensionsOfFundusImages[index].Item2;

            dynamic propertyBag = new ExpandoObject();

            propertyBag.ArrayOfImageData = Raw_Fundus_Images_As_Array;

            int width = (int) dimensionsOfFundusImages[index].Item1;
            int height = (int)dimensionsOfFundusImages[index].Item2;

            Bitmap fundusBitmap = new Bitmap(width, height);

            DMovingWindow createFundusImage = MovingWindowFundus;

            createFundusImage(fundusBitmap, index, propertyBag);

            var fundusImageFile = $"{imagesDirectory}{GuidString}/fundusImage{index +=1}.bmp";
            Bitmap resizedFundusImage = ResizeImage(fundusBitmap, 768, 768);
            resizedFundusImage.Save(fundusImageFile, ImageFormat.Bmp);
        }


        public delegate void DMovingWindow(Bitmap bitmap, int index, dynamic obj);

        public static void MovingWindow(Bitmap bitmap, int index, dynamic obj)
        {
            foreach(double[] array in obj.ArrayOfImageData)
            {
                uint indexOfInnerArray = 0;
                foreach(double d in array)
                {
                    if (d > 255)
                    {
                        obj.ArrayOfImageData[index][indexOfInnerArray] = 255;
                    } else if (d < 0)
                    {
                        obj.ArrayOfImageData[index][indexOfInnerArray] = 0;
                    }
                    indexOfInnerArray += 1;
                }
            }

            int n = 1;
            int step = 0;
            for (int x = 0; x < bitmap.Width; x++, n++)
            {
                for (int y = 0, position = step; position < bitmap.Height * n; y++, position++)
                {
                    Color color = Color.FromArgb((int) obj.ArrayOfImageData[index][position], (int) obj.ArrayOfImageData[index][position], (int) obj.ArrayOfImageData[index][position]);
                    bitmap.SetPixel(x, y, color);
                }

                step += bitmap.Height;
            }
        }

        public static void MovingWindowFundus(Bitmap bitmap, int index, dynamic obj)
        {
            int n = 1;
            int step = 0;

            for (int x = 0; x < bitmap.Width; x++, n++)
            {
                for (int y = 0, position1 = step; position1 < bitmap.Height * n; y++, position1++)
                {
                    Color color = Color.FromArgb(obj.ArrayOfImageData[index][position1], obj.ArrayOfImageData[index][position1], obj.ArrayOfImageData[index][position1]);
                    bitmap.SetPixel(x, y, color);
                }

                step += bitmap.Height;
            }
        }


        public static Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            Size size = new Size(width, height);
            Bitmap resized = new Bitmap(image, size);
            return resized;
        }

        public static string FindChunk(ChunkType chunkType, Dictionary<string, Dictionary<string, object>> chunks)
        {
            string chunkName;
            switch (chunkType)
            {
                case ChunkType.patientInfo:
                    {
                        IEnumerable<string> patientInfochunkNameQuery =
                            from chunk in chunks
                            from entry in chunk.Value
                            where entry.Key.SequenceEqual<char>("patient_identifier")
                            select chunk.Key;

                        chunkName = patientInfochunkNameQuery.Single();

                        return chunkName;
                    }

                case ChunkType.operatorInfo:
                    {
                        IEnumerable<string> queryOperatorNameChunk =
                            from kvp in chunks
                            where kvp.Value.ContainsKey(key: "full_name_of_operator")
                            select kvp.Key;

                        string operatorNameChunk = queryOperatorNameChunk.Single<string>();

                        return operatorNameChunk;
                    }

                default:
                    {
                        chunkName = "Chunk not found";
                        break;
                    }
                    
            }
            return chunkName;
        }

        public enum ChunkType
        {
            patientInfo, operatorInfo
        }

        public static double[] InvertIntensity(double[] imageData)
        {
            IEnumerable<double> queryElements =
                from d in imageData
                let inverted = 255 - d
                select inverted;

            double[] invertedIntensities = queryElements.ToArray();

            return invertedIntensities;
        }

        public static void Substitute(string filePath, string str1, string str2, string str3, string str4, string replacement1, string replacement2,
            string replacement3, string replacement4)
        {
            Tuple<string, string, string, string> tuple = PadWithNullCharacters(replacement1, replacement2, replacement3, replacement4);
            replacement1 = tuple.Item1;
            replacement2 = tuple.Item2;
            replacement3 = tuple.Item3;
            replacement4 = tuple.Item4;

            string[] fileNames = new string[] {filePath};

            IEnumerable<string> queryStrings =
                from file in fileNames
                let bytes = /*File.ReadAllBytes(file)*/ ReadFile(file)
                let iso88591 = Encoding.GetEncoding("ISO-8859-1")
                let iso88591FileText = iso88591.GetString(bytes)
                let size = iso88591FileText.Length
                let result1 = iso88591FileText.Replace(str1, replacement1)
                let result2 = result1.Replace(str2, replacement2)
                let result3 = result2.Replace(str3, replacement3)
                let result4 = result3.Replace(str4, replacement4)
                select result4;

            string outputText = queryStrings.SingleOrDefault();

            string subDirectory = CreateUniqueE2eDirectory();

            string newFile = anonymizedE2eDirectory + subDirectory + '/' + GetFileName(filePath).Insert(GetFileName(filePath).Length - 4, "Copy");

            File.WriteAllText(path: newFile, contents: outputText, encoding: Encoding.GetEncoding("ISO-8859-1"));
        }

        // Todo: Rename to CreateSubDirectory
        public static string CreateUniqueE2eDirectory()
        {
            Guid guid = Guid.NewGuid();
            string guidString = guid.ToString();

            DirectoryInfo dir = null;

            try
            {
            /*DirectoryInfo*/ dir = new DirectoryInfo(path: $"{anonymizedE2eDirectory}{guidString}");

            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            } 

            try
            {
                dir.Create();

            } catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }

            GuidString = guidString;

            return dir.Name;
        }

        private static byte[] ReadFile(FileInfo fi)
        {
            byte[] content;

            if (fi.Exists && fi.Directory.Exists)
            {
                //byte[] content = new byte[0x10000000];
                content = File.ReadAllBytes(fi.FullName);
            } else
            {
                content = new byte[] { };
            }

            return content;
        }

        private static byte[] ReadFile(string fileName)
        {
            FileInfo file = new FileInfo(fileName);

            return ReadFile(file);
        }

        private enum ExitCode
        {
            success = 0, failed = 1
        }

    }
}
