using FlowBoard.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.Storage.Provider;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace FlowBoard.Helpers
{
    public class FileHelper
    {
        public static ObservableCollection<AccessListEntry> Recents = new();
        public static async void RefreshRecentItems()
        {
            Recents.Clear();
            foreach (AccessListEntry entry in StorageApplicationPermissions.MostRecentlyUsedList.Entries)
            {
                try
                {
                    await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(entry.Token);
                    Recents.Add(entry);
                }
                catch
                {
                    // File does not exist
                    StorageApplicationPermissions.MostRecentlyUsedList.Remove(entry.Token);
                }
            }
        }

        public static void RemoveDuplicate(string Name)
        {
            foreach (AccessListEntry entry in StorageApplicationPermissions.MostRecentlyUsedList.Entries)
            {
                if (entry.Metadata.Replace(".png", "") == Name)
                {
                    StorageApplicationPermissions.MostRecentlyUsedList.Remove(entry.Token);
                }
            }
        }

        public static ObservableCollection<StorageFile> Files = new();
        public static async void RefreshFiles()
        {
            Files.Clear();
            foreach (StorageFile File in await ApplicationData.Current.LocalFolder.GetFilesAsync())
            {
                if (!File.Name.EndsWith(".png"))
                {
                    Files.Add(File);
                }
            }
        }

        public static async Task<bool> CreateProjectAsync(string Name, Color background)
        {
            try
            {
                ProjectClass project = new ProjectClass(Name);
                project.File.CanvasColor = background;
                project.RawFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(Name + ".flowx", CreationCollisionOption.GenerateUniqueName);
                project.Name = project.RawFile.DisplayName;
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(MainPage), project);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> OpenProjectAsync(StorageFile File)
        {
            try
            {
                ProjectClass project = new ProjectClass(File.Name);
                project.File = await OpenFileAsync(File);
                project.RawFile = File;
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(MainPage), project);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<FileClass> OpenFilePicker()
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".flowx");
            return JsonConvert.DeserializeObject<FileClass>(await FileIO.ReadTextAsync(await picker.PickSingleFileAsync()));
        }

        public static async Task<FileClass> OpenFileAsync(StorageFile File) => JsonConvert.DeserializeObject<FileClass>(await FileIO.ReadTextAsync(File));

        public static async Task<bool> SaveNewFile(Color canvasColor, List<FileInkStroke> inkStrokes)
        {
            FileClass File = new FileClass();
            File.CanvasColor = canvasColor;
            File.Version = 1;
            File.InkStrokes = inkStrokes;
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("FlowBoard Project", new List<string>() { ".flowx" });
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Project";
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(File));
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(HomePage));
                return status == FileUpdateStatus.Complete ? true : false;
            }
            else
            {
                return false;
            }
        }

        public static async Task<bool> IsFilePresent(string fileName)
        {
            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
            return item != null;
        }

        public static async Task<string> GetSequentialNewWhiteboardFilename()
        {
            Regex rx = new Regex(@"(New Whiteboard$|New Whiteboard \(([0-9]+)\)$)");

            int highestNumber = -1;
            var items = await ApplicationData.Current.LocalFolder.GetItemsAsync();
            foreach (var item in items)
            {
                foreach (Match match in rx.Matches(item.Name))
                {
                    highestNumber = match.Groups[2].Length == 0 ? 0 : int.Parse(match.Groups[2].Value);
                }
            }

            string postfix = highestNumber < 0 ? "" : " (" + (highestNumber + 1).ToString() + ")";
            return "New Whiteboard" + postfix;
        }

        // Save the project + add it to the most recently used list + get a preview image
        public static async Task<bool> SaveProjectAsync(Color canvasColor, InkCanvas inkCanvas, ProjectClass project, string PreviewName, bool IsSilent)
        {
            List<FileInkStroke> strokes = new List<FileInkStroke>();
            try
            {
                Parallel.ForEach(inkCanvas.InkPresenter.StrokeContainer.GetStrokes(), i =>
                {
                    strokes.Add(FileHelper.ConvertToFileInkStroke(i));
                });
            }
            catch
            {
                // No strokes drawn
            }
            project.File.CanvasColor = canvasColor;
            project.File.InkStrokes = strokes;
            if(project.RawFile.Name != project.Name)
            {
                await project.RawFile.RenameAsync(project.Name);
            }
            CachedFileManager.DeferUpdates(project.RawFile);
            await FileIO.WriteTextAsync(project.RawFile, JsonConvert.SerializeObject(project.File));
            FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(project.RawFile);
            StorageApplicationPermissions.MostRecentlyUsedList.Add(project.RawFile, PreviewName);
            if(IsSilent == true)
                return status == FileUpdateStatus.Complete ? true : false;
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(HomePage));
            return status == FileUpdateStatus.Complete ? true : false;
        }

        public static async Task<string> SavePreview(InkCanvas inkCanvas, string Name)
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(inkCanvas);
            var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
            var pixels = pixelBuffer.ToArray();
            var displayInformation = DisplayInformation.GetForCurrentView();
            StorageFolder localFolder =
            ApplicationData.Current.LocalFolder;
            StorageFile file = await localFolder.CreateFileAsync(Name + ".png", CreationCollisionOption.ReplaceExisting);
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                     BitmapAlphaMode.Premultiplied,
                                     (uint)renderTargetBitmap.PixelWidth,
                                     (uint)renderTargetBitmap.PixelHeight,
                                      displayInformation.RawDpiX,
                         displayInformation.RawDpiY,
                                     pixels);
                await encoder.FlushAsync();
            }
            return file.Name;
        }

        public static FileInkStroke ConvertToFileInkStroke(InkStroke ink)
        {
            List<FileInkPoint> Points = new List<FileInkPoint>();
            foreach(var i in ink.GetInkPoints())
            {
                Points.Add(new FileInkPoint
                {
                    Position = i.Position,
                    TiltX = i.TiltX,
                    TiltY = i.TiltY,
                    TimeStamp = i.Timestamp,
                    Pressure = i.Pressure
                });
            }
            return new FileInkStroke
            {
                IsPenTipCircle = ink.DrawingAttributes.PenTip == PenTipShape.Circle,
                IgnorePressure = ink.DrawingAttributes.IgnorePressure,
                IgnoreTilt = ink.DrawingAttributes.IgnoreTilt,
                Color = ink.DrawingAttributes.Color,
                DrawAsHighlighter = ink.DrawingAttributes.DrawAsHighlighter,
                Size = ink.DrawingAttributes.Size,
                PointTransform = ink.PointTransform,
                InkPoints = Points
            };
        }
    }
}
