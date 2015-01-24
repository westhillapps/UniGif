/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Texture Animation from GIF image
/// </summary>
public class UniGifTexture : MonoBehaviour
{
    /// <summary>
    /// This component state
    /// </summary>
    public enum STATE
    {
        NONE,
        LOADING,
        READY,
        PLAYING,
        PAUSE,
    }

    /// <summary>
    /// Now state
    /// </summary>
    public STATE state
    {
        get;
        private set;
    }

    // Renderer of target
    [SerializeField]
    Renderer targetRenderer;
    // Textures filter mode
    [SerializeField]
    FilterMode filterMode = FilterMode.Point;
    // Textures wrap mode
    [SerializeField]
    TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    // Animationa pause flag
    [SerializeField]
    bool pauseAnimation;
    // Load from url on start
    [SerializeField]
    bool loadOnStart;
    // GIF image url (WEB or StreamingAssets path)
    [SerializeField]
    string loadOnStartUrl;
    // Use coroutine flag to GetTextureList
    [SerializeField]
    bool useCoroutineGetTexture;
    // Rotating on loading
    [SerializeField]
    bool rotateOnLoading;
    // Debug log flag
    [SerializeField]
    bool outputDebugLog;

    // Decoded GIF texture list
    List<UniGif.GifTexture> gifTexList = new List<UniGif.GifTexture> ();
    // Loading flag
    bool loading;

    /// <summary>
    /// Animation loop count (0 is infinite)
    /// </summary>
    public int loopCount
    {
        get;
        private set;
    }

    /// <summary>
    /// Texture width (px)
    /// </summary>
    public int width
    {
        get;
        private set;
    }

    /// <summary>
    /// Texture height (px)
    /// </summary>
    public int height
    {
        get;
        private set;
    }

    void Start ()
    {
        if (targetRenderer == null) {
            targetRenderer = renderer;
        }
        if (loadOnStart) {
            StartCoroutine (SetGifFromUrlCoroutine (loadOnStartUrl));
        }
    }

    void Update ()
    {
        if (rotateOnLoading && loading) {
            transform.Rotate (0f, 0f, 30f * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// Set GIF texture from url
    /// </summary>
    /// <param name="url">GIF image url (WEB or StreamingAssets path)</param>
    /// <param name="autoPlay">Auto play after decode</param>
    /// <returns>IEnumerator</returns>
    public IEnumerator SetGifFromUrlCoroutine (string url, bool autoPlay = true)
    {
        if (string.IsNullOrEmpty (url)) {
            Debug.LogError ("URL is nothing.");
            yield break;
        }

        loading = true;

        string path;
        if (url.StartsWith ("http")) {
            // from WEB
            path = url;
        } else {
            // from StreamingAssets
            path = System.IO.Path.Combine ("file:///" + Application.streamingAssetsPath, url);
        }

        // Load file
        using (WWW www = new WWW (path)) {
            yield return www;

            if (string.IsNullOrEmpty (www.error) == false) {
                Debug.LogError ("File load error.\n" + www.error);

            } else {
                gifTexList.Clear ();

                // Get GIF textures
                if (useCoroutineGetTexture) {
                    // use coroutine (avoid lock up but more slow)
                    yield return StartCoroutine (UniGif.GetTextureListCoroutine (this, www.bytes, (gtList, loop, w, h) => {
                        gifTexList = gtList;
                        FinishedGetTextureList (loop, w, h, autoPlay);
                    }, filterMode, wrapMode, outputDebugLog));

                } else {
                    // dont use coroutine (there is a possibility of lock up)
                    int loop, w, h;
                    gifTexList = UniGif.GetTextureList (www.bytes, out loop, out w, out h, filterMode, wrapMode, outputDebugLog);
                    FinishedGetTextureList (loop, w, h, autoPlay);
                }
            }
        }
    }

    /// <summary>
    /// Finished UniGif.GetTextureList or UniGif.GetTextureListCoroutine
    /// </summary>
    void FinishedGetTextureList (int loop, int w, int h, bool autoPlay)
    {
        loading = false;
        loopCount = loop;
        width = w;
        height = h;
        state = STATE.READY;
        if (rotateOnLoading) {
            transform.localEulerAngles = Vector3.zero;
        }
        if (autoPlay) {
            // Start GIF animation
            StartCoroutine (GifLoopCoroutine ());
        }
    }

    /// <summary>
    /// GIF Animation loop
    /// </summary>
    /// <returns>IEnumerator</returns>
    IEnumerator GifLoopCoroutine ()
    {
        if (state != STATE.READY) {
            Debug.LogWarning ("State is not READY.");
            yield break;
        }
        if (targetRenderer == null || gifTexList == null || gifTexList.Count <= 0) {
            Debug.LogError ("TargetRenderer or GIF texture is nothing.");
            yield break;
        }
        // play start
        state = STATE.PLAYING;
        int nowLoopCount = 0;
        do {
            foreach (var gifTex in gifTexList) {
                // Change texture
                targetRenderer.material.mainTexture = gifTex.texture2d;
                // Delay
                float delayedTime = Time.time + gifTex.delaySec;
                while (delayedTime > Time.time) {
                    yield return 0;
                }
                // Pause
                while (pauseAnimation) {
                    yield return 0;
                }
            }
            if (loopCount > 0) {
                nowLoopCount++;
            }

        } while (loopCount <= 0 || nowLoopCount < loopCount);
    }

    /// <summary>
    /// Play animation
    /// </summary>
    public void Play ()
    {
        if (state != STATE.READY) {
            Debug.LogWarning ("State is not READY.");
            return;
        }
        StopAllCoroutines ();
        StartCoroutine (GifLoopCoroutine ());
    }

    /// <summary>
    /// Stop animation
    /// </summary>
    public void Stop ()
    {
        if (outputDebugLog && state != STATE.PLAYING && state != STATE.PAUSE) {
            Debug.Log ("State is not PLAYING and PAUSE.");
            return;
        }
        StopAllCoroutines ();
        state = STATE.READY;
    }

    /// <summary>
    /// Pause animation
    /// </summary>
    public void Pause ()
    {
        if (outputDebugLog && state != STATE.PLAYING) {
            Debug.Log ("State is not PLAYING.");
            return;
        }
        pauseAnimation = true;
        state = STATE.PAUSE;
    }

    /// <summary>
    /// Resume animation
    /// </summary>
    public void Resume ()
    {
        if (outputDebugLog && state != STATE.PAUSE) {
            Debug.Log ("State is not PAUSE.");
            return;
        }
        pauseAnimation = false;
        state = STATE.PLAYING;
    }
}