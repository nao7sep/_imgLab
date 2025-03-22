using System.Drawing;
using System.Drawing.Imaging;

namespace _imgLab
{
    public static class Utility
    {
        public static string GenerateOutputDirectoryPath (string parentDirectoryPath)
        {
            while (true)
            {
                string xOutputDirectoryPath = Path.Join (parentDirectoryPath, $"_imgLab-{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}");

                if (Directory.Exists (xOutputDirectoryPath) == false)
                    return xOutputDirectoryPath;

                // Making sure it wont skip 2 seconds.
                Thread.Sleep (333);
            }
        }

        public static string GenerateOutputDirectoryPath () =>
            GenerateOutputDirectoryPath (Environment.GetFolderPath (Environment.SpecialFolder.DesktopDirectory));

        public static int [] QualityLevels { get; } = [ 75, 80, 85, 90, 95, 96, 97, 98, 99, 100 ];

        public static ImageCodecInfo? GetEncoder (ImageFormat format)
        {
            foreach (ImageCodecInfo xCodec in ImageCodecInfo.GetImageDecoders ())
            {
                if (xCodec.FormatID == format.Guid)
                    return xCodec;
            }

            return null;
        }

        public static IList <(int QualityLevel, string ImagePath, long FileLength)> CompareJpegQualityLevelsAndFileLengths (
            string inputImagePath, string outputDirectoryPath, IEnumerable <int> qualityLevels)
        {
            List <(int QualityLevel, string ImagePath, long FileLength)> xResults = [];

            using Image xOriginalImage = Image.FromFile (inputImagePath);
            using Bitmap xBitmap = new (xOriginalImage);

            ImageCodecInfo? xJpegCodec = GetEncoder (ImageFormat.Jpeg) ?? throw new NullReferenceException ("JPEG codec not found.");

            foreach (int xQualityLevel in qualityLevels)
            {
                EncoderParameters xEncoderParameters = new (count: 1);
                xEncoderParameters.Param [0] = new (Encoder.Quality, xQualityLevel);

                Directory.CreateDirectory (outputDirectoryPath);
                string xOutputImagePath = Path.Join (outputDirectoryPath, $"{Path.GetFileNameWithoutExtension (inputImagePath)}-{xQualityLevel}.jpg");
                xBitmap.Save (xOutputImagePath, xJpegCodec, xEncoderParameters);

                long xFileLength = new FileInfo (xOutputImagePath).Length;
                xResults.Add ((xQualityLevel, xOutputImagePath, xFileLength));
            }

            return xResults;
        }

        public static IList <(int QualityLevel, string ImagePath, long FileLength)> CompareJpegQualityLevelsAndFileLengths (string inputImagePath) =>
            CompareJpegQualityLevelsAndFileLengths (inputImagePath, GenerateOutputDirectoryPath (), QualityLevels);

        public static string FileLengthToFriendlyString (long fileLength)
        {
            if (fileLength < 1024)
                return $"{fileLength} bytes";

            long xKilobytes = fileLength / 1024;
            return $"{xKilobytes:N0} KB";
        }

        public static void PrintJpegQualityLevelAndFileLengthComparisonResults (
            string inputImagePath, IEnumerable <(int QualityLevel, string ImagePath, long FileLength)> results)
        {
            Console.WriteLine ($"Comparison results for {Path.GetFileName (inputImagePath)}:");

            long xOriginalFileLength = new FileInfo (inputImagePath).Length;
            string xOriginalFileLengthFriendlyString = FileLengthToFriendlyString (xOriginalFileLength);

            long xLongestFileLength = results.Max (x => x.FileLength);
            string xLongestFileLengthFriendlyString = FileLengthToFriendlyString (xLongestFileLength);

            int xMaxFriendlyStringLength = Math.Max (xOriginalFileLengthFriendlyString.Length, xLongestFileLengthFriendlyString.Length);

            Console.WriteLine ($"    Original => {FileLengthToFriendlyString (xOriginalFileLength).PadLeft (xMaxFriendlyStringLength)}");

            foreach (var xResult in results)
            {
                string xQualityLevelString = xResult.QualityLevel.ToString ().PadLeft ("Original".Length),
                       xFileLengthFriendlyString = FileLengthToFriendlyString (xResult.FileLength).PadLeft (xMaxFriendlyStringLength),
                       xPercentageString = Math.Round (100.0 * xResult.FileLength / xOriginalFileLength).ToString ();

                Console.WriteLine ($"    {xQualityLevelString} => {xFileLengthFriendlyString} ({xPercentageString}%)");
            }
        }
    }
}
