UniGif
======

[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat)](http://opensource.org/licenses/mit-license.php)

GIF image decoder for Unity.  

Get a texture list by decoding a GIF file in runtime.  
Supports GIF87a or GIF89a format. (Animation, Transparent, Interlaced, etc)  

It is made with Unity4.3.4 (Mac, Win, Android, iOS), work probably also in other versions.

![GIFAnimSample](https://raw.githubusercontent.com/WestHillApps/westhillapps.github.io/master/res/unigif_sample.gif)

How To Use
-------
Use GIF file bytes which have been taken from the WWW or StreamingAssets.

```csharp
int loopCount, width, height;
List<UniGif.GifTexture> gifTexList = UniGif.GetTextureList (www.bytes, out loopCount, out width, out height);
```

For more information, Please check the example scene and example components.  
* example scene - (Assets/UniGif/Example/UniGifExample.unity)  
* example component - (Assets/UniGif/Example/Script/UniGifTexture.cs)

Developed By
-------
WestHillApps (Hironari Nishioka) - <xnininix@gmail.com>

<a href="https://twitter.com/westhillapps">
<img alt="Follow me on Twitter"
src="https://raw.githubusercontent.com/WestHillApps/westhillapps.github.io/master/res/twitter.png" width="75"/>
</a>

License
-------
The MIT License (MIT)

Copyright (c) 2015 WestHillApps (Hironari Nishioka)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

License (sample gif image)
-------
![UnityChanLicense](http://unity-chan.com/images/imageLicenseLogo.png)    
These contents are licensed under the [“Unity-Chan” License Terms and Conditions](http://unity-chan.com/contents/guideline_en/).