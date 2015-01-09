/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System.Collections;

public class UniGifTest : MonoBehaviour
{
    const int BASE_SCREEN_WIDTH = 640;
    const int FONT_SIZE = 22;


    Rect rectArea = new Rect ();
    Color textColor = new Color32 (230, 230, 230, 255);
    string textField = "";
    bool mutex;

    UniGifTexture uniGifTexture;
    FixPlaneAspectRatio fixAspect;

    void Awake ()
    {
        uniGifTexture = GetComponent<UniGifTexture> ();
        fixAspect = GetComponent<FixPlaneAspectRatio> ();
    }

    void OnGUI ()
    {
        rectArea.x = 0f;
        rectArea.y = 0f;
        rectArea.width = Screen.width;
        rectArea.height = Screen.height;

        GUILayout.BeginArea (rectArea);
        {
            float screenScale = (float) Screen.width / (float) BASE_SCREEN_WIDTH;

            GUIStyle guiStyle = GUIStyle.none;
            guiStyle.fontSize = (int) (FONT_SIZE * screenScale);
            guiStyle.normal.textColor = textColor;
            guiStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.FlexibleSpace ();

            // Label
            GUILayout.BeginHorizontal ();
            {
                GUILayout.FlexibleSpace ();

                GUILayout.Label ("Input GIF image URL", guiStyle, GUILayout.Width ((float) Screen.width));

                GUILayout.FlexibleSpace ();
            }
            GUILayout.EndHorizontal ();

            // Input
            GUILayout.BeginHorizontal ();
            {
                GUILayout.FlexibleSpace ();

                textField = GUILayout.TextField (textField, GUILayout.Width ((float) Screen.width * 0.5f));

                GUILayout.FlexibleSpace ();
            }
            GUILayout.EndHorizontal ();

            // Button
            GUILayout.BeginHorizontal ();
            {
                GUILayout.FlexibleSpace ();

                if (GUILayout.Button ("View GIF Texture", GUILayout.Width ((float) Screen.width * 0.5f), GUILayout.Height ((float) Screen.height * 0.075f))) {
                    if (mutex == false && uniGifTexture != null && string.IsNullOrEmpty (textField) == false) {
                        mutex = true;
                        uniGifTexture.Stop ();
                        StartCoroutine (ViewGifCoroutine ());
                    }
                }

                GUILayout.FlexibleSpace ();
            }
            GUILayout.EndHorizontal ();
        }
        GUILayout.EndArea ();
    }

    IEnumerator ViewGifCoroutine ()
    {
        yield return StartCoroutine (uniGifTexture.SetGifFromUrlCoroutine (textField));

        fixAspect.FixAspectRatio (uniGifTexture.width, uniGifTexture.height);

        mutex = false;
    }
}