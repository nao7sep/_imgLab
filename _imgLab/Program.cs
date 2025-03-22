namespace _imgLab
{
    class Program
    {
        static void Main (string [] args)
        {
            try
            {
                foreach (string xInputImagePath in args)
                {
                    var xResults = Utility.CompareJpegQualityLevelsAndFileLengths (xInputImagePath);
                    Utility.PrintJpegQualityLevelAndFileLengthComparisonResults (xInputImagePath, xResults);
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
