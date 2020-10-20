namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using Windows.Storage;
    using controls = Windows.UI.Xaml.Controls;
    using media = Windows.UI.Xaml.Media;

    class UWPVideoViewer
    {
        controls.MediaElement Result;
        VideoPlayer View;

        public UWPVideoViewer(VideoPlayer view)
        {
            View = view;
            view.Buffered.HandleOn(Thread.UI, BufferVideo);
            View.PathChanged.HandleOn(Thread.UI, LoadVideo);
            View.Started.HandleOn(Thread.UI, () => Result.Play());
            View.Paused.HandleOn(Thread.UI, () => Result.Pause());
            View.Resumed.HandleOn(Thread.UI, () => Result.Play());
            View.Stopped.HandleOn(Thread.UI, () => Result.Stop());
            View.SoughtBeginning.HandleOn(Thread.UI, () => Result.Position = 0.Milliseconds());
            view.Muted.HandleOn(Thread.UI, () => Result.IsMuted = view.IsMuted);

            Result = new controls.MediaElement { Stretch = media.Stretch.UniformToFill };
            Result.MediaEnded += (e, args) => View.FinishedPlaying.RaiseOn(Thread.UI);
        }

        public async Task<controls.MediaElement> Render()
        {
            await LoadVideo();
            return Result;
        }

        async Task LoadVideo()
        {
            try
            {
                await DoLoadVideo();
            }
            catch (Exception ex)
            {
                await Alert.Toast("Faield to play video: " + ex.Message);
            }
        }

        async Task DoLoadVideo()
        {
            var url = View.Path;
            if (string.IsNullOrEmpty(url)) return;

            if (url.IsUrl())
            {
                if (View.AutoBuffer)
                    await BufferVideo();
            }
            else
            {
                try
                {
                    var file = Device.IO.File(url);
                    var data = await file.ReadAllBytesAsync();
                    var source = data.AsBuffer().AsStream().AsRandomAccessStream();
                    Result.SetSource(source, string.Empty);

                    View.VideoSize = await GetVideoSize(file: file);
                }
                catch (Exception ex)
                {
                    await Alert.Toast("Failed to show video: " + ex.Message);
                }
            }

            Result.AutoPlay = View.AutoPlay;
            Result.IsLooping = View.Loop;

            Result.Loaded += (e, args) => View.LoadCompleted.Raise();
        }

        void Result_BufferingProgressChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            View.IsReady = true;
        }

        async Task BufferVideo()
        {
            var url = View.Path;
            if (string.IsNullOrEmpty(url)) return;

            Result.Source = url.AsUri();
            Result.BufferingProgressChanged += Result_BufferingProgressChanged;
            View.VideoSize = await GetVideoSize(source: url.AsUri());
        }

        async Task<Size> GetVideoSize(Uri source = null, FileInfo file = null)
        {
            TimeSpan timeOfFrame = new TimeSpan(0, 0, 1);
            StorageFile currentFile;

            if (source != null)
            {
                var fileBytes = await Device.Network.Download(source);
                var tempFile = Device.IO.CreateTempFile(".mp4");
                await tempFile.WriteAllBytesAsync(fileBytes);
                currentFile = await tempFile.ToStorageFile();
            }
            else
            {
                currentFile = await file.ToStorageFile();
            }

            var encodingPropertiesToRetrieve = new List<string>();
            encodingPropertiesToRetrieve.Add("System.Video.FrameHeight");
            encodingPropertiesToRetrieve.Add("System.Video.FrameWidth");
            var encodingProperties = await currentFile.Properties.RetrievePropertiesAsync(encodingPropertiesToRetrieve);
            uint frameHeight = (uint)encodingProperties["System.Video.FrameHeight"];
            uint frameWidth = (uint)encodingProperties["System.Video.FrameWidth"];

            return new Size(frameWidth, frameHeight);
        }
    }
}