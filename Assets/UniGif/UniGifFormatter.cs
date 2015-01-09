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

public static partial class UniGif
{
    /// <summary>
    /// Set GIF data
    /// </summary>
    /// <param name="gifBytes">GIF byte data</param>
    /// <param name="gifData">ref GIF data</param>
    /// <param name="debugLog">Debug log flag</param>
    /// <returns>Result</returns>
    static bool SetGifData (byte[] gifBytes, ref GifData gifData, bool debugLog)
    {
        if (debugLog) {
            Debug.Log ("SetGifData Start.");
        }

        if (gifBytes == null || gifBytes.Length <= 0) {
            Debug.LogError ("bytes is nothing.");
            return false;
        }

        int byteIndex = 0;

        if (SetGifHeader (gifBytes, ref byteIndex, ref gifData) == false) {
            Debug.LogError ("GIF header set error.");
            return false;
        }

        if (SetGifBlock (gifBytes, ref byteIndex, ref gifData) == false) {
            Debug.LogError ("GIF block set error.");
            return false;
        }

        if (debugLog) {
            gifData.Dump ();
            Debug.Log ("SetGifData Finish.");
        }
        return true;
    }

    static bool SetGifHeader (byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        // Signature(3 Bytes)
        // 0x47 0x49 0x46 (GIF)
        if (gifBytes[0] != 'G' || gifBytes[1] != 'I' || gifBytes[2] != 'F') {
            Debug.LogError ("This is not GIF image.");
            return false;
        }
        gifData.sig0 = gifBytes[0];
        gifData.sig1 = gifBytes[1];
        gifData.sig2 = gifBytes[2];

        // Version(3 Bytes)
        // 0x38 0x37 0x61 (87a) or 0x38 0x39 0x61 (89a)
        if ((gifBytes[3] != '8' || gifBytes[4] != '7' || gifBytes[5] != 'a') &&
            (gifBytes[3] != '8' || gifBytes[4] != '9' || gifBytes[5] != 'a')) {
            Debug.LogError ("GIF version error.\nSupported only GIF87a or GIF89a.");
            return false;
        }
        gifData.ver0 = gifBytes[3];
        gifData.ver1 = gifBytes[4];
        gifData.ver2 = gifBytes[5];

        // Logical Screen Width(2 Bytes)
        gifData.logicalScreenWidth = BitConverter.ToUInt16 (gifBytes, 6);

        // Logical Screen Height(2 Bytes)
        gifData.logicalScreenHeight = BitConverter.ToUInt16 (gifBytes, 8);

        // 1 Byte
        {
            // Global Color Table Flag(1 Bit)
            gifData.globalColorTableFlag = (gifBytes[10] & 128) == 128; // 0b10000000

            // Color Resolution(3 Bits)
            switch (gifBytes[10] & 112) {
                case 112: // 0b01110000
                    gifData.colorResolution = 8;
                    break;
                case 96: // 0b01100000
                    gifData.colorResolution = 7;
                    break;
                case 80: // 0b01010000
                    gifData.colorResolution = 6;
                    break;
                case 64: // 0b01000000
                    gifData.colorResolution = 5;
                    break;
                case 48: // 0b00110000
                    gifData.colorResolution = 4;
                    break;
                case 32: // 0b00100000
                    gifData.colorResolution = 3;
                    break;
                case 16: // 0b00010000
                    gifData.colorResolution = 2;
                    break;
                default:
                    gifData.colorResolution = 1;
                    break;
            }

            // Sort Flag(1 Bit)
            gifData.sortFlag = (gifBytes[10] & 8) == 8; // 0b00001000

            // Size of Global Color Table(3 Bits)
            int val = (gifBytes[10] & 7) + 1;
            gifData.sizeOfGlobalColorTable = (int) Math.Pow (2, val);
        }

        // Background Color Index(1 Byte)
        gifData.bgColorIndex = gifBytes[11];

        // Pixel Aspect Ratio(1 Byte)
        gifData.pixelAspectRatio = gifBytes[12];

        byteIndex = 13;
        if (gifData.globalColorTableFlag) {
            // Global Color Table(0～255×3 Bytes)
            gifData.globalColorTable = new List<byte[]> ();
            for (int i = byteIndex; i < byteIndex + (gifData.sizeOfGlobalColorTable * 3); i += 3) {
                gifData.globalColorTable.Add (new byte[] { gifBytes[i], gifBytes[i + 1], gifBytes[i + 2] });
            }
            byteIndex = byteIndex + (gifData.sizeOfGlobalColorTable * 3);
        }

        return true;
    }

