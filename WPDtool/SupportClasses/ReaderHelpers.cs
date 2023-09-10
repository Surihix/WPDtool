﻿using System;
using System.IO;

namespace BinaryReaderEx
{
    internal static class ReaderHelpers
    {
        public static ushort ReadBytesUInt16(this BinaryReader reader, bool isBigEndian)
        {
            var readValueBuffer = reader.ReadBytes(2);
            ReverseIfBigEndian(isBigEndian, readValueBuffer);

            return BitConverter.ToUInt16(readValueBuffer, 0);
        }

        public static uint ReadBytesUInt32(this BinaryReader reader, bool isBigEndian)
        {
            var readValueBuffer = reader.ReadBytes(4);
            ReverseIfBigEndian(isBigEndian, readValueBuffer);

            return BitConverter.ToUInt32(readValueBuffer, 0);
        }

        public static ulong ReadBytesUInt64(this BinaryReader reader, bool isBigEndian)
        {
            var readValueBuffer = reader.ReadBytes(8);
            ReverseIfBigEndian(isBigEndian, readValueBuffer);

            return BitConverter.ToUInt64(readValueBuffer, 0);
        }

        static void ReverseIfBigEndian(bool isBigEndian, byte[] readValueBuffer)
        {
            if (isBigEndian)
            {
                Array.Reverse(readValueBuffer);
            }
        }
    }
}