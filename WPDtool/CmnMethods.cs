﻿using System;

namespace WPDtool
{
    internal class CmnMethods
    {
        public static void ErrorExit(string errorMsg)
        {
            Console.WriteLine(errorMsg);
            Console.ReadLine();
            Environment.Exit(0);
        }

        public static string RecordsList = "!!WPD_Records.txt";
    }
}