/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static partial class UniGif
{
    /// <summary>
    /// Decode to textures from GIF data
    /// </summary>
    /// <param name="gifData">GIF data</param>
    /// <param name="callback">Callback method(param is GIF texture list)</param>
    /// <param name="filterMode">Textures filter mode</param>
    /// <param name="wrapMode">Textures wrap mode</param>
    /// <returns>IEnumerator</returns>
    private static IEnumerator DecodeTextureCoroutine(GifData gifData, Action<List<GifTexture>> callback, FilterMode filterMode, TextureWrapMode wrapMode)
    {
        if (gifData.m_imageBlockList == null || gifData.m_imageBlockList.Count < 1)
        {
            yield break;
        }

        List<GifTexture> gifTexList = new List<GifTexture>();

        Color32? bgColor = GetGlobalBgColor(gifData);

        // Disposal Method
        // 0 (No disposal specified)
        // 1 (Do not dispose)
        // 2 (Restore to background color)
        // 3 (Restore to previous)
        ushort disposalMethod = 0;

        int imgBlockIndex = 0;

        for (int i = 0; i < gifData.m_imageBlockList.Count; i++)
        {
            byte[] decodedData = GetDecodedData(gifData.m_imageBlockList[i]);

            List<byte[]> colorTable = GetColorTable(gifData, gifData.m_imageBlockList[i], ref bgColor);

            GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, imgBlockIndex);

            int transparentIndex = GetTransparentIndex(graphicCtrlEx);

            yield return 0;

            bool useBeforeTex = false;
            Texture2D tex = CreateTexture2D(gifData, gifTexList, imgBlockIndex, disposalMethod, filterMode, wrapMode, ref useBeforeTex);

            // Set pixel data
            int dataIndex = 0;
            // Reverse set pixels. because GIF data starts from the top left.
            for (int y = tex.height - 1; y >= 0; y--)
            {
                SetTexturePixelRow(tex, y, gifData.m_imageBlockList[i], decodedData, ref dataIndex, colorTable, bgColor, transparentIndex, useBeforeTex);
            }
            tex.Apply();

            yield return 0;

            float delaySec = GetDelaySec(graphicCtrlEx);

            // Add to GIF texture list
            gifTexList.Add(new GifTexture(tex, delaySec));

            disposalMethod = GetDisposalMethod(graphicCtrlEx);

            imgBlockIndex++;
        }

        if (callback != null)
        {
            callback(gifTexList);
        }

        yield break;
    }

    #region Call from DecodeTexture methods

    /// <summary>
    /// Get background color from global color table
    /// </summary>
    private static Color32? GetGlobalBgColor(GifData gifData)
    {
        Color32? bgColor = null;
        if (gifData.m_globalColorTableFlag)
        {
            // Set background color from global color table
            byte[] bgRgb = gifData.m_globalColorTable[gifData.m_bgColorIndex];
            bgColor = new Color32(bgRgb[0], bgRgb[1], bgRgb[2], 255);
        }
        return bgColor;
    }

    /// <summary>
    /// Get decoded image data from ImageBlock
    /// </summary>
    private static byte[] GetDecodedData(ImageBlock imgBlock)
    {
        // Combine LZW compressed data
        List<byte> lzwData = new List<byte>();
        for (int i = 0; i < imgBlock.m_imageDataList.Count; i++)
        {
            for (int k = 0; k < imgBlock.m_imageDataList[i].m_imageData.Length; k++)
            {
                lzwData.Add(imgBlock.m_imageDataList[i].m_imageData[k]);
            }
        }

        // LZW decode
        int needDataSize = imgBlock.m_imageHeight * imgBlock.m_imageWidth;
        byte[] decodedData = DecodeGifLZW(lzwData, imgBlock.m_lzwMinimumCodeSize, needDataSize);

        // Sort interlace GIF
        if (imgBlock.m_interlaceFlag)
        {
            decodedData = SortInterlaceGifData(decodedData, imgBlock.m_imageWidth);
        }
        return decodedData;
    }

    /// <summary>
    /// Get color table (local or global)
    /// </summary>
    private static List<byte[]> GetColorTable(GifData gifData, ImageBlock imgBlock, ref Color32? bgColor)
    {
        List<byte[]> colorTable = imgBlock.m_localColorTableFlag ? imgBlock.m_localColorTable : gifData.m_globalColorTable;

        if (imgBlock.m_localColorTableFlag)
        {
            // Set background color from local color table
            byte[] bgRgb = imgBlock.m_localColorTable[gifData.m_bgColorIndex];
            bgColor = new Color32(bgRgb[0], bgRgb[1], bgRgb[2], 255);
        }

        return colorTable;
    }

    /// <summary>
    /// Get GraphicControlExtension from GifData
    /// </summary>
    private static GraphicControlExtension? GetGraphicCtrlExt(GifData gifData, int imgBlockIndex)
    {
        if (gifData.m_graphicCtrlExList != null && gifData.m_graphicCtrlExList.Count > imgBlockIndex)
        {
            return gifData.m_graphicCtrlExList[imgBlockIndex];
        }
        return null;
    }

    /// <summary>
    /// Get transparent color index from GraphicControlExtension
    /// </summary>
    private static int GetTransparentIndex(GraphicControlExtension? graphicCtrlEx)
    {
        int transparentIndex = -1;
        if (graphicCtrlEx != null && graphicCtrlEx.Value.m_transparentColorFlag)
        {
            transparentIndex = graphicCtrlEx.Value.m_transparentColorIndex;
        }
        return transparentIndex;
    }

    /// <summary>
    /// Get delay seconds from GraphicControlExtension
    /// </summary>
    private static float GetDelaySec(GraphicControlExtension? graphicCtrlEx)
    {
        // Get delay sec from GraphicControlExtension
        float delaySec = graphicCtrlEx != null ? graphicCtrlEx.Value.m_delayTime / 100f : (1f / 60f);
        if (delaySec < 0f)
        {
            delaySec = (1f / 60f);
        }
        return delaySec;
    }

    /// <summary>
    /// Get disposal method from GraphicControlExtension
    /// </summary>
    private static ushort GetDisposalMethod(GraphicControlExtension? graphicCtrlEx)
    {
        return graphicCtrlEx != null ? graphicCtrlEx.Value.m_disposalMethod : (ushort)2;
    }

    /// <summary>
    /// Create Texture2D object and initial settings
    /// </summary>
    private static Texture2D CreateTexture2D(GifData gifData, List<GifTexture> gifTexList, int imgBlockIndex, ushort disposalMethod, FilterMode filterMode, TextureWrapMode wrapMode, ref bool useBeforeTex)
    {
        // Create texture
        Texture2D tex = new Texture2D(gifData.m_logicalScreenWidth, gifData.m_logicalScreenHeight, TextureFormat.ARGB32, false);
        tex.filterMode = filterMode;
        tex.wrapMode = wrapMode;

        // Check dispose
        useBeforeTex = false;
        int beforeIndex = -1;
        if (imgBlockIndex > 0 && disposalMethod == 0 || disposalMethod == 1)
        {
            // before 1
            beforeIndex = imgBlockIndex - 1;
        }
        else if (imgBlockIndex > 1 && disposalMethod == 3)
        {
            // before 2
            beforeIndex = imgBlockIndex - 2;
        }
        if (beforeIndex >= 0)
        {
            // Do not dispose
            useBeforeTex = true;
            Color32[] pix = gifTexList[beforeIndex].m_texture2d.GetPixels32();
            tex.SetPixels32(pix);
            tex.Apply();
        }

        return tex;
    }

    /// <summary>
    /// Set texture pixel row
    /// </summary>
    private static void SetTexturePixelRow(Texture2D tex, int y, ImageBlock imgBlock, byte[] decodedData, ref int dataIndex, List<byte[]> colorTable, Color32? bgColor, int transparentIndex, bool useBeforeTex)
    {
        // Row no (0~)
        int row = tex.height - 1 - y;

        for (int x = 0; x < tex.width; x++)
        {
            // Line no (0~)
            int line = x;

            // Out of image blocks
            if (row < imgBlock.m_imageTopPosition ||
                row >= imgBlock.m_imageTopPosition + imgBlock.m_imageHeight ||
                line < imgBlock.m_imageLeftPosition ||
                line >= imgBlock.m_imageLeftPosition + imgBlock.m_imageWidth)
            {
                if (useBeforeTex == false && bgColor != null)
                {
                    tex.SetPixel(x, y, bgColor.Value);
                }
                continue;
            }

            // Out of decoded data
            if (dataIndex >= decodedData.Length)
            {
                tex.SetPixel(x, y, Color.black);
                // Debug.LogError ("dataIndex exceeded the size of decodedData. dataIndex:" + dataIndex + " decodedData.Length:" + decodedData.Length + " y:" + y + " x:" + x);
                dataIndex++;
                continue;
            }

            // Get pixel color from color table
            byte colorIndex = decodedData[dataIndex];
            if (colorTable == null || colorTable.Count <= colorIndex)
            {
                Debug.LogError("colorIndex exceeded the size of colorTable. colorTable.Count:" + colorTable.Count + " colorIndex:" + colorIndex);
                dataIndex++;
                continue;
            }
            byte[] rgb = colorTable[colorIndex];

            // Set alpha
            byte alpha = transparentIndex >= 0 && transparentIndex == colorIndex ? (byte)0 : (byte)255;

            if (alpha != 0 || useBeforeTex == false)
            {
                Color32 col = new Color32(rgb[0], rgb[1], rgb[2], alpha);
                tex.SetPixel(x, y, col);
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
    private static byte[] DecodeGifLZW(List<byte> compData, int lzwMinimumCodeSize, int needDataSize)
    {
        int clearCode = 0;
        int finishCode = 0;

        // Initialize dictionary
        Dictionary<int, string> dic = new Dictionary<int, string>();
        int lzwCodeSize = 0;
        InitDictionary(dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);

        // Convert to bit array
        byte[] compDataArr = compData.ToArray();
        var bitData = new BitArray(compDataArr);

        byte[] output = new byte[needDataSize];
        int outputAddIndex = 0;

        string prevEntry = null;

        bool dicInitFlag = false;

        int bitDataIndex = 0;

        // LZW decode loop
        while (bitDataIndex < bitData.Length)
        {
            if (dicInitFlag)
            {
                InitDictionary(dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);
                dicInitFlag = false;
            }

            int key = bitData.GetNumeral(bitDataIndex, lzwCodeSize);

            string entry = null;

            if (key == clearCode)
            {
                // Clear (Initialize dictionary)
                dicInitFlag = true;
                bitDataIndex += lzwCodeSize;
                prevEntry = null;
                continue;
            }
            else if (key == finishCode)
            {
                // Exit
                Debug.LogWarning("early stop code. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
                break;
            }
            else if (dic.ContainsKey(key))
            {
                // Output from dictionary
                entry = dic[key];
            }
            else if (key >= dic.Count)
            {
                if (prevEntry != null)
                {
                    // Output from estimation
                    entry = prevEntry + prevEntry[0];
                }
                else
                {
                    Debug.LogWarning("It is strange that come here. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
                    bitDataIndex += lzwCodeSize;
                    continue;
                }
            }
            else
            {
                Debug.LogWarning("It is strange that come here. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
                bitDataIndex += lzwCodeSize;
                continue;
            }

            // Output
            // Take out 8 bits from the string.
            byte[] temp = Encoding.Unicode.GetBytes(entry);
            for (int i = 0; i < temp.Length; i++)
            {
                if (i % 2 == 0)
                {
                    output[outputAddIndex] = temp[i];
                    outputAddIndex++;
                }
            }

            if (outputAddIndex >= needDataSize)
            {
                // Exit
                break;
            }

            if (prevEntry != null)
            {
                // Add to dictionary
                dic.Add(dic.Count, prevEntry + entry[0]);
            }

            prevEntry = entry;

            bitDataIndex += lzwCodeSize;

            if (lzwCodeSize == 3 && dic.Count >= 8)
            {
                lzwCodeSize = 4;
            }
            else if (lzwCodeSize == 4 && dic.Count >= 16)
            {
                lzwCodeSize = 5;
            }
            else if (lzwCodeSize == 5 && dic.Count >= 32)
            {
                lzwCodeSize = 6;
            }
            else if (lzwCodeSize == 6 && dic.Count >= 64)
            {
                lzwCodeSize = 7;
            }
            else if (lzwCodeSize == 7 && dic.Count >= 128)
            {
                lzwCodeSize = 8;
            }
            else if (lzwCodeSize == 8 && dic.Count >= 256)
            {
                lzwCodeSize = 9;
            }
            else if (lzwCodeSize == 9 && dic.Count >= 512)
            {
                lzwCodeSize = 10;
            }
            else if (lzwCodeSize == 10 && dic.Count >= 1024)
            {
                lzwCodeSize = 11;
            }
            else if (lzwCodeSize == 11 && dic.Count >= 2048)
            {
                lzwCodeSize = 12;
            }
            else if (lzwCodeSize == 12 && dic.Count >= 4096)
            {
                int nextKey = bitData.GetNumeral(bitDataIndex, lzwCodeSize);
                if (nextKey != clearCode)
                {
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
    private static void InitDictionary(Dictionary<int, string> dic, int lzwMinimumCodeSize, out int lzwCodeSize, out int clearCode, out int finishCode)
    {
        int dicLength = (int)Math.Pow(2, lzwMinimumCodeSize);

        clearCode = dicLength;
        finishCode = clearCode + 1;

        dic.Clear();

        for (int i = 0; i < dicLength + 2; i++)
        {
            dic.Add(i, ((char)i).ToString());
        }

        lzwCodeSize = lzwMinimumCodeSize + 1;
    }

    /// <summary>
    /// Sort interlace GIF data
    /// </summary>
    /// <param name="decodedData">Decoded GIF data</param>
    /// <param name="xNum">Pixel number of horizontal row</param>
    /// <returns>Sorted data</returns>
    private static byte[] SortInterlaceGifData(byte[] decodedData, int xNum)
    {
        int rowNo = 0;
        int dataIndex = 0;
        var newArr = new byte[decodedData.Length];
        // Every 8th. row, starting with row 0.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 == 0)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 8th. row, starting with row 4.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 == 4)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 4th. row, starting with row 2.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 4 == 2)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 2nd. row, starting with row 1.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 != 0 && rowNo % 8 != 4 && rowNo % 4 != 2)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }

        return newArr;
    }

    #endregion
}