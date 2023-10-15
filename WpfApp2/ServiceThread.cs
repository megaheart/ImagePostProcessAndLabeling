using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows.Documents;
using System.Threading;
using WpfApp2.Models;
using WpfApp2.Data;
using WpfApp2.ServiceThreadUtil.Models;
using WpfApp2.ServiceThreadUtil.Helpers;
using System.IO;
using System.Diagnostics;

namespace WpfApp2
{
    public delegate void UpdateImageLabelStateCallback(string workspacePath, string imgPath, int stateVersion);
    public class ServiceThread
    {
        public static readonly ServiceThread Instance = new ServiceThread();
        public ServiceThread()
        {

        }


        private void HandleSaveImageLabelState(ImageLabelStateUpdateModel model, Action callback)
        {
            var repo = new ImageLabelStateRepository(model.WsFolder);

            repo.InsertOrReplace(model.State);

            callback();
        }

        private void HandleExportCsvAndResizedImages(string[] labels, ExportCsvAndResizedImagesModel model, Action<int> progressCallback)
        {
            if(Directory.Exists(model.OutputFolder))
            {
                Directory.Delete(model.OutputFolder, true);
            }
            Directory.CreateDirectory(model.OutputFolder);
            Directory.CreateDirectory(Path.Combine(model.OutputFolder, "imgs"));

            var repo = new ImageLabelStateRepository(model.WsFolder);
            var states = repo.GetAll();
            StringBuilder csvBuilder = new StringBuilder();

            csvBuilder.Append("A," + string.Join(",", labels));
            int i = 0;
            foreach (var state in states)
            {
                var imgPath = Path.Combine(model.WorkspacePath, state.ImagePath);
                var imgOutputPath = Path.Combine(model.OutputFolder, "imgs", state.ImagePath);
                // Load image from disk and resize
                // If image is not found, skip
                if(!ImageHelper.ProcessImage(imgPath, imgOutputPath, state.CutOffsetX, state.CutOffsetY, state.CutWidth, state.CutHeight))
                {
                    continue;
                }
                csvBuilder.AppendLine();

                // Import image to csv
                csvBuilder.Append(state.ImagePath);
                foreach (var label in labels)
                {
                    csvBuilder.Append(',');
                    csvBuilder.Append(state.Labels.TryGetValue(label, out var t) && t ? '1' : '0');
                }

                i++;
                if(i != states.Count)
                {
                    progressCallback(i * 100 / states.Count);
                }
            }

            File.WriteAllText(Path.Combine(model.OutputFolder, "labels.csv"), csvBuilder.ToString());
            progressCallback(100);

            Process.Start("explorer.exe", model.OutputFolder);
        }

        public BlockingCollection<ServiceWork> WorkQueue { get; private set; } = new BlockingCollection<ServiceWork>();

        private void ThreadProc()
        {
            string[] labels = new string[] { "HG", "HT", "TR", "CTH", "BD", "VH", "CTQ", "DQT", "KS", "CVN" };

            while (true)
            {
                var work = WorkQueue.Take();
                switch (work.Type)
                {
                    case ServiceWorkType.UpdateImageLabelState:
                        HandleSaveImageLabelState(work.Data as ImageLabelStateUpdateModel, work.Callback);
                        break;
                    case ServiceWorkType.Shutdown:
                        return;
                    case ServiceWorkType.ExportCsvAndResizedImages:
                        HandleExportCsvAndResizedImages(labels, work.Data as ExportCsvAndResizedImagesModel, work.ProgressCallback);
                        break;
                }
            }
        }

        Thread thread = null;
        public void Start()
        {
            thread = new Thread(new ThreadStart(ThreadProc));
            thread.Start();
        }

        public void UpdateImageLabelState(Workspace workspace, ImageLabelState state, UpdateImageLabelStateCallback callback)
        {
            var model = new ImageLabelStateUpdateModel
            {
                WorkspacePath = workspace.Path,
                WsFolder = workspace.WsFolder,
                State = state
            };
            var work = new ServiceWork
            {
                Type = ServiceWorkType.UpdateImageLabelState,
                Data = model,
                Callback = () =>
                {
                    callback?.Invoke(model.WorkspacePath, state.ImagePath, state.StateVersion);
                }
            };
            WorkQueue.Add(work);
        }

        public void ExportCsvAndResizedImages(Workspace workspace, Action<int> progressCallback)
        {
            var model = new ExportCsvAndResizedImagesModel
            {
                WorkspacePath = workspace.Path,
                WsFolder = workspace.WsFolder,
                OutputFolder = workspace.OutputFolder
            };
            var work = new ServiceWork
            {
                Type = ServiceWorkType.ExportCsvAndResizedImages,
                Data = model,
                ProgressCallback = progressCallback
            };
            WorkQueue.Add(work);
        }

        public void Stop()
        {
            WorkQueue.Add(new ServiceWork { Type = ServiceWorkType.Shutdown });
            thread.Join();
        }

    }
}
