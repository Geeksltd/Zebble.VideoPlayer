namespace Zebble
{
    using System;
    using AVFoundation;
    using Foundation;
    using UIKit;

    class IosVideo : UIView
    {
        VideoPlayer View;
        AVAsset Asset;
        AVUrlAsset UrlAsset;
        AVPlayerItem PlayerItem;
        AVPlayer Player;
        AVPlayerLayer PlayerLayer;

        public IosVideo(VideoPlayer view)
        {
            View = view;
            LoadVideo();
        }

        void LoadVideo()
        {
            UIGraphics.BeginImageContext(new CoreGraphics.CGSize(1, 1));

            Frame = View.GetFrame();

            string url = View.Path;

            if (url.IsUrl())
            {
                UrlAsset = new AVUrlAsset(NSUrl.FromString(url));
                PlayerItem = new AVPlayerItem(UrlAsset);
            }
            else
            {
                url = "file://" + Device.IO.File(url).FullName;
                Asset = AVAsset.FromUrl(NSUrl.FromString(url));
                PlayerItem = new AVPlayerItem(Asset);
            }

            Player = new AVPlayer(PlayerItem);
            PlayerLayer = AVPlayerLayer.FromPlayer(Player);

            PlayerLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
            PlayerLayer.Frame = this.Bounds;

            Layer.AddSublayer(PlayerLayer);

            UIGraphics.EndImageContext();

            if (View.AutoPlay) Player.Play();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Asset?.Dispose();
                Asset = null;

                UrlAsset?.Dispose();
                UrlAsset = null;

                PlayerItem?.Dispose();
                PlayerItem = null;

                Player?.Pause();
                Player?.Dispose();
                Player = null;

                PlayerLayer?.Dispose();
                PlayerLayer = null;

                View = null;
            }

            base.Dispose(disposing);
        }
    }
}