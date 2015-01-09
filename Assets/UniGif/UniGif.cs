/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static partial class UniGif
{
    /// <summary>
    /// Get GIF texture list
    /// </summary>
    /// <param name="bytes">GIF file byte data</param>
    /// <param name="loopCount">out Animation loop count</param>
    /// <param name="width">out GIF image width (px)</param>
    /// <param name="height">out GIF image height (px)</param>
    /// <param name="filterMode">Textures filter mode</param>
    /// <param name="wrapMode">Textures wrap mode</param>
    /// <param name="debugLog">Debug Log Flag</param>
    /// <returns>GIF texture list</returns>
    public static List<GifTexture> GetTextureList (byte[] bytes, out int loopCount, out int width, out int height,
        FilterMode filterMode = FilterMode.Bilinear,
        TextureWrapMode wrapMode = TextureWrapMode.Clamp,
        bool debugLog = false)
    {
        loopCount = -1;
        width = 0;
        height = 0;

        // Set GIF data
        var gifData = new GifData ();
        if (SetGifData (bytes, ref gifData, debugLog) == false) {
            Debug.LogError ("GIF file data set error.");
            return null;
        }

        // Decode to textures from GIF data
        var gifTexList = new List<GifTexture> ();
        if (DecodeTexture (ref gifData, gifTexList, filterMode, wrapMode, debugLog) == false) {
            Debug.LogError ("GIF texture decode error.");
            return null;
        }

        loopCount = gifData.appEx.loopCount;
        width = gifData.logicalScreenWidth;
        height = gifData.logicalScreenHeight;
        return gifTexList;
    }
}