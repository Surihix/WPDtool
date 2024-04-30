using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

internal static class BinaryReaderHelpers
{
    public static uint ReadBytesUInt32(this BinaryReader reader, bool isBigEndian)
    {
        var readValueBuffer = reader.ReadBytes(4);
        ReverseIfBigEndian(isBigEndian, readValueBuffer);

        return BitConverter.ToUInt32(readValueBuffer, 0);
    }

    public static string ReadStringTillNull(this BinaryReader readerName)
    {
        var sb = new StringBuilder();
        char chars;
        while ((chars = readerName.ReadChar()) != default)
        {
            sb.Append(chars);
        }

        return sb.ToString();
    }

    public static List<byte> ReadBytesTillNull(this BinaryReader reader)
    {
        var byteList = new List<byte>();
        byte currentValue;
        while ((currentValue = reader.ReadByte()) != default)
        {
            byteList.Add(currentValue);
        }

        return byteList;
    }

    static void ReverseIfBigEndian(bool isBigEndian, byte[] readValueBuffer)
    {
        if (isBigEndian)
        {
            Array.Reverse(readValueBuffer);
        }
    }
}