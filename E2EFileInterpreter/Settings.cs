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
