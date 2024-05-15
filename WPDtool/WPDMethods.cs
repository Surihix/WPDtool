using System;
using System.Linq;

namespace WPDtool
{
    internal class WPDMethods
    {
        public static void ErrorExit(string errorMsg)
        {
            Console.WriteLine(errorMsg);
            Console.ReadLine();
            Environment.Exit(1);
        }

        public static string RecordsList = "!!WPD_Records.txt";

        public static string[] DataSplitChar = new string[] { " |-| " };

        static readonly char[] IllegalCharsArray = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        public static string RemoveIllegalChars(string inputString)
        {
            string processedString = "";
            foreach (char c in inputString)
            {
                if (IllegalCharsArray.Contains(c))
                {
                    processedString += "";
                }
                else
                {
                    processedString += c;
                }
            }

            return processedString;
        }
    }
}