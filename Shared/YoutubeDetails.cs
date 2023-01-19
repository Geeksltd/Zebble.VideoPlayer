using System;
using System.Collections.Generic;
using System.Text;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Zebble
{
    public class YoutubeDetails
    {
        public Video Video { get; set; }   
        public StreamManifest StreamManifest { get; set; }   
    }
}
