using System;
using System.IO;
using WPDtool.WPDClasses;
using static WPDtool.SupportClasses.CmnMethods;

namespace WPDtool
{
    internal class Core
    {
        static void Main(string[] args)
        {
            if (args.Length > 2)
            {
                ErrorExit("Error: Enough arguments not specified\nMust be: '-u', '-r', or '-rb' and 'WPDfileOrFolder'");
            }

            var actionEnumString = args[0].Replace("-", "");

            var convertedAction = new ActionEnums();
            if (Enum.TryParse(actionEnumString, false, out ActionEnums resultEnum))
            {
                convertedAction = resultEnum;
            }

            var inWPDfileOrFolder = args[1];

            switch (convertedAction)
            {
                case ActionEnums.u:
                    if (!File.Exists(inWPDfileOrFolder))
                    {
                        ErrorExit("Error: Specified file does not exist");
                    }
                    WPD.UnpackWPD(inWPDfileOrFolder);
                    break;

                case ActionEnums.r:
                case ActionEnums.rb:
                    if (!Directory.Exists(inWPDfileOrFolder))
                    {
                        ErrorExit("Error: Specified directory to repack, does not exist");
                    }
                    WPD.RepackWPD(inWPDfileOrFolder, convertedAction);
                    break;

                default:
                    ErrorExit("Error: Proper tool action is not specified.\nMust be: '-u', '-r' or '-rb'");
                    break;
            }
        }
    }
}