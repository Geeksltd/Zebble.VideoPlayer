namespace Zebble.Plugin.Renderer
{
    using System.ComponentModel;
    using System.Threading.Tasks;
    using UIKit;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VideoPlayerRenderer : ICustomRenderer
    {
        VideoPlayer View;
        UIView Result;

        public Task<UIView> Render(object view)
        {
            View = (VideoPlayer)view;
            Result = new IosVideo(View);

            return Task.FromResult(Result);
        }

        public void Dispose()
        {
            Result?.Dispose();
            View = null;
            Result = null;
        }
    }
}