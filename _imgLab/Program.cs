﻿namespace _imgLab
{
    class Program
    {
        static void Main (string [] args)
        {
            try
            {
                string xOutputDirectoryPath = Utility.GenerateOutputDirectoryPath ();
                xOutputDirectoryPath = Environment.GetFolderPath (Environment.SpecialFolder.DesktopDirectory); // More convenient.

                foreach (string xInputImagePath in args.Order (StringComparer.OrdinalIgnoreCase))
                {
                    // var xResults = Utility.CompareJpegQualityLevelsAndFileLengths (xInputImagePath, xOutputDirectoryPath, Utility.QualityLevels);
                    // Utility.PrintJpegQualityLevelAndFileLengthComparisonResults (xInputImagePath, xResults);

                    // string xSquareImagePath = Utility.GenerateSquareImageForInstagram (xInputImagePath, xOutputDirectoryPath, 1080);
                    // Console.WriteLine ($"Square image for Instagram created: {Path.GetFileName (xSquareImagePath)}");

                    var xWatermarkedImageResult = Utility.GenerateWatermarkedImageForInstagram (xInputImagePath, xOutputDirectoryPath, false);
                    Console.WriteLine ($"Watermarked image for Instagram created: {Path.GetFileName (xWatermarkedImageResult.WatermarkedImagePath)}");

                    if (xWatermarkedImageResult.WatermarkedPartialImagePath != null)
                        Console.WriteLine ($"Watermarked partial image created: {Path.GetFileName (xWatermarkedImageResult.WatermarkedPartialImagePath)}");
                }
            }

            catch (Exception xException)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine (xException.ToString ());
                Console.ResetColor ();
            }

            finally
            {
                Console.Write ("Press any key to exit: ");
                Console.ReadKey (intercept: true);
                Console.WriteLine ();
            }
        }
    }
}
