using System;
namespace E2EFileInterpreter
{
    public struct Header
    {
        String magic1;
        UInt32 version;
        UInt16[] unknown;
        UInt16 unknown2;

        public Header(string magic1, uint version, ushort[] unknown, ushort unkown2)
        {
            this.magic1 = magic1;
            this.version = version;
            this.unknown = unknown;
            this.unknown2 = unkown2;
        }
    }
}
