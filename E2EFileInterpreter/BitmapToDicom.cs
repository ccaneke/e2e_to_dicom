/* Copyright (C) Interneuron, Inc - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Chukwuemezie Aneke <ccanekedev@gmail.com>, May 2020
 */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Dicom
{
    public class BitmapToDicom
    {
        public static void ImportImages(string imagesDirectory, string firstName, string lastName, string patientIdentifier, string dicomOutputDirectory, string guidString, string e2eFilePath)
        {
            string e2eImagesDirectory = imagesDirectory + guidString;
            string[] files = Directory.GetFiles(path: e2eImagesDirectory);

            IEnumerable<string> tomogramFilesQuery =
                from file in files
                where file.Contains(value: "tomogram")
                select file;

            MemoryByteBuffer[] tomogramBuffers = new MemoryByteBuffer[tomogramFilesQuery.Count()];

            int biggestRow = 0;
            int biggestColumn = 0;
            int index = 0;
            foreach (string file in tomogramFilesQuery)
            {
                Bitmap bitmap = new Bitmap(file);

                bitmap = GetValidImage(bitmap);
                int rows, columns;
                byte[] pixels = GetPixels(bitmap, out rows, out columns);
                MemoryByteBuffer buffer = new MemoryByteBuffer(pixels);
                tomogramBuffers[index] = buffer;

                if (rows > biggestRow)
                    biggestRow = rows;

                if (columns > biggestColumn)
                    biggestColumn = columns;

                index++;
            }

            IEnumerable<string> fundusFilesQuery =
                from file in files
                where file.Contains(value: "fundus")
                select file;

            MemoryByteBuffer[] fundusBuffers = new MemoryByteBuffer[fundusFilesQuery.Count()];
            int index2 = 0;
            foreach (String file in fundusFilesQuery)
            {
                Bitmap bitmap = new Bitmap(file);

                bitmap = GetValidImage(bitmap);
                int rows, columns;
                byte[] pixels = GetPixels(bitmap, out rows, out columns);
                MemoryByteBuffer buffer = new MemoryByteBuffer(pixels);
                fundusBuffers[index2] = buffer;

                if (rows > biggestRow)
                    biggestRow = rows;

                if (columns > biggestColumn)
                    biggestColumn = columns;

                index2++;
            }

            DicomDataset dataset = new DicomDataset();
            FillDataset(dataset, firstName, lastName, patientIdentifier);
            dataset.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation./*Monochrome2.Value*/Rgb.Value);

            UInt16[] test = new ushort[2] { 768, 768 };
            ushort[] test2 = new ushort[2] { 496, 768 };

            UInt16 frameRows = 768;
            ushort frameColumns = 768;
            dataset.Add(DicomTag.Rows, values: frameRows);
            dataset.Add(DicomTag.Columns, /*(ushort)*/frameColumns);
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
            DicomPixelData pixelData = DicomPixelData.Create(dataset, true);
            pixelData.BitsStored = 8;
            pixelData.SamplesPerPixel = 3;

            pixelData.HighBit = 7;
            pixelData.PixelRepresentation = 0;
            pixelData.PlanarConfiguration = 0;

            foreach (MemoryByteBuffer buffer in tomogramBuffers)
            {
                pixelData.AddFrame(data: buffer);
            }

            foreach (MemoryByteBuffer buffer in fundusBuffers)
            {
                pixelData.AddFrame(data: buffer);
            }

            Directory.CreateDirectory($"{dicomOutputDirectory}dicom_export/{guidString}");

            DicomFile dicomFile = new DicomFile(dataset);

            char[] charactersToTrim = new char[] { '.', 'E', '2', 'E' };
            string e2eFileNameWithoutExtension = new FileInfo(e2eFilePath).Name.ToUpper().TrimEnd(charactersToTrim);

            dicomFile.Save($"{dicomOutputDirectory}dicom_export/{guidString}/{e2eFileNameWithoutExtension}Dicom.dcm");
        }

        private static void FillDataset(DicomDataset dataset, string firstName, string lastName, string patientIdentifier)
        {

            //type 1 attributes.
            dataset.Add(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
            dataset.Add(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.Add(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.Add(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());

            //type 2 attributes
            dataset.Add(DicomTag.PatientID,patientIdentifier);
            dataset.Add(DicomTag.PatientName, $"{firstName} {lastName}");
            dataset.Add(DicomTag.PatientBirthDate, "00000000");
            dataset.Add(DicomTag.PatientSex, "M");
            dataset.Add(DicomTag.StudyDate, DateTime.Now);
            dataset.Add(DicomTag.StudyTime, DateTime.Now);
            dataset.Add(DicomTag.AccessionNumber, string.Empty);
            dataset.Add(DicomTag.ReferringPhysicianName, string.Empty);
            dataset.Add(DicomTag.StudyID, "1");
            dataset.Add(DicomTag.SeriesNumber, "1");
            dataset.Add(DicomTag.ModalitiesInStudy, "CR");
            dataset.Add(DicomTag.Modality, "CR");
            dataset.Add(DicomTag.NumberOfStudyRelatedInstances, "1");
            dataset.Add(DicomTag.NumberOfStudyRelatedSeries, "1");
            dataset.Add(DicomTag.NumberOfSeriesRelatedInstances, "1");
            dataset.Add(DicomTag.PatientOrientation, "FA", "FA");
            dataset.Add(DicomTag.ImageLaterality, "U");
        }

        private static Bitmap GetValidImage(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb)
            {
                Bitmap old = bitmap;
                using (old)
                {
                    bitmap = new Bitmap(old.Width, old.Height, PixelFormat.Format24bppRgb);
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.DrawImage(old, 0, 0, old.Width, old.Height);
                    }
                }
            }
            return bitmap;
        }
        private static byte[] GetPixels(Bitmap image, out int rows, out int columns)
        {
            rows = image.Height;
            columns = image.Width;

            if (rows % 2 != 0 && columns % 2 != 0)
                --columns;
            BitmapData data = image.LockBits(new Rectangle(0, 0, columns, rows), ImageLockMode.ReadOnly, image.PixelFormat);
            IntPtr bmpData = data.Scan0;
            try
            {
                int stride = columns * 3;
                int size = rows * stride;
                byte[] pixelData = new byte[size];
                for (int i = 0; i < rows; ++i)
                    Marshal.Copy(source: new IntPtr(value: bmpData.ToInt64() + i * data.Stride), pixelData, i * stride, length: stride);

                SwapRedBlue(pixelData);
                return pixelData;
            }
            finally
            {
                image.UnlockBits(data);
            }
        }
        private static void SwapRedBlue(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 3)
            {
                byte temp = pixels[i];
                pixels[i] = pixels[i + 2];
                pixels[i + 2] = temp;
            }
        }
    }
}