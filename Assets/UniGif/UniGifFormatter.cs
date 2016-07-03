/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class UniGif
{
    /// <summary>
    /// Set GIF data
    /// </summary>
    /// <param name="gifBytes">GIF byte data</param>
    /// <param name="gifData">ref GIF data</param>
    /// <param name="debugLog">Debug log flag</param>
    /// <returns>Result</returns>
    private static bool SetGifData(byte[] gifBytes, ref GifData gifData, bool debugLog)
    {
        if (debugLog)
        {
            Debug.Log("SetGifData Start.");
        }

        if (gifBytes == null || gifBytes.Length <= 0)
        {
            Debug.LogError("bytes is nothing.");
            return false;
        }

        int byteIndex = 0;

        if (SetGifHeader(gifBytes, ref byteIndex, ref gifData) == false)
        {
            Debug.LogError("GIF header set error.");
            return false;
        }

        if (SetGifBlock(gifBytes, ref byteIndex, ref gifData) == false)
        {
            Debug.LogError("GIF block set error.");
            return false;
        }

        if (debugLog)
        {
            gifData.Dump();
            Debug.Log("SetGifData Finish.");
        }
        return true;
    }

    private static bool SetGifHeader(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        // Signature(3 Bytes)
        // 0x47 0x49 0x46 (GIF)
        if (gifBytes[0] != 'G' || gifBytes[1] != 'I' || gifBytes[2] != 'F')
        {
            Debug.LogError("This is not GIF image.");
            return false;
        }
        gifData.m_sig0 = gifBytes[0];
        gifData.m_sig1 = gifBytes[1];
        gifData.m_sig2 = gifBytes[2];

        // Version(3 Bytes)
        // 0x38 0x37 0x61 (87a) or 0x38 0x39 0x61 (89a)
        if ((gifBytes[3] != '8' || gifBytes[4] != '7' || gifBytes[5] != 'a') &&
            (gifBytes[3] != '8' || gifBytes[4] != '9' || gifBytes[5] != 'a'))
        {
            Debug.LogError("GIF version error.\nSupported only GIF87a or GIF89a.");
            return false;
        }
        gifData.m_ver0 = gifBytes[3];
        gifData.m_ver1 = gifBytes[4];
        gifData.m_ver2 = gifBytes[5];

        // Logical Screen Width(2 Bytes)
        gifData.m_logicalScreenWidth = BitConverter.ToUInt16(gifBytes, 6);

        // Logical Screen Height(2 Bytes)
        gifData.m_logicalScreenHeight = BitConverter.ToUInt16(gifBytes, 8);

        // 1 Byte
        {
            // Global Color Table Flag(1 Bit)
            gifData.m_globalColorTableFlag = (gifBytes[10] & 128) == 128; // 0b10000000

            // Color Resolution(3 Bits)
            switch (gifBytes[10] & 112)
            {
                case 112: // 0b01110000
                    gifData.m_colorResolution = 8;
                    break;
                case 96: // 0b01100000
                    gifData.m_colorResolution = 7;
                    break;
                case 80: // 0b01010000
                    gifData.m_colorResolution = 6;
                    break;
                case 64: // 0b01000000
                    gifData.m_colorResolution = 5;
                    break;
                case 48: // 0b00110000
                    gifData.m_colorResolution = 4;
                    break;
                case 32: // 0b00100000
                    gifData.m_colorResolution = 3;
                    break;
                case 16: // 0b00010000
                    gifData.m_colorResolution = 2;
                    break;
                default:
                    gifData.m_colorResolution = 1;
                    break;
            }

            // Sort Flag(1 Bit)
            gifData.m_sortFlag = (gifBytes[10] & 8) == 8; // 0b00001000

            // Size of Global Color Table(3 Bits)
            int val = (gifBytes[10] & 7) + 1;
            gifData.m_sizeOfGlobalColorTable = (int)Math.Pow(2, val);
        }

        // Background Color Index(1 Byte)
        gifData.m_bgColorIndex = gifBytes[11];

        // Pixel Aspect Ratio(1 Byte)
        gifData.m_pixelAspectRatio = gifBytes[12];

        byteIndex = 13;
        if (gifData.m_globalColorTableFlag)
        {
            // Global Color Table(0～255×3 Bytes)
            gifData.m_globalColorTable = new List<byte[]>();
            for (int i = byteIndex; i < byteIndex + (gifData.m_sizeOfGlobalColorTable * 3); i += 3)
            {
                gifData.m_globalColorTable.Add(new byte[] { gifBytes[i], gifBytes[i + 1], gifBytes[i + 2] });
            }
            byteIndex = byteIndex + (gifData.m_sizeOfGlobalColorTable * 3);
        }

        return true;
    }

    private static bool SetGifBlock(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        try
        {
            int lastIndex = 0;
            while (true)
            {
                int nowIndex = byteIndex;

                if (gifBytes[nowIndex] == 0x2c)
                {
                    // Image Block(0x2c)
                    SetImageBlock(gifBytes, ref byteIndex, ref gifData);

                }
                else if (gifBytes[nowIndex] == 0x21)
                {
                    // Extension
                    switch (gifBytes[nowIndex + 1])
                    {
                        case 0xf9:
                            // Graphic Control Extension(0x21 0xf9)
                            SetGraphicControlExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0xfe:
                            // Comment Extension(0x21 0xfe)
                            SetCommentExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0x01:
                            // Plain Text Extension(0x21 0x01)
                            SetPlainTextExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0xff:
                            // Application Extension(0x21 0xff)
                            SetApplicationExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        default:
                            break;
                    }
                }
                else if (gifBytes[nowIndex] == 0x3b)
                {
                    // Trailer(1 Byte)
                    gifData.m_trailer = gifBytes[byteIndex];
                    byteIndex++;
                    break;
                }

                if (lastIndex == nowIndex)
                {
                    Debug.LogError("Infinite loop error.");
                    return false;
                }

                lastIndex = nowIndex;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            return false;
        }

        return true;
    }

    private static void SetImageBlock(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        ImageBlock ib = new ImageBlock();

        // Image Separator(1 Byte)
        // 0x2c
        ib.m_imageSeparator = gifBytes[byteIndex];
        byteIndex++;

        // Image Left Position(2 Bytes)
        ib.m_imageLeftPosition = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Image Top Position(2 Bytes)
        ib.m_imageTopPosition = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Image Width(2 Bytes)
        ib.m_imageWidth = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Image Height(2 Bytes)
        ib.m_imageHeight = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // 1 Byte
        {
            // Local Color Table Flag(1 Bit)
            ib.m_localColorTableFlag = (gifBytes[byteIndex] & 128) == 128; // 0b10000000

            // Interlace Flag(1 Bit)
            ib.m_interlaceFlag = (gifBytes[byteIndex] & 64) == 64; // 0b01000000

            // Sort Flag(1 Bit)
            ib.m_sortFlag = (gifBytes[byteIndex] & 32) == 32; // 0b00100000

            // Reserved(2 Bits)
            // Unused

            // Size of Local Color Table(3 Bits)
            int val = (gifBytes[byteIndex] & 7) + 1;
            ib.m_sizeOfLocalColorTable = (int)Math.Pow(2, val);

            byteIndex++;
        }

        if (ib.m_localColorTableFlag)
        {
            // Local Color Table(0～255×3 Bytes)
            ib.m_localColorTable = new List<byte[]>();
            for (int i = byteIndex; i < byteIndex + (ib.m_sizeOfLocalColorTable * 3); i += 3)
            {
                ib.m_localColorTable.Add(new byte[] { gifBytes[i], gifBytes[i + 1], gifBytes[i + 2] });
            }
            byteIndex = byteIndex + (ib.m_sizeOfLocalColorTable * 3);
        }

        // LZW Minimum Code Size(1 Byte)
        ib.m_lzwMinimumCodeSize = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Image Data List
        while (true)
        {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var imageDataBlock = new ImageBlock.ImageDataBlock();
            imageDataBlock.m_blockSize = blockSize;

            // Image Data(? Bytes)
            imageDataBlock.m_imageData = new byte[imageDataBlock.m_blockSize];
            for (int i = 0; i < imageDataBlock.m_imageData.Length; i++)
            {
                imageDataBlock.m_imageData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (ib.m_imageDataList == null)
            {
                ib.m_imageDataList = new List<ImageBlock.ImageDataBlock>();
            }
            ib.m_imageDataList.Add(imageDataBlock);
        }

        if (gifData.m_imageBlockList == null)
        {
            gifData.m_imageBlockList = new List<ImageBlock>();
        }
        gifData.m_imageBlockList.Add(ib);
    }

    private static void SetGraphicControlExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        GraphicControlExtension gcEx = new GraphicControlExtension();

        // Extension Introducer(1 Byte)
        // 0x21
        gcEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Graphic Control Label(1 Byte)
        // 0xf9
        gcEx.m_graphicControlLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x04
        gcEx.m_blockSize = gifBytes[byteIndex];
        byteIndex++;

        // 1 Byte
        {
            // Reserved(3 Bits)
            // Unused

            // Disposal Mothod(3 Bits)
            // 0 (No disposal specified)
            // 1 (Do not dispose)
            // 2 (Restore to background color)
            // 3 (Restore to previous)
            switch (gifBytes[byteIndex] & 28)
            { // 0b00011100
                case 4:     // 0b00000100
                    gcEx.m_disposalMethod = 1;
                    break;
                case 8:     // 0b00001000
                    gcEx.m_disposalMethod = 2;
                    break;
                case 12:    // 0b00001100
                    gcEx.m_disposalMethod = 3;
                    break;
                default:
                    gcEx.m_disposalMethod = 0;
                    break;
            }

            // User Input Flag(1 Bit)
            // Unknown

            // Transparent Color Flag(1 Bit)
            gcEx.m_transparentColorFlag = (gifBytes[byteIndex] & 1) == 1; // 0b00000001

            byteIndex++;
        }

        // Delay Time(2 Bytes)
        gcEx.m_delayTime = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Transparent Color Index(1 Byte)
        gcEx.m_transparentColorIndex = gifBytes[byteIndex];
        byteIndex++;

        // Block Terminator(1 Byte)
        gcEx.m_blockTerminator = gifBytes[byteIndex];
        byteIndex++;

        if (gifData.m_graphicCtrlExList == null)
        {
            gifData.m_graphicCtrlExList = new List<GraphicControlExtension>();
        }
        gifData.m_graphicCtrlExList.Add(gcEx);
    }

    private static void SetCommentExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        CommentExtension commentEx = new CommentExtension();

        // Extension Introducer(1 Byte)
        // 0x21
        commentEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Comment Label(1 Byte)
        // 0xfe
        commentEx.m_commentLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Comment Data List
        while (true)
        {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var commentDataBlock = new CommentExtension.CommentDataBlock();
            commentDataBlock.m_blockSize = blockSize;

            // Comment Data(n Byte)
            commentDataBlock.m_commentData = new byte[commentDataBlock.m_blockSize];
            for (int i = 0; i < commentDataBlock.m_commentData.Length; i++)
            {
                commentDataBlock.m_commentData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (commentEx.m_commentDataList == null)
            {
                commentEx.m_commentDataList = new List<CommentExtension.CommentDataBlock>();
            }
            commentEx.m_commentDataList.Add(commentDataBlock);
        }

        if (gifData.m_commentExList == null)
        {
            gifData.m_commentExList = new List<CommentExtension>();
        }
        gifData.m_commentExList.Add(commentEx);
    }

    private static void SetPlainTextExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        PlainTextExtension plainTxtEx = new PlainTextExtension();

        // Extension Introducer(1 Byte)
        // 0x21
        plainTxtEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Plain Text Label(1 Byte)
        // 0x01
        plainTxtEx.m_plainTextLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x0c
        plainTxtEx.m_blockSize = gifBytes[byteIndex];
        byteIndex++;

        // Text Grid Left Position(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Text Grid Top Position(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Text Grid Width(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Text Grid Height(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Character Cell Width(1 Bytes)
        // Not supported
        byteIndex++;

        // Character Cell Height(1 Bytes)
        // Not supported
        byteIndex++;

        // Text Foreground Color Index(1 Bytes)
        // Not supported
        byteIndex++;

        // Text Background Color Index(1 Bytes)
        // Not supported
        byteIndex++;

        // Block Size & Plain Text Data List
        while (true)
        {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var plainTextDataBlock = new PlainTextExtension.PlainTextDataBlock();
            plainTextDataBlock.m_blockSize = blockSize;

            // Plain Text Data(n Byte)
            plainTextDataBlock.m_plainTextData = new byte[plainTextDataBlock.m_blockSize];
            for (int i = 0; i < plainTextDataBlock.m_plainTextData.Length; i++)
            {
                plainTextDataBlock.m_plainTextData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (plainTxtEx.m_plainTextDataList == null)
            {
                plainTxtEx.m_plainTextDataList = new List<PlainTextExtension.PlainTextDataBlock>();
            }
            plainTxtEx.m_plainTextDataList.Add(plainTextDataBlock);
        }

        if (gifData.m_plainTextExList == null)
        {
            gifData.m_plainTextExList = new List<PlainTextExtension>();
        }
        gifData.m_plainTextExList.Add(plainTxtEx);
    }

    private static void SetApplicationExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        // Extension Introducer(1 Byte)
        // 0x21
        gifData.m_appEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Extension Label(1 Byte)
        // 0xff
        gifData.m_appEx.m_extensionLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x0b
        gifData.m_appEx.m_blockSize = gifBytes[byteIndex];
        byteIndex++;

        // Application Identifier(8 Bytes)
        gifData.m_appEx.m_appId1 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId2 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId3 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId4 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId5 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId6 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId7 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId8 = gifBytes[byteIndex];
        byteIndex++;

        // Application Authentication Code(3 Bytes)
        gifData.m_appEx.m_appAuthCode1 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appAuthCode2 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appAuthCode3 = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Application Data List
        while (true)
        {
            // Block Size (1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var appDataBlock = new ApplicationExtension.ApplicationDataBlock();
            appDataBlock.m_blockSize = blockSize;

            // Application Data(n Byte)
            appDataBlock.m_applicationData = new byte[appDataBlock.m_blockSize];
            for (int i = 0; i < appDataBlock.m_applicationData.Length; i++)
            {
                appDataBlock.m_applicationData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (gifData.m_appEx.m_appDataList == null)
            {
                gifData.m_appEx.m_appDataList = new List<ApplicationExtension.ApplicationDataBlock>();
            }
            gifData.m_appEx.m_appDataList.Add(appDataBlock);
        }
    }
}
