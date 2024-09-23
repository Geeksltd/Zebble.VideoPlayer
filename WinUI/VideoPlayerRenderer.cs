namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.UI.Xaml;

    class VideoPlayerRenderer : INativeRenderer
    {
        WinUIVideoViewer Result;

        public Task<FrameworkElement> Render(Renderer renderer)
        {
            Result = new WinUIVideoViewer((VideoPlayer)renderer.View);
            return Task.FromResult((FrameworkElement)Result.Result);
        }

        public void Dispose()
        {
            Result?.Dispose();
            Result = null;
			
			GC.SuppressFinalize(this);
        }
    }
}