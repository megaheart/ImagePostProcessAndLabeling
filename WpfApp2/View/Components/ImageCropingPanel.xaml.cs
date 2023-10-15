using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp2.Models;

namespace WpfApp2.View.Components
{
    /// <summary>
    /// Interaction logic for ImageCropingPanel.xaml
    /// </summary>
    public partial class ImageCropingPanel : UserControl
    {
        public ImageCropingPanel()
        {
            InitializeComponent();

            rectController = new RectController(ImgCropingPanel);
        }

        public static readonly RoutedEvent CropingRectangleChangedEvent = EventManager.RegisterRoutedEvent(
                       "CropingRectangleChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ImageCropingPanel));

        public event RoutedEventHandler CropingRectangleChanged
        {
            add { AddHandler(CropingRectangleChangedEvent, value); }
            remove { RemoveHandler(CropingRectangleChangedEvent, value); }
        }

        private double imageScale = 1.0; // raw img size / canvas size
        private int rawImageWidth = 0;
        private int rawImageHeight = 0;

        public ImageCropRect ImageCropingRectangle
        {
            get
            {
                if (!rectController.IsVisible)
                {
                    return new ImageCropRect() { X = 0, Y = 0, Width = rawImageWidth, Height = rawImageHeight };
                }

                var x = (int)Math.Round((rectController.X - rectController.MinX) * imageScale);
                var y = (int)Math.Round((rectController.Y - rectController.MinY) * imageScale);
                var width = (int)Math.Round(rectController.Width * imageScale);
                var height = (int)Math.Round(rectController.Height * imageScale);

                x = Math.Min(Math.Max(0, x), rawImageWidth);
                y = Math.Min(Math.Max(0, y), rawImageHeight);
                width = Math.Min(Math.Max(0, width), rawImageWidth - x);
                height = Math.Min(Math.Max(0, height), rawImageHeight - y);

                return new ImageCropRect() { X = x, Y = y, Width = width, Height = height };
            }
        }

        public void SetRect(double x, double y, double width, double height)
        {
            if (width < 0 || height < 0 || x < 0 || y < 0)
            {
                throw new Exception("Invalid rect");
            }
            if (width == 0 || height == 0)
            {
                rectController.IsVisible = false;
                return;
            }
            if (width == rawImageWidth && height == rawImageHeight)
            {
                rectController.IsVisible = false;
                return;
            }

            // Convert raw image size to canvas size
            var canvasX = x / imageScale + rectController.MinX;
            var canvasY = y / imageScale + rectController.MinY;
            var canvasWidth = width / imageScale;
            var canvasHeight = height / imageScale;
            rectController.IsVisible = true;

            rectController.SetPositionAndSize(canvasX, canvasY, canvasWidth, canvasHeight);
        }

        public void SetImage(BitmapImage image)
        {
            // Set image to canvas background with uniform stretch
            ImgCropingPanel.Background = new ImageBrush(image)
            {
                Stretch = Stretch.Uniform
            };
            rawImageWidth = image.PixelWidth;
            rawImageHeight = image.PixelHeight;

            // Limit canvas size to image size
            Size canvasSize = new Size(ImgCropingPanel.ActualWidth, ImgCropingPanel.ActualHeight);
            Size imageSize = new Size(image.PixelWidth, image.PixelHeight);
            double heightIfFitWithWidth = canvasSize.Width * imageSize.Height / imageSize.Width;

            if (heightIfFitWithWidth <= canvasSize.Height)
            {
                rectController.MinX = 0;
                rectController.MinY = (canvasSize.Height - heightIfFitWithWidth) / 2;
                rectController.MaxX = canvasSize.Width;
                rectController.MaxY = rectController.MinY + heightIfFitWithWidth;
                imageScale = imageSize.Width / canvasSize.Width;
            }
            else
            {
                double widthIfFitWithHeight = canvasSize.Height * imageSize.Width / imageSize.Height;
                rectController.MinX = (canvasSize.Width - widthIfFitWithHeight) / 2;
                rectController.MinY = 0;
                rectController.MaxX = rectController.MinX + widthIfFitWithHeight;
                rectController.MaxY = canvasSize.Height;
                imageScale = imageSize.Height / canvasSize.Height;
            }

            //rectController.SetPositionAndSize(rectController.MinX + 100, rectController.MinY + 100, rectController.MaxX - rectController.MinX - 200, rectController.MaxY - rectController.MinY - 200);
            //rectController.IsVisible = true;
        }

        public void ClearImage()
        {
            ImgCropingPanel.Background = null;
            rectController.MinX = 0;
            rectController.MinY = 0;
            rectController.MaxX = 0;
            rectController.MaxY = 0;
            imageScale = 1.0;
            rawImageWidth = 0;
            rawImageHeight = 0;
        }

        enum MouseDownState
        {
            CreateRectangle,
            Move,
            ResizeRectangleTopLeft,
            ResizeRectangleTopRight,
            ResizeRectangleBottomLeft,
            ResizeRectangleBottomRight,
            ResizeRectangleTop,
            ResizeRectangleBottom,
            ResizeRectangleLeft,
            ResizeRectangleRight,
            None
        }

        MouseDownState mouseDownState = MouseDownState.None;

        RectController rectController;

        private void CreateRectangle(System.Windows.Point point)
        {
            if (rectController.IsVisible)
            {
                rectController.ResizeBottomRightCornerToPos(point.X, point.Y);
            }
            else
            {
                rectController.IsVisible = true;
                rectController.SetPositionAndSize(point.X, point.Y, 0, 0);
                rectController.CaptureRectangle(point.X, point.Y);
            }
        }

        private bool IsMouseMoveInRect(Point mousePos, Point rectPos, Size rectSize)
        {
            return mousePos.X >= rectPos.X && mousePos.X <= rectPos.X + rectSize.Width
                && mousePos.Y >= rectPos.Y && mousePos.Y <= rectPos.Y + rectSize.Height;
        }

        private int MousePosToIndex(double pos, double rectPos, double rectSize, double margin)
        {
            if (pos < rectPos - margin)
            {
                return -1;
            }
            else if (pos <= rectPos + margin)
            {
                return 0;
            }
            else if (pos < rectPos + rectSize - margin)
            {
                return 1;
            }
            else if (pos <= rectPos + rectSize + margin)
            {
                return 2;
            }

            return -1;
        }

        MouseDownState[,] mouseStateIndexes = new MouseDownState[3, 3]
        {
            { MouseDownState.ResizeRectangleTopLeft, MouseDownState.ResizeRectangleTop, MouseDownState.ResizeRectangleTopRight },
            { MouseDownState.ResizeRectangleLeft, MouseDownState.Move, MouseDownState.ResizeRectangleRight },
            { MouseDownState.ResizeRectangleBottomLeft, MouseDownState.ResizeRectangleBottom, MouseDownState.ResizeRectangleBottomRight}
        };

        Cursor MouseDownStateToCursorType(MouseDownState state)
        {
            switch (state)
            {
                case MouseDownState.None:
                    return Cursors.Arrow;
                case MouseDownState.CreateRectangle:
                    return Cursors.Arrow;
                case MouseDownState.ResizeRectangleTopLeft:
                case MouseDownState.ResizeRectangleBottomRight:
                    return Cursors.SizeNWSE;
                case MouseDownState.ResizeRectangleTopRight:
                case MouseDownState.ResizeRectangleBottomLeft:
                    return Cursors.SizeNESW;
                case MouseDownState.ResizeRectangleTop:
                case MouseDownState.ResizeRectangleBottom:
                    return Cursors.SizeNS;
                case MouseDownState.ResizeRectangleLeft:
                case MouseDownState.ResizeRectangleRight:
                    return Cursors.SizeWE;
                case MouseDownState.Move:
                    return Cursors.SizeAll;
                default:
                    return Cursors.Arrow;

            }
        }

        private MouseDownState GetMouseDownState(Point mousePos)
        {
            if (rectController.IsVisible)
            {
                const int marginSize = 10;
                var indexX = MousePosToIndex(mousePos.X, rectController.X, rectController.Width, marginSize);
                var indexY = MousePosToIndex(mousePos.Y, rectController.Y, rectController.Height, marginSize);

                if (indexX == -1 || indexY == -1)
                {
                    Cursor = Cursors.Arrow;
                    return MouseDownState.None;
                }
                else
                {
                    var mouseState = mouseStateIndexes[indexY, indexX];
                    Cursor = MouseDownStateToCursorType(mouseState);
                    return mouseState;
                }
            }

            return MouseDownState.None;
        }

        private void ImageCropingPanel_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(ImgCropingPanel);

            if (e.LeftButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
            {
                GetMouseDownState(mousePos);
                return;
            }

            switch (mouseDownState)
            {
                case MouseDownState.None:
                    break;
                case MouseDownState.CreateRectangle:
                    CreateRectangle(mousePos);
                    break;
                case MouseDownState.ResizeRectangleTopLeft:
                    rectController.ResizeTopLeftCornerToPos(mousePos.X, mousePos.Y);
                    break;
                case MouseDownState.ResizeRectangleTopRight:
                    rectController.ResizeTopRightCornerToPos(mousePos.X, mousePos.Y);
                    break;
                case MouseDownState.ResizeRectangleBottomLeft:
                    rectController.ResizeBottomLeftCornerToPos(mousePos.X, mousePos.Y);
                    break;
                case MouseDownState.ResizeRectangleBottomRight:
                    rectController.ResizeBottomRightCornerToPos(mousePos.X, mousePos.Y);
                    break;
                case MouseDownState.ResizeRectangleTop:
                    rectController.ResizeTopSideToPos(mousePos.Y);
                    break;
                case MouseDownState.ResizeRectangleBottom:
                    rectController.ResizeBottomSideToPos(mousePos.Y);
                    break;
                case MouseDownState.ResizeRectangleLeft:
                    rectController.ResizeLeftSideToPos(mousePos.X);
                    break;
                case MouseDownState.ResizeRectangleRight:
                    rectController.ResizeRightSideToPos(mousePos.X);
                    break;
                case MouseDownState.Move:
                    rectController.MoveToPos(mousePos.X, mousePos.Y);
                    break;
                default:
                    break;

            }
        }

        private void ImageCropingPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(ImgCropingPanel);
            if (rectController.IsVisible)
            {
                mouseDownState = GetMouseDownState(mousePos);
                rectController.CaptureRectangle(mousePos.X, mousePos.Y);
            }
            else
            {
                if (mousePos.X < rectController.MinX || mousePos.Y < rectController.MinY ||
                    mousePos.X > rectController.MaxX || mousePos.Y > rectController.MaxY)
                {
                    return;
                }
                mouseDownState = MouseDownState.CreateRectangle;
                Cursor = Cursors.Hand;
            }

        }

        public void ClearCropingRectangles()
        {
            rectController.IsVisible = false;
        }

        private void ImgCropingPanel_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(mouseDownState != MouseDownState.None)
            {
                RaiseEvent(new RoutedEventArgs(CropingRectangleChangedEvent));
            }

            GetMouseDownState(e.GetPosition(ImgCropingPanel));
            mouseDownState = MouseDownState.None;
        }
    }
}
