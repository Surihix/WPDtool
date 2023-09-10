using System;
using System.IO;
using System.Text;

namespace WPDtool.SupportClasses
{
    internal static class CmnMethods
    {
        public static void ErrorExit(string errorMsg)
        {
            Console.WriteLine(errorMsg);
            Console.ReadLine();
            Environment.Exit(0);
        }


        public enum ActionEnums
        {
            u,
            r
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


        public static void ExCopyTo(this Stream source, Stream destination, long offset, long count, int bufferSize = 81920)
        {
            // Seek to the given offset of the source stream
            var returnAddress = source.Position;
            source.Seek(offset, SeekOrigin.Begin);

            // Copy the data in chunks of bufferSize bytes until all are done
            var bytesRemaining = count;
            while (bytesRemaining > 0)
            {
                var readSize = Math.Min(bufferSize, bytesRemaining);
                var buffer = new byte[readSize];
                _ = source.Read(buffer, 0, (int)readSize);

                destination.Write(buffer, 0, (int)readSize);
                bytesRemaining -= readSize;
            }

            // Seek the source stream back to where it was
            source.Seek(returnAddress, SeekOrigin.Begin);
        }
    }
}