    static bool SetGifBlock (byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        try {
            int lastIndex = 0;
            while (true) {
                int nowIndex = byteIndex;

                if (gifBytes[nowIndex] == 0x2c) {
                    // Image Block(0x2c)
                    SetImageBlock (gifBytes, ref byteIndex, ref gifData);

                } else if (gifBytes[nowIndex] == 0x21) {
                    // Extension
                    switch (gifBytes[nowIndex + 1]) {
                        case 0xf9:
                            // Graphic Control Extension(0x21 0xf9)
                            SetGraphicControlExtension (gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0xfe:
                            // Comment Extension(0x21 0xfe)
                            SetCommentExtension (gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0x01:
                            // Plain Text Extension(0x21 0x01)
                            SetPlainTextExtension (gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0xff:
                            // Application Extension(0x21 0xff)
                            SetApplicationExtension (gifBytes, ref byteIndex, ref gifData);
                            break;
                        default:
                            break;
                    }
                } else if (gifBytes[nowIndex] == 0x3b) {
                    // Trailer(1 Byte)
                    gifData.trailer = gifBytes[byteIndex];
                    byteIndex++;
                    break;
                }

                if (lastIndex == nowIndex) {
                    Debug.LogError ("Infinite loop error.");
                    return false;
                }

                lastIndex = nowIndex;
            }
        } catch (Exception ex) {
            Debug.LogError (ex.Message);
            return false;
        }

        return true;
    }

    static void SetImageBlock (byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        ImageBlock ib = new ImageBlock ();

        // Image Separator(1 Byte)
        // 0x2c
        ib.imageSeparator = gifBytes[byteIndex];
        byteIndex++;

        // Image Left Position(2 Bytes)
        ib.imageLeftPosition = BitConverter.ToUInt16 (gifBytes, byteIndex);
        byteIndex += 2;

        // Image Top Position(2 Bytes)
        ib.imageTopPosition = BitConverter.ToUInt16 (gifBytes, byteIndex);
        byteIndex += 2;

        // Image Width(2 Bytes)
        ib.imageWidth = BitConverter.ToUInt16 (gifBytes, byteIndex);
        byteIndex += 2;

        // Image Height(2 Bytes)
        ib.imageHeight = BitConverter.ToUInt16 (gifBytes, byteIndex);
        byteIndex += 2;

        // 1 Byte
        {
            // Local Color Table Flag(1 Bit)
            ib.localColorTableFlag = (gifBytes[byteIndex] & 128) == 128; // 0b10000000

            // Interlace Flag(1 Bit)
            ib.interlaceFlag = (gifBytes[byteIndex] & 64) == 64; // 0b01000000

            // Sort Flag(1 Bit)
            ib.sortFlag = (gifBytes[byteIndex] & 32) == 32; // 0b00100000

            // Reserved(2 Bits)
            // Unused

            // Size of Local Color Table(3 Bits)
            int val = (gifBytes[byteIndex] & 7) + 1;
            ib.sizeOfLocalColorTable = (int) Math.Pow (2, val);

            byteIndex++;
        }

        if (ib.localColorTableFlag) {
            // Local Color Table(0～255×3 Bytes)
            ib.localColorTable = new List<byte[]> ();
            for (int i = byteIndex; i < byteIndex + (ib.sizeOfLocalColorTable * 3); i += 3) {
                ib.localColorTable.Add (new byte[] { gifBytes[i], gifBytes[i + 1], gifBytes[i + 2] });
            }
            byteIndex = byteIndex + (ib.sizeOfLocalColorTable * 3);
        }

        // LZW Minimum Code Size(1 Byte)
        ib.LzwMinimumCodeSize = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Image Data List
        while (true) {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00) {
                // Block Terminator(1 Byte)
                break;
            }

            var imageDataBlock = new ImageBlock.ImageDataBlock ();
            imageDataBlock.blockSize = blockSize;

            // Image Data(? Bytes)
            imageDataBlock.imageData = new byte[imageDataBlock.blockSize];
            for (int i = 0; i < imageDataBlock.imageData.Length; i++) {
                imageDataBlock.imageData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (ib.imageDataList == null) {
                ib.imageDataList = new List<ImageBlock.ImageDataBlock> ();
            }
            ib.imageDataList.Add (imageDataBlock);
        }

        if (gifData.imageBlockList == null) {
            gifData.imageBlockList = new List<ImageBlock> ();
        }
        gifData.imageBlockList.Add (ib);
    }

    static void SetGraphicControlExtension (byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        GraphicControlExtension gcEx = new GraphicControlExtension ();

        // Extension Introducer(1 Byte)
        // 0x21
        gcEx.extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Graphic Control Label(1 Byte)
        // 0xf9
        gcEx.graphicControlLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x04
        gcEx.blockSize = gifBytes[byteIndex];
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
            switch (gifBytes[byteIndex] & 28) { // 0b00011100
                case 4:     // 0b00000100
                    gcEx.disposalMethod = 1;
                    break;
                case 8:     // 0b00001000
                    gcEx.disposalMethod = 2;
                    break;
                case 12:    // 0b00001100
                    gcEx.disposalMethod = 3;
                    break;
                default:
                    gcEx.disposalMethod = 0;
                    break;
            }

            // User Input Flag(1 Bit)
            // Unknown

            // Transparent Color Flag(1 Bit)
            gcEx.transparentColorFlag = (gifBytes[byteIndex] & 1) == 1; // 0b00000001

            byteIndex++;
        }

        // Delay Time(2 Bytes)
        gcEx.delayTime = BitConverter.ToUInt16 (gifBytes, byteIndex);
        byteIndex += 2;

        // Transparent Color Index(1 Byte)
        gcEx.transparentColorIndex = gifBytes[byteIndex];
        byteIndex++;

        // Block Terminator(1 Byte)
        gcEx.blockTerminator = gifBytes[byteIndex];
        byteIndex++;

        if (gifData.graphicCtrlExList == null) {
            gifData.graphicCtrlExList = new List<GraphicControlExtension> ();
        }
        gifData.graphicCtrlExList.Add (gcEx);
    }

    static void SetCommentExtension (byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        CommentExtension commentEx = new CommentExtension ();

        // Extension Introducer(1 Byte)
        // 0x21
        commentEx.extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Comment Label(1 Byte)
        // 0xfe
        commentEx.commentLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Comment Data List
        while (true) {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00) {
                // Block Terminator(1 Byte)
                break;
            }

            var commentDataBlock = new CommentExtension.CommentDataBlock ();
            commentDataBlock.blockSize = blockSize;

            // Comment Data(n Byte)
            commentDataBlock.commentData = new byte[commentDataBlock.blockSize];
            for (int i = 0; i < commentDataBlock.commentData.Length; i++) {
                commentDataBlock.commentData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (commentEx.commentDataList == null) {
                commentEx.commentDataList = new List<CommentExtension.CommentDataBlock> ();
            }
            commentEx.commentDataList.Add (commentDataBlock);
        }

        if (gifData.commentExList == null) {
            gifData.commentExList = new List<CommentExtension> ();
        }
        gifData.commentExList.Add (commentEx);
    }

    static void SetPlainTextExtension (byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        PlainTextExtension plainTxtEx = new PlainTextExtension ();

        // Extension Introducer(1 Byte)
        // 0x21
        plainTxtEx.extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Plain Text Label(1 Byte)
        // 0x01
        plainTxtEx.plainTextLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x0c
        plainTxtEx.blockSize = gifBytes[byteIndex];
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
        while (true) {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00) {
                // Block Terminator(1 Byte)
                break;
            }

            var plainTextDataBlock = new PlainTextExtension.PlainTextDataBlock ();
            plainTextDataBlock.blockSize = blockSize;

            // Plain Text Data(n Byte)
            plainTextDataBlock.plainTextData = new byte[plainTextDataBlock.blockSize];
            for (int i = 0; i < plainTextDataBlock.plainTextData.Length; i++) {
                plainTextDataBlock.plainTextData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (plainTxtEx.plainTextDataList == null) {
                plainTxtEx.plainTextDataList = new List<PlainTextExtension.PlainTextDataBlock> ();
            }
            plainTxtEx.plainTextDataList.Add (plainTextDataBlock);
        }

        if (gifData.plainTextExList == null) {
            gifData.plainTextExList = new List<PlainTextExtension> ();
        }
        gifData.plainTextExList.Add (plainTxtEx);
    }

    static void SetApplicationExtension (byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        // Extension Introducer(1 Byte)
        // 0x21
        gifData.appEx.extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Extension Label(1 Byte)
        // 0xff
        gifData.appEx.extensionLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x0b
        gifData.appEx.blockSize = gifBytes[byteIndex];
        byteIndex++;

        // Application Identifier(8 Bytes)
        gifData.appEx.appId1 = gifBytes[byteIndex];
        byteIndex++;
        gifData.appEx.appId2 = gifBytes[byteIndex];
        byteIndex++;
        gifData.appEx.appId3 = gifBytes[byteIndex];
        byteIndex++;
        gifData.appEx.appId4 = gifBytes[byteIndex];
        byteIndex++;
        gifData.appEx.appId5 = gifBytes[byteIndex];
        byteIndex++;
        gifData.appEx.appId6 = gifBytes[byteIndex];
        byteIndex++;
        gifData.appEx.appId7 = gifBytes[byteIndex];
        byteIndex++;
        gifData.appEx.appId8 = gifBytes[byteIndex];
        byteIndex++;

        // Application Authentication Code(3 Bytes)
        gifData.appEx.appAuthCode1 = gifBytes[byteIndex];
        byteIndex++;
        gifData.appEx.appAuthCode2 = gifBytes[byteIndex];
        byteIndex++;
        gifData.appEx.appAuthCode3 = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Application Data List
        while (true) {
            // Block Size (1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00) {
                // Block Terminator(1 Byte)
                break;
            }

            var appDataBlock = new ApplicationExtension.ApplicationDataBlock ();
            appDataBlock.blockSize = blockSize;

            // Application Data(n Byte)
            appDataBlock.applicationData = new byte[appDataBlock.blockSize];
            for (int i = 0; i < appDataBlock.applicationData.Length; i++) {
                appDataBlock.applicationData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (gifData.appEx.appDataList == null) {
                gifData.appEx.appDataList = new List<ApplicationExtension.ApplicationDataBlock> ();
            }
            gifData.appEx.appDataList.Add (appDataBlock);
        }
    }
}
