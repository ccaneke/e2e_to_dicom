/* Copyright (C) Interneuron, Inc - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Chukwuemezie Aneke <ccanekedev@gmail.com>, May 2020
 */

using System;
namespace E2EFileInterpreter
{
    public struct MainDirectory
    {
        string magic2;
        uint version;
        UInt16[] unknown;
        UInt16 unknown2;
        public uint numEntries;
        public UInt32 current;
        uint unknown3;
        UInt32 unknown4;

        public MainDirectory(Object magic2, object version, object unknown, Object unknown2, object numEntries, object current,
            object unknown3, Object unknown4)
        {
            this.magic2 = magic2 as string;
            this.version = (UInt32)version;
            this.unknown = unknown as ushort[];
            this.unknown2 = (UInt16)unknown2;
            this.numEntries = (UInt32)numEntries;
            this.current = (uint)current;
            this.unknown3 = (UInt32)unknown3;
            this.unknown4 = (uint)unknown4;
        }
    }
}
