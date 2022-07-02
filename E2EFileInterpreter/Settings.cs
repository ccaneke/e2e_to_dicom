/* Copyright (C) Interneuron, Inc - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Chukwuemezie Aneke <ccanekedev@gmail.com>, May 2020
 */

using System;
namespace E2EFileInterpreter
{
    public struct Settings
    {
        public string SourceE2eFilePath { get; set; }
        public string AnonymizedE2eDirectory { get; set; }
        public string ImagesDirectory { get; set; }
        private string _dicomDirectory;
        public string DicomDirectory
        {
            get
            {
                return this._dicomDirectory;
            }
            set
            {
                _dicomDirectory = value;
            }
        }
    }
}
