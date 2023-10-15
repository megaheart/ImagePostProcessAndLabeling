using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WpfApp2.View.Components;

namespace WpfApp2.ServiceThreadUtil.Helpers
{
    public static class ImageHelper
    {
        public static bool ProcessImage(string path, string outputPath, int offsetX, int offsetY, int width, int height)
        {
            Image originalImage;

            try
            {
                originalImage = Image.FromFile(path);
            }
            catch(Exception _)
            {
                return false;
            }

            // Crop the image
            Rectangle cropArea = new Rectangle(offsetX, offsetY, width, height);
            // Resize the image to approximately 20KB JPEG
            using (MemoryStream resizedImage = CropAndResizeImage(originalImage, cropArea, 20))
            {
                FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                resizedImage.WriteTo(fs);
                fs.Close();
            }
            originalImage.Dispose();
            return true;
        }

        public static Bitmap CropImage(Image originalImage, Rectangle cropArea)
        {
            Bitmap croppedImage = new Bitmap(cropArea.Width, cropArea.Height);
            using (Graphics g = Graphics.FromImage(croppedImage))
            {
                g.DrawImage(originalImage, new Rectangle(0, 0, croppedImage.Width, croppedImage.Height), cropArea, GraphicsUnit.Pixel);
            }
            return croppedImage;
        }

        public static MemoryStream ResizeImage(Image image, int targetSizeKB)
        {
            MemoryStream stream = new MemoryStream();

            const int START_AREA = 102_400;

            Size startSize = ScaleSize(image.Size, Math.Sqrt(START_AREA * 1.0 / image.Size.Width / image.Size.Height));

            int scale = 10;

            do
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.Position = 0;
                stream.SetLength(0);

                Size size = ScaleSize(startSize, 1.0 * scale / 10);

                Bitmap resizedImage = new Bitmap(size.Width, size.Height);
                using (Graphics graphics = Graphics.FromImage(resizedImage))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(image, 0, 0, size.Width, size.Height);
                }

                resizedImage.Save(stream, GetEncoderInfo("image/jpeg"), GetEncoderParameters(100));
                resizedImage.Dispose();
                scale--;
            }
            while (stream.Length / 1024 > targetSizeKB && scale > 0);

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            return stream;

        }

        public static MemoryStream CropAndResizeImage(Image image, Rectangle cropArea, int targetSizeKB)
        {
            MemoryStream stream = new MemoryStream();

            const int START_AREA = 102_400;

            Size startSize = ScaleSize(cropArea.Size, Math.Sqrt(START_AREA * 1.0 / image.Size.Width / image.Size.Height));

            int scale = 10;

            do
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.Position = 0;
                stream.SetLength(0);

                Size size = ScaleSize(startSize, 1.0 * scale / 10);

                Bitmap resizedImage = new Bitmap(size.Width, size.Height);
                using (Graphics graphics = Graphics.FromImage(resizedImage))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(image, new Rectangle(0, 0, size.Width, size.Height), cropArea, GraphicsUnit.Pixel);
                }

                resizedImage.Save(stream, GetEncoderInfo("image/jpeg"), GetEncoderParameters(100));
                resizedImage.Dispose();
                scale--;
            }
            while (stream.Length / 1024 > targetSizeKB && scale > 0);

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            return stream;

        }

        public static Size ScaleSize(Size maxSize, double scale)
        {
            int width = (int)Math.Round(maxSize.Width * scale);
            int height = (int)Math.Round(maxSize.Height * scale);
            return new Size(width, height);
        }

        public static Size FitSize(Size maxSize, Size currentSize)
        {
            int heightIfFitWithWidth = (int)Math.Round(1.0 * maxSize.Width * currentSize.Height / currentSize.Width);

            if (heightIfFitWithWidth <= maxSize.Height)
            {
                return new Size(maxSize.Width, heightIfFitWithWidth);
            }
            else
            {
                int widthIfFitWithHeight = (int)Math.Round(1.0 * maxSize.Height * currentSize.Width / currentSize.Height);

                return new Size(widthIfFitWithHeight, maxSize.Height);
            }
        }

        public static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
            return Array.Find(encoders, encoder => encoder.MimeType == mimeType);
        }

        static EncoderParameters GetEncoderParameters(int quality)
        {
            EncoderParameter parameter = new EncoderParameter(Encoder.Quality, quality);
            EncoderParameters parameters = new EncoderParameters(1);
            parameters.Param[0] = parameter;
            return parameters;
        }
    }
}
