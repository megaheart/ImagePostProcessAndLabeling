using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace WpfApp2.View.Components
{
    public class RectController
    {
        private Panel panel;
        public RectController(Panel parent)
        {
            // Other Rectangles (Unselected Zones)
            for(var i = 0; i < 3; i++)
            {
                for(var j = 0; j < 3; j++)
                {
                    if(i == 1 && j == 1)
                    {
                        continue;
                    }

                    var rect = rectangles[i, j];
                    rect.Visibility = Visibility.Hidden;
                    Panel.SetZIndex(rect, 100);
                    parent.Children.Add(rect);
                }
            }


            // Main Rectangle
            Rectangle = rectangles[1 , 1];
            Rectangle.Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 0)) { Opacity = 0.75 };
            Rectangle.StrokeThickness = 2;
            Rectangle.Width = 0;
            Rectangle.Height = 0;
            Rectangle.Margin = new Thickness(0, 0, 0, 0);
            Rectangle.Visibility = Visibility.Hidden;
            Panel.SetZIndex(Rectangle, 100);
            parent.Children.Add(Rectangle);

            // Other settings
            panel = parent;
            MinX = 0;
            MinY = 0;
            MaxX = 0;
            MaxY = 0;
            //parent.Dispatcher.InvokeAsync(() =>
            //{
            //    MaxX = parent.ActualWidth;
            //    MaxY = parent.ActualHeight;
            //}, System.Windows.Threading.DispatcherPriority.Loaded);

            //MinX = 100;
            //MinY = 100;
            //parent.Dispatcher.InvokeAsync(() =>
            //{
            //    MaxX = parent.ActualWidth - 100;
            //    MaxY = parent.ActualHeight - 100;
            //}, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static readonly Brush StockBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0)) { Opacity = 0.75 };
        private static readonly Brush UnselectedZoneBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0)) { Opacity = 0.5 };

        private Rectangle[,] rectangles = new Rectangle[3, 3] {
            {
                new Rectangle(){ Fill = UnselectedZoneBrush },
                new Rectangle(){ Fill = UnselectedZoneBrush },
                new Rectangle(){ Fill = UnselectedZoneBrush },
            },
            {
                new Rectangle(){ Fill = UnselectedZoneBrush },
                new Rectangle(),
                new Rectangle(){ Fill = UnselectedZoneBrush },
            },
            {
                new Rectangle(){ Fill = UnselectedZoneBrush },
                new Rectangle(){ Fill = UnselectedZoneBrush },
                new Rectangle(){ Fill = UnselectedZoneBrush },
            },
        };

        public bool IsVisible
        {
            get => Rectangle.Visibility == Visibility.Visible;
            set {
                var visibility = value ? Visibility.Visible : Visibility.Hidden;

                if(visibility == Rectangle.Visibility)
                {
                    return;
                }

                for (var i = 0; i < 3; i++)
                {
                    for (var j = 0; j < 3; j++)
                    {
                        var rect = rectangles[i, j];
                        rect.Visibility = visibility;
                    }
                }
            }
        }

        public Rectangle Rectangle { get; private set; }

        public double X { 
            get => Rectangle.Margin.Left;
            //set => Rectangle.Margin = new Thickness(value, Y, 0, 0);
        }
        public double Y
        {
            get => Rectangle.Margin.Top;
            //set => Rectangle.Margin = new Thickness(X, value, 0, 0);
        }
        public double Width { 
            get => Rectangle.Width;
            //set => Rectangle.Width = value;
        }
        public double Height { 
            get => Rectangle.Height;
            //set => Rectangle.Height = value;
        }

        public double MinX { set; get; }
        public double MinY { set; get; }
        public double MaxX { set; get; }
        public double MaxY { set; get; }

        public void SetPositionAndSize(double x, double y, double width, double height)
        {
            //Rectangle.Margin = new Thickness(x, y, 0, 0);
            //Rectangle.Width = width;
            //Rectangle.Height = height;

            Span<double> axisX = stackalloc double[4] { MinX, x, x + width, MaxX };
            Span<double> axisY = stackalloc double[4] { MinY, y, y + height, MaxY };
            Span<double> sizeX = stackalloc double[3] { axisX[1] - axisX[0], axisX[2] - axisX[1], axisX[3] - axisX[2] };
            Span<double> sizeY = stackalloc double[3] { axisY[1] - axisY[0], axisY[2] - axisY[1], axisY[3] - axisY[2] };

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var rect = rectangles[i, j];
                    rect.Margin = new Thickness(axisX[j], axisY[i], 0, 0);
                    rect.Width = sizeX[j];
                    rect.Height = sizeY[i];
                }
            }

            //Tuple<Point, Size>[,] sizes = new Tuple<Point, Size>[3,3];
            //for (var i = 0; i < 3; i++)
            //{
            //    for (var j = 0; j < 3; j++)
            //    {
            //        var rect = rectangles[i, j];
            //        sizes[i, j] = new Tuple<Point, Size>(new Point(rect.Margin.Left, rect.Margin.Top), new Size(rect.Width, rect.Height));
            //    }
            //}

        }

        Point CapturedTopLeftPosition;
        Point CapturedTopRightPosition;
        Point CapturedBottomLeftPosition;
        Point CapturedBottomRightPosition;
        Size CapturedSize;
        Point CapturedMousePosition;

        public void CaptureRectangle(double x, double y)
        {
            CapturedMousePosition = new Point(x, y);
            CapturedTopLeftPosition = new Point(X, Y);
            CapturedTopRightPosition = new Point(X + Width, Y);
            CapturedBottomLeftPosition = new Point(X, Y + Height);
            CapturedBottomRightPosition = new Point(X + Width, Y + Height);
            CapturedSize = new Size(Width, Height);
        }

        public void ResizeTopLeftCornerToPos(double x, double y)
        {
            double oldXBottomRight = CapturedBottomRightPosition.X;
            double oldYBottomRight = CapturedBottomRightPosition.Y;

            x = Math.Min(Math.Max(x, MinX), MaxX);
            y = Math.Min(Math.Max(y, MinY), MaxY);

            double newX = Math.Min(x, oldXBottomRight);
            double newY = Math.Min(y, oldYBottomRight);

            double newWidth = Math.Abs(oldXBottomRight - x);
            double newHeight = Math.Abs(oldYBottomRight - y);

            SetPositionAndSize(newX, newY, newWidth, newHeight);
        }

        public void ResizeBottomRightCornerToPos(double x, double y)
        {
            double oldXTopLeft = CapturedTopLeftPosition.X;
            double oldYTopLeft = CapturedTopLeftPosition.Y;

            x = Math.Min(Math.Max(x, MinX), MaxX);
            y = Math.Min(Math.Max(y, MinY), MaxY);

            double newX = Math.Min(x, oldXTopLeft);
            double newY = Math.Min(y, oldYTopLeft);

            double newWidth = Math.Abs(x - oldXTopLeft);
            double newHeight = Math.Abs(y - oldYTopLeft);

            SetPositionAndSize(newX, newY, newWidth, newHeight);
        }

        public void ResizeTopRightCornerToPos(double x, double y)
        {
            double oldXBottomLeft = CapturedBottomLeftPosition.X;
            double oldYBottomLeft = CapturedBottomLeftPosition.Y;

            x = Math.Min(Math.Max(x, MinX), MaxX);
            y = Math.Min(Math.Max(y, MinY), MaxY);

            double newX = Math.Min(x, oldXBottomLeft);
            double newY = Math.Min(y, oldYBottomLeft);

            double newWidth = Math.Abs(oldXBottomLeft - x);
            double newHeight = Math.Abs(oldYBottomLeft - y);

            SetPositionAndSize(newX, newY, newWidth, newHeight);
        }

        public void ResizeBottomLeftCornerToPos(double x, double y)
        {
            double oldXTopRight = CapturedTopRightPosition.X;
            double oldYTopRight = CapturedTopRightPosition.Y;

            x = Math.Min(Math.Max(x, MinX), MaxX);
            y = Math.Min(Math.Max(y, MinY), MaxY);

            double newX = Math.Min(x, oldXTopRight);
            double newY = Math.Min(y, oldYTopRight);

            double newWidth = Math.Abs(oldXTopRight - x);
            double newHeight = Math.Abs(oldYTopRight - y);

            SetPositionAndSize(newX, newY, newWidth, newHeight);
        }

        public void ResizeTopSideToPos(double y)
        {
            double oldYBottom = CapturedBottomRightPosition.Y;

            y = Math.Min(Math.Max(y, MinY), MaxY);

            double newY = Math.Min(y, oldYBottom);

            double newHeight = Math.Abs(oldYBottom - y);

            SetPositionAndSize(X, newY, Width, newHeight);
        }

        public void ResizeBottomSideToPos(double y)
        {
            double oldYTop = CapturedTopRightPosition.Y;

            y = Math.Min(Math.Max(y, MinY), MaxY);

            double newY = Math.Min(y, oldYTop);

            double newHeight = Math.Abs(oldYTop - y);

            SetPositionAndSize(X, newY, Width, newHeight);
        }

        public void ResizeLeftSideToPos(double x)
        {
            double oldXRight = CapturedBottomRightPosition.X;

            x = Math.Min(Math.Max(x, MinX), MaxX);

            double newX = Math.Min(x, oldXRight);

            double newWidth = Math.Abs(oldXRight - x);

            SetPositionAndSize(newX, Y, newWidth, Height);
        }

        public void ResizeRightSideToPos(double x)
        {
            double oldXLeft = CapturedTopLeftPosition.X;

            x = Math.Min(Math.Max(x, MinX), MaxX);

            double newX = Math.Min(x, oldXLeft);

            double newWidth = Math.Abs(oldXLeft - x);

            SetPositionAndSize(newX, Y, newWidth, Height);
        }

        //public void Move(double deltaX, double deltaY)
        //{
        //    double newX = X + deltaX;
        //    double newY = Y + deltaX;

        //    newX = Math.Min(Math.Max(newX, 0), panel.ActualWidth - Width);
        //    newY = Math.Min(Math.Max(newY, 0), panel.ActualHeight - Height);

        //    Rectangle.Margin = new Thickness(X + deltaX, Y + deltaY, 0, 0);
        //}

        public void MoveToPos(double x, double y)
        {
            double newX = x - CapturedMousePosition.X + CapturedTopLeftPosition.X;
            double newY = y - CapturedMousePosition.Y + CapturedTopLeftPosition.Y;

            newX = Math.Min(Math.Max(newX, MinX), MaxX - Width);
            newY = Math.Min(Math.Max(newY, MinY), MaxY - Height);

            SetPositionAndSize(newX, newY, Width, Height);
        }
    }
}
