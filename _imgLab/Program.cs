namespace _imgLab
{
    class Program
    {
        static void Main (string [] args)
        {
            try
            {
                string xOutputDirectoryPath = Utility.GenerateOutputDirectoryPath ();

                foreach (string xInputImagePath in args)
                {
                    var xResults = Utility.CompareJpegQualityLevelsAndFileLengths (xInputImagePath, xOutputDirectoryPath, Utility.QualityLevels);
                    Utility.PrintJpegQualityLevelAndFileLengthComparisonResults (xInputImagePath, xResults);

                    string xSquareImagePath = Utility.GenerateSquareImageForInstagram (xInputImagePath, xOutputDirectoryPath, 1080);
                    Console.WriteLine ($"Square image for Instagram created: {Path.GetFileName (xSquareImagePath)}");

                    string xWatermarkedImagePath = Utility.GenerateWatermarkedImageForInstagram (xInputImagePath, xOutputDirectoryPath, true);
                    Console.WriteLine ($"Watermarked image for Instagram created: {Path.GetFileName (xWatermarkedImagePath)}");
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
