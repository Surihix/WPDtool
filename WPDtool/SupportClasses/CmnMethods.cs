using System;

namespace WPDtool.SupportClasses
{
    internal class CmnMethods
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

        public static string RecordsList = "!!WPD_Records";
    }
}