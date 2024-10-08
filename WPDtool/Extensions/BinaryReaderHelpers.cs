using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class BinaryReaderHelpers
{
    public static uint ReadBytesUInt32(this BinaryReader reader, bool isBigEndian)
    {
        var readValueBuffer = reader.ReadBytes(4);
        ReverseIfBigEndian(isBigEndian, readValueBuffer);

        return BitConverter.ToUInt32(readValueBuffer, 0);
    }


    public static string ReadBytesString(this BinaryReader reader, int readCount, bool isBigEndian)
    {
        var readValueBuffer = reader.ReadBytes(readCount);
        ReverseIfBigEndian(isBigEndian, readValueBuffer);

        return Encoding.UTF8.GetString(readValueBuffer).Replace("\0", "");
    }


    public static string ReadStringTillNull(this BinaryReader reader)
    {
        var sb = new StringBuilder();
        char chars;
        while ((chars = reader.ReadChar()) != default)
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


    private static void ReverseIfBigEndian(bool isBigEndian, byte[] readValueBuffer)
    {
        if (isBigEndian)
        {
            Array.Reverse(readValueBuffer);
        }
    }
}