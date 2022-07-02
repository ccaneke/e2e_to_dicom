/* Copyright (C) Interneuron, Inc - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Chukwuemezie Aneke <ccanekedev@gmail.com>, May 2020
 */

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
