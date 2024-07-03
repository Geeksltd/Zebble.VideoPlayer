namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Android.Runtime;

    class VideoPlayerRenderer : INativeRenderer
    {
        AndroidVideo Result;

        [Preserve]
        public VideoPlayerRenderer() { }

        public Task<Android.Views.View> Render(Renderer renderer)
        {
            Result = new AndroidVideo((VideoPlayer)renderer.View);
            return Task.FromResult<Android.Views.View>(Result);
        }

        public void Dispose()
        {
            Result?.Dispose();
            Result = null;
			
			GC.SuppressFinalize(this);
        }
    }
}