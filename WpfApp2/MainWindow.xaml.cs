using WpfApp2.Native;
using System;
using System.Collections.Generic;
using System.IO;
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
using WpfApp2.View.Components;
using WpfApp2.Data;
using System.Collections.Immutable;
using static WpfApp2.Native.WindowResizer;
using LiteDB;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            WpfApp2.Data.ImageLabelStateRepository.ConfigureBsonMapper();
            WpfApp2.Data.WorkspaceRepository.ConfigureBsonMapper();

            ServiceThread.Instance.Start();

            DataContext = WindowStateContext;
            InitializeComponent();

            WindowResizer = new WindowResizer(this);
            WindowResizer.DisableMaximizeButton();

            workspace = WorkspaceRepository.GetLastRecentWorkspace();

            Loaded += async (sender, e) =>
            {
                var msg = await LoadWorkspace(workspace);

                if (!string.IsNullOrEmpty(msg))
                {
                    workspace = null;
                    WorkspaceRepository.Delete(workspace.Path);
                    ImageListViewer.Visibility = Visibility.Hidden;
                    Toolbar1.Visibility = Visibility.Hidden;
                    Toolbar2.Visibility = Visibility.Hidden;
                    ImageListViewer.SelectedIndex = -1;
                    ImageListViewer.ItemsSource = null;
                    ImgCropingPanel.ClearImage();
                    ImgCropingPanel.ClearCropingRectangles();
                    MessageBox.Show(msg);
                }

                //DiableReactiveUI();
            };

            Closed += (sender, e) =>
            {
                ServiceThread.Instance.Stop();
            };
        }

        private void DisableReactiveUI()
        {
            ImgCropingPanel.IsEnabled = false;
            ImageListViewer.IsEnabled = false;
            foreach (var child in CheckLabelGroup.Children)
            {
                var checkbox = child as CheckBox;
                checkbox.IsEnabled = false;
            }

            WindowStateContext.AllReactiveUIEnabled = false;
        }

        private void EnableReactiveUI()
        {
            ImgCropingPanel.IsEnabled = true;
            ImageListViewer.IsEnabled = true;
            foreach (var child in CheckLabelGroup.Children)
            {
                var checkbox = child as CheckBox;
                checkbox.IsEnabled = true;
            }

            WindowStateContext.AllReactiveUIEnabled = true;
        }

        WindowStateContext WindowStateContext = new WindowStateContext() 
        { 
            AllReactiveUIEnabled = true,
        };
        WorkspaceRepository WorkspaceRepository = new WorkspaceRepository();
        ImageLabelStateRepository ImageLabelStateRepository;
        WindowResizer WindowResizer;
        Workspace? workspace;
        List<ImageInfo> imageList;


        private async Task<string?> LoadWorkspace(Workspace workspace)
        {
            if (workspace == null)
            {
                ImageListViewer.SelectedIndex = -1;
                ImageListViewer.ItemsSource = null;
                ImgCropingPanel.ClearImage();
                ImgCropingPanel.ClearCropingRectangles();
                ImageListViewer.Visibility = Visibility.Hidden;
                Toolbar1.Visibility = Visibility.Hidden;
                Toolbar2.Visibility = Visibility.Hidden;

                return null;
            }

            var files = System.IO.Directory.GetFiles(workspace.Path);

            var images = files.Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".jpeg"));

            if (images.Count() == 0)
            {
                return "No image in this folder";
            }

            var wsFolder = workspace.WsFolder;

            if (!System.IO.Directory.Exists(wsFolder))
            {
                System.IO.Directory.CreateDirectory(wsFolder);
            }

            ImageLabelStateRepository = new ImageLabelStateRepository(wsFolder);

            imageList = images.Select(f => new ImageInfo()
            {
                LabelState = new ImageLabelState()
                {
                    ImagePath = System.IO.Path.GetFileName(f),
                    Labels = labels.ToDictionary(x => x, x => false),
                },
                ProcessState = ImageProcessState.Unlabeled,
            }).ToList();

            var imageLabelStates = ImageLabelStateRepository.GetAll().ToImmutableDictionary(x => x.ImagePath, x => x);
            
            foreach(var image in imageList)
            {
                ImageLabelState? state;
                if(imageLabelStates.TryGetValue(image.LabelState.ImagePath, out state))
                {
                    image.LabelState = state;
                    image.ProcessState = ImageProcessState.Labeled;
                }
            }

            //ImageListViewer.SelectedIndex = -1;
            ImageListViewer.ItemsSource = imageList;
            ImageListViewer.SelectedIndex = 0;
            Toolbar1.Visibility = Visibility.Visible;
            Toolbar2.Visibility = Visibility.Visible;
            ImageListViewer.Visibility = Visibility.Visible;
            return null;
        }

        private void OpenWorkspace(object sender, ExecutedRoutedEventArgs e)
        {
            //Clear current workspace

            //Open new workspace
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var path = dialog.SelectedPath;
                    var files = System.IO.Directory.GetFiles(path);

                    if(files.Length == 0)
                    {
                        System.Windows.MessageBox.Show("No image in this folder");
                        return;
                    }

                    var existWorkspace = WorkspaceRepository.Get(path);

                    if(existWorkspace != null)
                    {
                        workspace = existWorkspace;
                        workspace.LastVisitedTime = DateTime.Now;

                        WorkspaceRepository.Update(workspace);
                    }
                    else
                    {
                        workspace = new Workspace()
                        {
                            Path = path,
                            LastVisitedTime = DateTime.Now,
                        };

                        WorkspaceRepository.Insert(workspace);
                    }

                    LoadWorkspace(workspace);
                }
            }
        }

        private void NextImage(object sender, ExecutedRoutedEventArgs e)
        {
            ImageListViewer.SelectedIndex = (ImageListViewer.SelectedIndex + 1) % ImageListViewer.Items.Count;
        }

        private void PreviousImage(object sender, ExecutedRoutedEventArgs e)
        {
            ImageListViewer.SelectedIndex = (ImageListViewer.SelectedIndex + ImageListViewer.Items.Count - 1) % ImageListViewer.Items.Count;
        }
        private void ExportToCsv(object sender, ExecutedRoutedEventArgs e)
        {
            DisableReactiveUI();

            AlertMsg.Foreground = (Brush) FindResource("LightYellow2");
            AlertMsg.Text = "Exporting 0%";

            ServiceThread.Instance.ExportCsvAndResizedImages(workspace, (progress) =>
            {
                if (progress == 100)
                {
                    this.Dispatcher.InvokeAsync(() =>
                    {
                        EnableReactiveUI();
                        AlertMsg.Foreground = (Brush)FindResource("LightGreen2");
                        AlertMsg.Text = "Exported successfully at " + DateTime.Now.ToString("HH:mm dd/MM/yyyy") + ".";
                    });
                }
                else
                {
                    AlertMsg.Dispatcher.InvokeAsync(() =>
                    {
                        AlertMsg.Text = "Exporting " + progress + "%";
                    });
                }
            }); 
        }

        private bool checkboxChecked = false;
        private string[] labels = new string[] { "HG", "HT", "TR", "CTH", "BD", "VH", "CTQ", "DQT", "KS", "CVN" };    
        private void SetLabel(object sender, ExecutedRoutedEventArgs e)
        {
            if (workspace == null) return;

            var imageInfo = ImageListViewer.SelectedItem as ImageInfo;
            var imageLabelState = imageInfo.LabelState;

            var labelIndex = int.Parse(e.Parameter.ToString());
            if(labelIndex == 0) labelIndex = 10;
            labelIndex--;

            var checkbox = CheckLabelGroup.Children[labelIndex] as CheckBox;
            var currentLabelEnable = checkbox.IsChecked.HasValue && checkbox.IsChecked.Value;
            var currentLabel = labels[labelIndex];

            if (checkboxChecked)
            {
                checkboxChecked = false;
            }
            else
            {
                currentLabelEnable = !currentLabelEnable;
                checkbox.IsChecked = currentLabelEnable;
            }

            imageLabelState.Labels[currentLabel] = currentLabelEnable;

            if(imageLabelState.CutWidth == 0 || imageLabelState.CutHeight == 0)
            {
                var rect = ImgCropingPanel.ImageCropingRectangle;
                imageLabelState.CutOffsetX = rect.X;
                imageLabelState.CutOffsetY = rect.Y;
                imageLabelState.CutWidth = rect.Width;
                imageLabelState.CutHeight = rect.Height;
            }

            if (UpdateImageLabelState(imageLabelState, imageInfo))
            {
                imageInfo.ProcessState = ImageProcessState.Saving;
            }
            else
            {
                imageInfo.ProcessState = ImageProcessState.Invalid;
            }

        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            checkboxChecked = true;
        }

        private void ImageListViewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            if (workspace == null) return;

            var imageInfo = e.AddedItems[0] as ImageInfo;
            var imagePath = imageInfo.LabelState.ImagePath;
            var imageFullPath = System.IO.Path.Combine(workspace.Path, imagePath);

            if (File.Exists(imageFullPath) == false)
            {
                System.Windows.MessageBox.Show("Image not found");
                return;
            }

            if (imageFullPath != null)
            {
                ImgCropingPanel.ClearImage();
                var image = new BitmapImage(new Uri(imageFullPath));
                ImgCropingPanel.SetImage(image);

                var imageLabelState = imageInfo.LabelState;
                ImgCropingPanel.SetRect(imageLabelState.CutOffsetX, imageLabelState.CutOffsetY, imageLabelState.CutWidth, imageLabelState.CutHeight);

                for (int i = 0; i < labels.Length; i++)
                {
                    var checkbox = CheckLabelGroup.Children[i] as CheckBox;
                    checkbox.IsChecked = imageLabelState.Labels[labels[i]];
                }
            }
        }

        private bool UpdateImageLabelState(ImageLabelState imageLabelState, ImageInfo imageInfo)
        {
            if(imageLabelState.CutOffsetX >= 0 && imageLabelState.CutOffsetY >= 0 && imageLabelState.CutWidth > 0 && imageLabelState.CutHeight > 0)
            {
                var labelCount = imageLabelState.Labels.Count(x => x.Value);
                if(labelCount > 1)
                {
                    imageLabelState.StateVersion++;

                    ServiceThread.Instance.UpdateImageLabelState(workspace, imageLabelState.Clone(), (ws, imgPath, ver) =>
                    {
                        ImageListViewer.Dispatcher.InvokeAsync(() =>
                        {
                            if(workspace.Path != ws)
                            {
                                return;
                            }

                            if(imageLabelState.ImagePath != imgPath)
                                throw new Exception("Image path not match");

                            if(imageLabelState.StateVersion == ver)
                            {
                                imageInfo.ProcessState = ImageProcessState.Labeled;
                            }
                        });
                    });

                    return true;
                }
            }
            
            return false;   
        }

        private void ImgCropingPanel_CropingRectangleChanged(object sender, RoutedEventArgs e)
        {
            var imageInfo = ImageListViewer.SelectedItem as ImageInfo;
            var imageLabelState = imageInfo.LabelState;
            var rect = ImgCropingPanel.ImageCropingRectangle;

            imageLabelState.CutOffsetX = rect.X;
            imageLabelState.CutOffsetY = rect.Y;
            imageLabelState.CutWidth = rect.Width;
            imageLabelState.CutHeight = rect.Height;

            if (UpdateImageLabelState(imageLabelState, imageInfo))
            {
                imageInfo.ProcessState = ImageProcessState.Saving;
            }
            else
            {
                imageInfo.ProcessState = ImageProcessState.Invalid;
            }
        }

        private void ClearCropingRect(object sender, ExecutedRoutedEventArgs e)
        {
            ImgCropingPanel.ClearCropingRectangles();

            var imageInfo = ImageListViewer.SelectedItem as ImageInfo;
            var imageLabelState = imageInfo.LabelState;
            var rect = ImgCropingPanel.ImageCropingRectangle;

            imageLabelState.CutOffsetX = rect.X;
            imageLabelState.CutOffsetY = rect.Y;
            imageLabelState.CutWidth = rect.Width;
            imageLabelState.CutHeight = rect.Height;

            if (UpdateImageLabelState(imageLabelState, imageInfo))
            {
                imageInfo.ProcessState = ImageProcessState.Saving;
            }
            else
            {
                imageInfo.ProcessState = ImageProcessState.Invalid;
            }
        }
    }
}
