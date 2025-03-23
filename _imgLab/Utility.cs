using ImageMagick;

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

        public static IList <(int QualityLevel, string ImagePath, long FileLength, string FileLengthFriendlyString)>
            CompareJpegQualityLevelsAndFileLengths (string inputImagePath, string outputDirectoryPath, IEnumerable <int> qualityLevels)
        {
            List <(int QualityLevel, string ImagePath, long FileLength, string FileLengthFriendlyString)> xResults = [];

            // Optimizing Images for the Web: A Comprehensive Guide
            // https://github.com/nao7sep/Resources/blob/main/Documents/AI-Generated%20Notes/Optimizing%20Images%20for%20the%20Web%20-%20A%20Comprehensive%20Guide.md

            using MagickImage xImage = new (inputImagePath);

            xImage.AutoOrient ();

            // If an ICC profile is present, it's safe enough to assume that the image's color space property is consistent with the ICC profile.
            // We'll let Magick.NET identify the color space based on the ICC profile and then transform it to sRGB.
            // We'll remove the ICC profile because we may not always "Strip" the image.

            if (xImage.HasProfile ("icc"))
            {
                xImage.TransformColorSpace (ColorProfile.SRGB);
                xImage.RemoveProfile ("icc");
#if DEBUG
                Console.WriteLine ("ICC profile detected.");
                Console.WriteLine ("Color space transformed to sRGB.");
                Console.WriteLine ("ICC profile removed.");
#endif
            }

            // If an ICC profile is not present and the color space property is not explicitly set to sRGB, Magick.NET may not be able to identify the color space correctly.
            // Adobe RGB (1998) is a common color space for professional cameras, which makes it reasonable to fall back to it as the source color space.
            // If Magick.NET somehow identifies the color space correctly, this parameter will be ignored.

            else if (xImage.ColorSpace != ColorSpace.sRGB)
            {
                xImage.TransformColorSpace (ColorProfile.AdobeRGB1998, ColorProfile.SRGB);
#if DEBUG
                Console.WriteLine ("Color space not explicitly set to sRGB.");
                Console.WriteLine ("Color space transformed to sRGB.");
#endif
            }

            var xExifProfile = xImage.GetExifProfile ();

            // Based on the CreateThumbnail method in the IExifProfileExtensions class and the RemoveThumbnail method in the ExifProfile class,
            // we should be able to determine whether a thumbnail is present in the Exif profile depending on the ThumbnailOffset and ThumbnailLength properties.
            // https://github.com/dlemstra/Magick.NET/blob/main/src/Magick.NET/net8.0/Extensions/IExifProfileExtensions.cs
            // https://github.com/dlemstra/Magick.NET/blob/main/src/Magick.NET.Core/Profiles/Exif/ExifProfile.cs

            if (xExifProfile != null && xExifProfile.ThumbnailOffset != 0 && xExifProfile.ThumbnailLength != 0)
            {
                xExifProfile.RemoveThumbnail ();
                xImage.SetProfile (xExifProfile);
#if DEBUG
                Console.WriteLine ("Thumbnail detected.");
                Console.WriteLine ("Thumbnail removed.");
#endif
            }

            xImage.Strip ();

            Directory.CreateDirectory (outputDirectoryPath);

            foreach (int xQualityLevel in qualityLevels)
            {
                string xOutputImagePath = Path.Join (outputDirectoryPath, $"{Path.GetFileNameWithoutExtension(inputImagePath)}-{xQualityLevel}.jpg");

                xImage.Quality = (uint) xQualityLevel;
                xImage.Write (xOutputImagePath, MagickFormat.Jpeg);

                long xFileLength = new FileInfo(xOutputImagePath).Length;
                string xFileLengthFriendlyString = FileLengthToFriendlyString (xFileLength);
                xResults.Add ((xQualityLevel, xOutputImagePath, xFileLength, xFileLengthFriendlyString));
            }

            return xResults;
        }

        public static IList <(int QualityLevel, string ImagePath, long FileLength, string FileLengthFriendlyString)>
            CompareJpegQualityLevelsAndFileLengths (string inputImagePath) =>
                CompareJpegQualityLevelsAndFileLengths (inputImagePath, GenerateOutputDirectoryPath (), QualityLevels);

        public static string FileLengthToFriendlyString (long fileLength)
        {
            if (fileLength < 1024)
                return $"{fileLength} bytes";

            long xKilobytes = fileLength / 1024;
            return $"{xKilobytes:N0} KB";
        }

        public static void PrintJpegQualityLevelAndFileLengthComparisonResults (
            string inputImagePath, IEnumerable <(int QualityLevel, string ImagePath, long FileLength, string FileLengthFriendlyString)> results)
        {
            Console.WriteLine ($"Comparison results for {Path.GetFileName (inputImagePath)}:");

            long xOriginalFileLength = new FileInfo (inputImagePath).Length;
            string xOriginalFileLengthFriendlyString = FileLengthToFriendlyString (xOriginalFileLength);
            int xMaxFriendlyStringLength = Math.Max (xOriginalFileLengthFriendlyString.Length, results.Max (x => x.FileLengthFriendlyString.Length));

            Console.WriteLine ($"    Original => {FileLengthToFriendlyString (xOriginalFileLength).PadLeft (xMaxFriendlyStringLength)}");

            foreach (var xResult in results)
            {
                string xQualityLevelPart = xResult.QualityLevel.ToString ().PadLeft ("Original".Length),
                       xFileLengthFriendlyStringPart = xResult.FileLengthFriendlyString.PadLeft (xMaxFriendlyStringLength),
                       xPercentagePart = Math.Round (100.0 * xResult.FileLength / xOriginalFileLength).ToString () + '%'; // Not padded.

                Console.WriteLine ($"    {xQualityLevelPart} => {xFileLengthFriendlyStringPart} ({xPercentagePart})");
            }
        }
    }
}
