/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static partial class UniGif
{
    /// <summary>
    /// Decode to textures from GIF data
    /// </summary>
    /// <param name="gifData">GIF data</param>
    /// <param name="gifTexList">GIF texture list</param>
    /// <param name="filterMode">Textures filter mode</param>
    /// <param name="wrapMode">Textures wrap mode</param>
    /// <returns>IEnumerator</returns>
    static IEnumerator DecodeTextureCoroutine (GifData gifData, Action<List<GifTexture>> cb, FilterMode filterMode, TextureWrapMode wrapMode)
    {
        if (gifData.imageBlockList == null || gifData.imageBlockList.Count < 1) {
            yield break;
        }

        List<GifTexture> gifTexList = new List<GifTexture> ();

        Color32? bgColor = GetGlobalBgColor (gifData);

        // Disposal Method
        // 0 (No disposal specified)
        // 1 (Do not dispose)
        // 2 (Restore to background color)
        // 3 (Restore to previous)
        ushort disposalMethod = 0;

        int imgBlockIndex = 0;
        foreach (var imgBlock in gifData.imageBlockList) {
            var decodedData = GetDecodedData (imgBlock);

            var colorTable = GetColorTable (gifData, imgBlock, ref bgColor);

            var graphicCtrlEx = GetGraphicCtrlExt (gifData, imgBlockIndex);

            int transparentIndex = GetTransparentIndex (graphicCtrlEx);

            // avoid lock up
            yield return 0;

            bool useBeforeTex = false;
            var tex = CreateTexture2D (gifData, gifTexList, imgBlockIndex, disposalMethod, filterMode, wrapMode, ref useBeforeTex);

            // Set pixel data
            int dataIndex = 0;
            // Reverse set pixels. because GIF data starts from the top left.
            for (int y = tex.height - 1; y >= 0; y--) {
                SetTexturePixelRow (tex, y, imgBlock, decodedData, ref dataIndex, colorTable, bgColor, transparentIndex, useBeforeTex);

                // avoid lock up
                if (y % 10 == 0) {
                    yield return 0;
                }
            }
            tex.Apply ();

            float delaySec = GetDelaySec (graphicCtrlEx);

            // Add to GIF texture list
            gifTexList.Add (new GifTexture (tex, delaySec));

            disposalMethod = GetDisposalMethod (graphicCtrlEx);

            imgBlockIndex++;

            // avoid lock up
            yield return 0;
        }

        if (cb != null) {
            cb (gifTexList);
        }
    }

    /// <summary>
    /// Decode to textures from GIF data
    /// </summary>
    /// <param name="gifData">GIF data</param>
    /// <param name="gifTexList">GIF texture list</param>
    /// <param name="filterMode">Textures filter mode</param>
    /// <param name="wrapMode">Textures wrap mode</param>
    /// <returns>Result</returns>
    static bool DecodeTexture (GifData gifData, List<GifTexture> gifTexList, FilterMode filterMode, TextureWrapMode wrapMode)
    {
        if (gifData.imageBlockList == null || gifData.imageBlockList.Count < 1) {
            return false;
        }

        Color32? bgColor = GetGlobalBgColor (gifData);

        // Disposal Method
        // 0 (No disposal specified)
        // 1 (Do not dispose)
        // 2 (Restore to background color)
        // 3 (Restore to previous)
        ushort disposalMethod = 0;

        int imgBlockIndex = 0;
        foreach (var imgBlock in gifData.imageBlockList) {
            var decodedData = GetDecodedData (imgBlock);

            var colorTable = GetColorTable (gifData, imgBlock, ref bgColor);

            var graphicCtrlEx = GetGraphicCtrlExt (gifData, imgBlockIndex);

            int transparentIndex = GetTransparentIndex (graphicCtrlEx);

            bool useBeforeTex = false;
            var tex = CreateTexture2D (gifData, gifTexList, imgBlockIndex, disposalMethod, filterMode, wrapMode, ref useBeforeTex);

            // Set pixel data
            int dataIndex = 0;
            // Reverse set pixels. because GIF data starts from the top left.
            for (int y = tex.height - 1; y >= 0; y--) {
                SetTexturePixelRow (tex, y, imgBlock, decodedData, ref dataIndex, colorTable, bgColor, transparentIndex, useBeforeTex);
            }
            tex.Apply ();

            float delaySec = GetDelaySec (graphicCtrlEx);

            // Add to GIF texture list
            if (gifTexList == null) {
                gifTexList = new List<GifTexture> ();
            }
            gifTexList.Add (new GifTexture (tex, delaySec));

            disposalMethod = GetDisposalMethod (graphicCtrlEx);

            imgBlockIndex++;
        }

        return true;
    }

    #region Call from DecodeTexture methods

    /// <summary>
    /// Get background color from global color table
    /// </summary>
    static Color32? GetGlobalBgColor (GifData gifData)
    {
        Color32? bgColor = null;
        if (gifData.globalColorTableFlag) {
            // Set background color from global color table
            byte[] bgRgb = gifData.globalColorTable[gifData.bgColorIndex];
            bgColor = new Color32 (bgRgb[0], bgRgb[1], bgRgb[2], 255);
        }
        return bgColor;
    }

    /// <summary>
    /// Get decoded image data from ImageBlock
    /// </summary>
    static byte[] GetDecodedData (ImageBlock imgBlock)
    {
        // Combine LZW compressed data
        List<byte> lzwData = new List<byte> ();
        foreach (var imageData in imgBlock.imageDataList) {
            foreach (var data in imageData.imageData) {
                lzwData.Add (data);
            }
        }

        // LZW decode
        int needDataSize = imgBlock.imageHeight * imgBlock.imageWidth;
        byte[] decodedData = DecodeGifLZW (lzwData, imgBlock.LzwMinimumCodeSize, needDataSize);

        // Sort interlace GIF
        if (imgBlock.interlaceFlag) {
            decodedData = SortInterlaceGifData (decodedData, imgBlock.imageWidth);
        }
        return decodedData;
    }

    /// <summary>
    /// Get color table (local or global)
    /// </summary>
    static List<byte[]> GetColorTable (GifData gifData, ImageBlock imgBlock, ref Color32? bgColor)
    {
        var colorTable = imgBlock.localColorTableFlag ? imgBlock.localColorTable : gifData.globalColorTable;

        if (imgBlock.localColorTableFlag) {
            // Set background color from local color table
            byte[] bgRgb = imgBlock.localColorTable[gifData.bgColorIndex];
            bgColor = new Color32 (bgRgb[0], bgRgb[1], bgRgb[2], 255);
        }

        return colorTable;
    }

    /// <summary>
    /// Get GraphicControlExtension from GifData
    /// </summary>
    static GraphicControlExtension? GetGraphicCtrlExt (GifData gifData, int imgBlockIndex)
    {
        if (gifData.graphicCtrlExList != null && gifData.graphicCtrlExList.Count > imgBlockIndex) {
            return gifData.graphicCtrlExList[imgBlockIndex];
        }
        return null;
    }

    /// <summary>
    /// Get transparent color index from GraphicControlExtension
    /// </summary>
    static int GetTransparentIndex (GraphicControlExtension? graphicCtrlEx)
    {
        int transparentIndex = -1;
        if (graphicCtrlEx != null && graphicCtrlEx.Value.transparentColorFlag) {
            transparentIndex = graphicCtrlEx.Value.transparentColorIndex;
        }
        return transparentIndex;
    }

    /// <summary>
    /// Get delay seconds from GraphicControlExtension
    /// </summary>
    static float GetDelaySec (GraphicControlExtension? graphicCtrlEx)
    {
        // Get delay sec from GraphicControlExtension
        float delaySec = graphicCtrlEx != null ? (float) graphicCtrlEx.Value.delayTime / 100f : 0.1f;
        // Minimum 0.1 seconds delay (because major browsers have become so...)
        if (delaySec < 0.1f) {
            delaySec = 0.1f;
        }
        return delaySec;
    }

    /// <summary>
    /// Get disposal method from GraphicControlExtension
    /// </summary>
    static ushort GetDisposalMethod (GraphicControlExtension? graphicCtrlEx)
    {
        return graphicCtrlEx != null ? graphicCtrlEx.Value.disposalMethod : (ushort) 2;
    }

    /// <summary>
    /// Create Texture2D object and initial settings
    /// </summary>
    static Texture2D CreateTexture2D (GifData gifData, List<GifTexture> gifTexList, int imgBlockIndex, ushort disposalMethod, FilterMode filterMode, TextureWrapMode wrapMode, ref bool useBeforeTex)
    {
        // Create texture
        Texture2D tex = new Texture2D (gifData.logicalScreenWidth, gifData.logicalScreenHeight, TextureFormat.ARGB32, false);
        tex.filterMode = filterMode;
        tex.wrapMode = wrapMode;

        // Check dispose
        useBeforeTex = false;
        int beforeIndex = -1;
        if (imgBlockIndex > 0 && disposalMethod == 0 || disposalMethod == 1) {
            // before 1
            beforeIndex = imgBlockIndex - 1;
        } else if (imgBlockIndex > 1 && disposalMethod == 3) {
            // before 2
            beforeIndex = imgBlockIndex - 2;
        }
        if (beforeIndex >= 0) {
            // Do not dispose
            useBeforeTex = true;
            Color32[] pix = gifTexList[beforeIndex].texture2d.GetPixels32 ();
            tex.SetPixels32 (pix);
            tex.Apply ();
        }

        return tex;
    }

    /// <summary>
    /// Set texture pixel row
    /// </summary>
    static void SetTexturePixelRow (Texture2D tex, int y, ImageBlock imgBlock, byte[] decodedData, ref int dataIndex, List<byte[]> colorTable, Color32? bgColor, int transparentIndex, bool useBeforeTex)
    {
        // Row no (0~)
        int row = tex.height - 1 - y;

        for (int x = 0; x < tex.width; x++) {
            // Line no (0~)
            int line = x;

            // Out of image blocks
            if (row < imgBlock.imageTopPosition ||
                row >= imgBlock.imageTopPosition + imgBlock.imageHeight ||
                line < imgBlock.imageLeftPosition ||
                line >= imgBlock.imageLeftPosition + imgBlock.imageWidth) {
                if (useBeforeTex == false && bgColor != null) {
                    tex.SetPixel (x, y, bgColor.Value);
                }
                continue;
            }

            // Out of decoded data
            if (dataIndex >= decodedData.Length) {
                tex.SetPixel (x, y, Color.black);
                // Debug.LogError ("dataIndex exceeded the size of decodedData. dataIndex:" + dataIndex + " decodedData.Length:" + decodedData.Length + " y:" + y + " x:" + x);
                dataIndex++;
                continue;
            }

            // Get pixel color from color table
            byte colorIndex = decodedData[dataIndex];
            if (colorTable == null || colorTable.Count <= colorIndex) {
                Debug.LogError ("colorIndex exceeded the size of colorTable. colorTable.Count:" + colorTable.Count + " colorIndex:" + colorIndex);
                dataIndex++;
                continue;
            }
            byte[] rgb = colorTable[colorIndex];

            // Set alpha
            byte alpha = transparentIndex >= 0 && transparentIndex == colorIndex ? (byte) 0 : (byte) 255;

            if (alpha != 0 || useBeforeTex == false) {
                Color32 col = new Color32 (rgb[0], rgb[1], rgb[2], alpha);
                tex.SetPixel (x, y, col);
            }

            dataIndex++;
        }
    }

    #endregion

    #region Decode LZW & Sort interrace methods

    /// <summary>
    /// GIF LZW decode
    /// </summary>
    /// <param name="compData">LZW compressed data</param>
    /// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
    /// <param name="needDataSize">Need decoded data size</param>
    /// <returns>Decoded data array</returns>
    static byte[] DecodeGifLZW (List<byte> compData, int lzwMinimumCodeSize, int needDataSize)
    {
        int clearCode = 0;
        int finishCode = 0;

        // Initialize dictionary
        Dictionary<int, string> dic = new Dictionary<int, string> ();
        int lzwCodeSize = 0;
        InitDictionary (dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);

        // Convert to bit array
        byte[] compDataArr = compData.ToArray ();
        var bitData = new BitArray (compDataArr);

        byte[] output = new byte[needDataSize];
        int outputAddIndex = 0;

        string prevEntry = null;

        bool dicInitFlag = false;

        int bitDataIndex = 0;

        // LZW decode loop
        while (bitDataIndex < bitData.Length) {
            if (dicInitFlag) {
                InitDictionary (dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);
                dicInitFlag = false;
            }

            int key = bitData.GetNumeral (bitDataIndex, lzwCodeSize);

            string entry = null;

            if (key == clearCode) {
                // Clear (Initialize dictionary)
                dicInitFlag = true;
                bitDataIndex += lzwCodeSize;
                prevEntry = null;
                continue;

            } else if (key == finishCode) {
                // Exit
                Debug.LogWarning ("early stop code. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
                break;

            } else if (dic.ContainsKey (key)) {
                // Output from dictionary
                entry = dic[key];

            } else if (key >= dic.Count) {
                if (prevEntry != null) {
                    // Output from estimation
                    entry = prevEntry + prevEntry[0];

                } else {
                    Debug.LogWarning ("It is strange that come here. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
                    bitDataIndex += lzwCodeSize;
                    continue;
                }

            } else {
                Debug.LogWarning ("It is strange that come here. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
                bitDataIndex += lzwCodeSize;
                continue;
            }

            // Output
            // Take out 8 bits from the string.
            var temp = Encoding.Unicode.GetBytes (entry);
            for (int i = 0; i < temp.Length; i++) {
                if (i % 2 == 0) {
                    output[outputAddIndex] = temp[i];
                    outputAddIndex++;
                }
            }

            if (outputAddIndex >= needDataSize) {
                // Exit
                break;
            }

            if (prevEntry != null) {
                // Add to dictionary
                dic.Add (dic.Count, prevEntry + entry[0]);
            }

            prevEntry = entry;

            bitDataIndex += lzwCodeSize;

            if (lzwCodeSize == 3 && dic.Count >= 8) {
                lzwCodeSize = 4;
            } else if (lzwCodeSize == 4 && dic.Count >= 16) {
                lzwCodeSize = 5;
            } else if (lzwCodeSize == 5 && dic.Count >= 32) {
                lzwCodeSize = 6;
            } else if (lzwCodeSize == 6 && dic.Count >= 64) {
                lzwCodeSize = 7;
            } else if (lzwCodeSize == 7 && dic.Count >= 128) {
                lzwCodeSize = 8;
            } else if (lzwCodeSize == 8 && dic.Count >= 256) {
                lzwCodeSize = 9;
            } else if (lzwCodeSize == 9 && dic.Count >= 512) {
                lzwCodeSize = 10;
            } else if (lzwCodeSize == 10 && dic.Count >= 1024) {
                lzwCodeSize = 11;
            } else if (lzwCodeSize == 11 && dic.Count >= 2048) {
                lzwCodeSize = 12;
            } else if (lzwCodeSize == 12 && dic.Count >= 4096) {
                int nextKey = bitData.GetNumeral (bitDataIndex, lzwCodeSize);
                if (nextKey != clearCode) {
                    dicInitFlag = true;
                }
            }
        }

        return output;
    }

    /// <summary>
    /// Initialize dictionary
    /// </summary>
    /// <param name="dic">Dictionary</param>
    /// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
    /// <param name="lzwCodeSize">out LZW code size</param>
    /// <param name="clearCode">out Clear code</param>
    /// <param name="finishCode">out Finish code</param>
    static void InitDictionary (Dictionary<int, string> dic, int lzwMinimumCodeSize, out int lzwCodeSize, out int clearCode, out int finishCode)
    {
        int dicLength = (int) Math.Pow (2, lzwMinimumCodeSize);

        clearCode = dicLength;
        finishCode = clearCode + 1;

        dic.Clear ();

        for (int i = 0; i < dicLength + 2; i++) {
            dic.Add (i, ((char) i).ToString ());
        }

        lzwCodeSize = lzwMinimumCodeSize + 1;
    }

    /// <summary>
    /// Sort interlace GIF data
    /// </summary>
    /// <param name="decodedData">Decoded GIF data</param>
    /// <param name="xNum">Pixel number of horizontal row</param>
    /// <returns>Sorted data</returns>
    static byte[] SortInterlaceGifData (byte[] decodedData, int xNum)
    {
        int rowNo = 0;
        int dataIndex = 0;
        var newArr = new byte[decodedData.Length];
        // Every 8th. row, starting with row 0.
        for (int i = 0; i < newArr.Length; i++) {
            if (rowNo % 8 == 0) {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0) {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 8th. row, starting with row 4.
        for (int i = 0; i < newArr.Length; i++) {
            if (rowNo % 8 == 4) {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0) {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 4th. row, starting with row 2.
        for (int i = 0; i < newArr.Length; i++) {
            if (rowNo % 4 == 2) {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0) {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 2nd. row, starting with row 1.
        for (int i = 0; i < newArr.Length; i++) {
            if (rowNo % 8 != 0 && rowNo % 8 != 4 && rowNo % 4 != 2) {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0) {
                rowNo++;
            }
        }

        return newArr;
    }

    #endregion
}

/// <summary>
/// Extension methods class
/// </summary>
public static class Extension
{
    /// <summary>
    /// Convert BitArray to int (Specifies the start index and bit length)
    /// </summary>
    /// <param name="startIndex">Start index</param>
    /// <param name="bitLength">Bit length</param>
    /// <returns>Converted int</returns>
    public static int GetNumeral (this BitArray array, int startIndex, int bitLength)
    {
        var newArray = new BitArray (bitLength);

        for (int i = 0; i < bitLength; i++) {
            if (array.Length <= startIndex + i) {
                newArray[i] = false;

            } else {
                bool bit = array.Get (startIndex + i);
                newArray[i] = bit;
            }
        }

        return newArray.ToNumeral ();
    }

    /// <summary>
    /// Convert BitArray to int
    /// </summary>
    /// <returns>Converted int</returns>
    public static int ToNumeral (this BitArray array)
    {
        if (array == null) {
            Debug.LogError ("array is nothing.");
            return 0;
        }
        if (array.Length > 32) {
            Debug.LogError ("must be at most 32 bits long.");
            return 0;
        }

        var result = new int[1];
        array.CopyTo (result, 0);
        return result[0];
    }
}
