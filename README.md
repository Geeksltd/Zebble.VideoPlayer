[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.VideoPlayer/master/Shared/Icon.png "Zebble.VideoPlayer"


## Zebble.VideoPlayer

![logo]

A Zebble plugin to allows you to play a video.


[![NuGet](https://img.shields.io/nuget/v/Zebble.VideoPlayer.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.VideoPlayer/)

> With this plugin you can play a video from resources or an URL. Also, it makes you able to show and hide the controls.

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.VideoPlayer/](https://www.nuget.org/packages/Zebble.VideoPlayer/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>


### Api Usage

To show a video you can use VideoPlayer mark up or c# code like below:
```xml
<VideoPlayer Path="Video/MylocalFile.mp4"/>
<VideoPlayer Path="http://www.something.com/my-file.mp4" AutoPlay="true"/>
```
```csharp
this.Add(new VideoPlayer { Path = "Video/MylocalFile.mp4" });

this.Add(new VideoPlayer { Path = "http://www.something.com/my-file.mp4", AutoPlay = true });
```
### Platform Specific Notes
Some platforms require certain settings before it will plays an online video.

#### IOS:
f you want to play a video in iOS from an Url you need to disable Transport security for that website:

If your app needs to load and display web content from non-secure sites, add the following to your app's Info.plist file to allow web pages to load correctly while Apple Transport Security (ATS) protection is still enabled for the rest of the app:
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key> NSAllowsArbitraryLoadsInWebContent</key>
    <true/>
</dict>
```
Optionally, you can make the following changes to your app's Info.plist file to completely disable ATS for all domains and internet communication:
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>
</dict>
```
You can find more information here:

https://developer.xamarin.com/guides/ios/platform_features/introduction_to_ios9/ats/

### Properties
| Property     | Type         | Android | iOS | Windows |
| :----------- | :----------- | :------ | :-- | :------ |
| Path           | string          | x       | x   | x       |
| AutoPlay  | bool | x | x | x |
| ShowControls   | bool | x | x | x |

### Events
| Event             | Type                                          | Android | iOS | Windows |
| :-----------      | :-----------                                  | :------ | :-- | :------ |
| PathChanged             | AsyncEvent    | x       | x   | x       |
| Played              | AsyncEvent    | x       | x   | x       |
| Paused              | AsyncEvent    | x       | x   | x       |
| Stopped              | AsyncEvent    | x       | x   | x       |

### Methods
| Method       | Return Type  | Parameters                          | Android | iOS | Windows |
| :----------- | :----------- | :-----------                        | :------ | :-- | :------ |
| Play         | Void| -| x       | x   | x       |
| Pause         | Void| -| x       | x   | x       |
| Stop         | Void| -| x       | x   | x       |