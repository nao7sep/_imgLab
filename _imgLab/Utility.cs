using ImageMagick;
using ImageMagick.Drawing;

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
                xImage.SetProfile (xExifProfile); // Without this line, the thumbnail will not be removed.
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

        public static string GenerateSquareImageForInstagram (string inputImagePath, string outputDirectoryPath, uint widthAndHeight)
        {
            using MagickImage xImage = new (inputImagePath);

            xImage.AutoOrient ();

            if (xImage.HasProfile ("icc"))
            {
                xImage.TransformColorSpace (ColorProfile.SRGB);
                xImage.RemoveProfile ("icc");
            }

            else if (xImage.ColorSpace != ColorSpace.sRGB)
                xImage.TransformColorSpace (ColorProfile.AdobeRGB1998, ColorProfile.SRGB);

            var xExifProfile = xImage.GetExifProfile ();

            if (xExifProfile != null && xExifProfile.ThumbnailOffset != 0 && xExifProfile.ThumbnailLength != 0)
            {
                xExifProfile.RemoveThumbnail ();
                xImage.SetProfile (xExifProfile);
            }

            xImage.Strip ();

            using var xBackgroundImage = xImage.Clone ();

            // https://github.com/dlemstra/Magick.NET/blob/main/src/Magick.NET/Types/MagickGeometry.cs

            xBackgroundImage.Resize (new MagickGeometry (widthAndHeight, widthAndHeight)
            {
                // Gets or sets a value indicating whether the image is resized without preserving aspect ratio (!).
                IgnoreAspectRatio = false,

                // Gets or sets a value indicating whether the image is resized based on the smallest fitting dimension (^).
                FillArea = true,
            });

            // Crop image (subregion of original image). ResetPage should be called unless the Page information is needed.
            xBackgroundImage.Crop (widthAndHeight, widthAndHeight, Gravity.Center);

            // Resets the page property of this image.
            // => Page = new MagickGeometry(0, 0, 0, 0);
            xBackgroundImage.ResetPage ();

            // radius: The radius of the Gaussian in pixels, not counting the center pixel.
            // sigma: The standard deviation of the Laplacian, in pixels.
            xBackgroundImage.Blur (radius: 0, sigma: 10);

            using var xForegroundImage = xImage.Clone ();

            xForegroundImage.Resize (new MagickGeometry (widthAndHeight, widthAndHeight)
            {
                IgnoreAspectRatio = false
            });

            uint xOffsetX = (xBackgroundImage.Width - xForegroundImage.Width) / 2,
                 xOffsetY = (xBackgroundImage.Height - xForegroundImage.Height) / 2;

            // Compose an image onto another at specified offset using the specified algorithm.
            // Over: The result is the union of the two image shapes with the composite image obscuring image in the region of overlap.
            // https://www.imagemagick.org/Magick++/Enumerations.html
            xBackgroundImage.Composite (xForegroundImage, (int) xOffsetX, (int) xOffsetY, CompositeOperator.Over);

            var xDrawables = new Drawables ().
                // https://fonts.google.com/specimen/Merriweather
                Font ("Merriweather").
                FontPointSize (25).
                FillColor (MagickColors.White).
                FillOpacity (new Percentage (25)).
                TextAlignment (TextAlignment.Right).
                // https://www.instagram.com/nao7sep/
                Text (xBackgroundImage.Width - 50, xBackgroundImage.Height - 50, "@nao7sep");

            xDrawables.Draw (xBackgroundImage);

            string xOutputImagePath = Path.Join (outputDirectoryPath, $"{Path.GetFileNameWithoutExtension(inputImagePath)}-Square.jpg");

            Directory.CreateDirectory (outputDirectoryPath);
            xBackgroundImage.Quality = 85; // Between 75 and 95.
            xBackgroundImage.Write (xOutputImagePath, MagickFormat.Jpeg);

            return xOutputImagePath;
        }

        public static string GenerateWatermarkedImageForInstagram (string inputImagePath, string outputDirectoryPath, bool generateWatermarkedPartialImage)
        {
            var xInitialThreadPriority = Thread.CurrentThread.Priority;

            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                using MagickImage xImage = new (inputImagePath);

                xImage.AutoOrient ();

                if (xImage.HasProfile ("icc"))
                {
                    xImage.TransformColorSpace (ColorProfile.SRGB);
                    xImage.RemoveProfile ("icc");
                }

                else if (xImage.ColorSpace != ColorSpace.sRGB)
                    xImage.TransformColorSpace (ColorProfile.AdobeRGB1998, ColorProfile.SRGB);

                var xExifProfile = xImage.GetExifProfile ();

                if (xExifProfile != null && xExifProfile.ThumbnailOffset != 0 && xExifProfile.ThumbnailLength != 0)
                {
                    xExifProfile.RemoveThumbnail ();
                    xImage.SetProfile (xExifProfile);
                }

                xImage.Strip ();

                // -----------------------------------------------------------------------------
                // Instagram-ish Optimization
                // -----------------------------------------------------------------------------

                xImage.LinearStretch (blackPoint: new Percentage (0), whitePoint: new Percentage (0.05));
                xImage.Modulate (brightness: new Percentage (100), saturation: new Percentage (115), hue: new Percentage (100));
                xImage.AdaptiveSharpen (radius: 0, sigma: 1);

                // -----------------------------------------------------------------------------
                // Metrics
                // -----------------------------------------------------------------------------

                double xFontPointSize = 25.0 * Math.Max (xImage.Width, xImage.Height) / 1080;

                using var xDummyImage = new MagickImage (MagickColors.Transparent, width: 1, height: 1);
                xDummyImage.Settings.Font = "Merriweather";
                xDummyImage.Settings.FontPointsize = xFontPointSize; // Not a typo.

                var xMetrics = xDummyImage.FontTypeMetrics ("@nao7sep") ?? throw new NullReferenceException ("Metrics are null.");

                // -----------------------------------------------------------------------------
                // Average Luminance
                // -----------------------------------------------------------------------------

                double xOffsetX = (double) xImage.Width * 1 / 4,
                    xOffsetY = (double) xImage.Height * 4 / 5;

                MagickGeometry xGeometry = new (
                    (int) Math.Round (xOffsetX - xMetrics.TextWidth / 2),
                    (int) Math.Round (xOffsetY - xMetrics.Ascent),
                    (uint) Math.Round (xMetrics.TextWidth),
                    (uint) Math.Round (xMetrics.TextHeight));

                using var xPartialImage = xImage.CloneArea (xGeometry);
                using var xPixels = xPartialImage.GetPixels ();

                double xAverageLuminance = Enumerable.Range (0, (int) xPartialImage.Width).
                    SelectMany (x => Enumerable.Range (0, (int) xPartialImage.Height).
                    Select (y => xPixels.GetPixel (x, y))).
                    Select (pixel =>
                {
                    double xRed = (double) pixel.GetChannel ((int) PixelChannel.Red) / Quantum.Max,
                        xGreen = (double) pixel.GetChannel ((int) PixelChannel.Green) / Quantum.Max,
                        xBlue = (double) pixel.GetChannel ((int) PixelChannel.Blue) / Quantum.Max,
                        xLuminance = 0.2126 * xRed + 0.7152 * xGreen + 0.0722 * xBlue;

                    return xLuminance;
                }).
                Average ();

                // -----------------------------------------------------------------------------
                // Watermark
                // -----------------------------------------------------------------------------

                var xDrawables = new Drawables ().
                    // https://fonts.google.com/specimen/Merriweather
                    Font ("Merriweather").
                    FontPointSize (xFontPointSize).
                    FillColor (xAverageLuminance >= 0.5 ? MagickColors.Black : MagickColors.White).
                    FillOpacity (new Percentage (50)). // 25% is a little too faint.
                    TextAlignment (TextAlignment.Center).
                    // https://www.instagram.com/nao7sep/
                    Text (xOffsetX, xOffsetY, "@nao7sep");

                xDrawables.Draw (xImage);

                string xOutputImagePath = Path.Join (outputDirectoryPath, $"{Path.GetFileNameWithoutExtension(inputImagePath)}-Watermarked.jpg");

                Directory.CreateDirectory (outputDirectoryPath);
                xImage.Quality = 85; // Between 75 and 95.
                xImage.Write (xOutputImagePath, MagickFormat.Jpeg);

                if (generateWatermarkedPartialImage)
                {
                    using var xWatermarkedPartialImage = xImage.CloneArea (xGeometry);
                    xWatermarkedPartialImage.Quality = 75; // Standard quality.
                    xWatermarkedPartialImage.Write (Path.Join (outputDirectoryPath, $"{Path.GetFileNameWithoutExtension(inputImagePath)}-Partial.jpg"), MagickFormat.Jpeg);
                }

                return xOutputImagePath;
            }

            finally
            {
                Thread.CurrentThread.Priority = xInitialThreadPriority;
            }
        }
    }
}
