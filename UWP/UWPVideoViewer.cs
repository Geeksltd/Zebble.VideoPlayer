namespace Zebble
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using controls = Windows.UI.Xaml.Controls;
    using media = Windows.UI.Xaml.Media;

    class UWPVideoViewer
    {
        controls.MediaElement Result;
        VideoPlayer View;

        public UWPVideoViewer(VideoPlayer view)
        {
            View = view;
            View.PathChanged.HandleOn(Thread.UI, LoadVideo);
            View.Played.HandleOn(Thread.UI, () => Result.Play());
            View.Paused.HandleOn(Thread.UI, () => Result.Pause());
            View.Stopped.HandleOn(Thread.UI, () => Result.Stop());

            Result = new controls.MediaElement { Stretch = media.Stretch.UniformToFill };
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

            if (url.IsUrl())
            {
                Result.Source = url.AsUri();
            }
            else
            {
                try
                {
                    var data = await Device.IO.File(url).ReadAllBytesAsync();
                    var source = data.AsBuffer().AsStream().AsRandomAccessStream();
                    Result.SetSource(source, string.Empty);
                }
                catch (Exception ex)
                {
                    await Alert.Toast("Failed to show video: " + ex.Message);
                }
            }

            Result.AutoPlay = View.AutoPlay;
            Result.IsLooping = View.Loop;
        }
    }
}