/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Texture Animation from GIF image
/// </summary>
public class UniGifImage : MonoBehaviour
{
    /// <summary>
    /// This component state
    /// </summary>
    public enum State
    {
        None,
        Loading,
        Ready,
        Playing,
        Pause,
    }

    // Target row image
    [SerializeField]
    private RawImage m_rawImage;
    // Image Aspect Controller
    [SerializeField]
    private UniGifImageAspectController m_imgAspectCtrl;
    // Textures filter mode
    [SerializeField]
    private FilterMode m_filterMode = FilterMode.Point;
    // Textures wrap mode
    [SerializeField]
    private TextureWrapMode m_wrapMode = TextureWrapMode.Clamp;
    // Animationa pause flag
    [SerializeField]
    private bool m_pauseAnimation;
    // Load from url on start
    [SerializeField]
    private bool m_loadOnStart;
    // GIF image url (WEB or StreamingAssets path)
    [SerializeField]
    private string m_loadOnStartUrl;
    // Use coroutine flag to GetTextureList
    [SerializeField]
    private bool m_useCoroutineGetTexture;
    // Rotating on loading
    [SerializeField]
    private bool m_rotateOnLoading;
    // Debug log flag
    [SerializeField]
    private bool m_outputDebugLog;

    // Decoded GIF texture list
    private List<UniGif.GifTexture> m_gifTexList = new List<UniGif.GifTexture>();
    // Loading flag
    private bool m_loading;

    /// <summary>
    /// Now state
    /// </summary>
    public State state
    {
        get;
        private set;
    }

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

    private void Start()
    {
        if (m_rawImage == null)
        {
            m_rawImage = GetComponent<RawImage>();
        }
        if (m_loadOnStart)
        {
            StartCoroutine(SetGifFromUrlCoroutine(m_loadOnStartUrl));
        }
    }

    private void Update()
    {
        if (m_rotateOnLoading && m_loading)
        {
            transform.Rotate(0f, 0f, 30f * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// Set GIF texture from url
    /// </summary>
    /// <param name="url">GIF image url (WEB or StreamingAssets path)</param>
    /// <param name="autoPlay">Auto play after decode</param>
    /// <returns>IEnumerator</returns>
    public IEnumerator SetGifFromUrlCoroutine(string url, bool autoPlay = true)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("URL is nothing.");
            yield break;
        }

        m_loading = true;

        string path;
        if (url.StartsWith("http"))
        {
            // from WEB
            path = url;
        }
        else
        {
            // from StreamingAssets
            path = System.IO.Path.Combine("file:///" + Application.streamingAssetsPath, url);
        }

        // Load file
        using (WWW www = new WWW(path))
        {
            yield return www;

            if (string.IsNullOrEmpty(www.error) == false)
            {
                Debug.LogError("File load error.\n" + www.error);
            }
            else
            {
                m_gifTexList.Clear();

                // Get GIF textures
                if (m_useCoroutineGetTexture)
                {
                    // use coroutine (avoid lock up but more slow)
                    yield return StartCoroutine(UniGif.GetTextureListCoroutine(this, www.bytes, (gtList, loop, w, h) =>
                    {
                        m_gifTexList = gtList;
                        FinishedGetTextureList(loop, w, h, autoPlay);
                    }, m_filterMode, m_wrapMode, m_outputDebugLog));

                }
                else
                {
                    // dont use coroutine (there is a possibility of lock up)
                    int loop, w, h;
                    m_gifTexList = UniGif.GetTextureList(www.bytes, out loop, out w, out h, m_filterMode, m_wrapMode, m_outputDebugLog);
                    FinishedGetTextureList(loop, w, h, autoPlay);
                }
            }
        }
    }

    /// <summary>
    /// Finished UniGif.GetTextureList or UniGif.GetTextureListCoroutine
    /// </summary>
    private void FinishedGetTextureList(int loop, int w, int h, bool autoPlay)
    {
        m_loading = false;
        loopCount = loop;
        width = w;
        height = h;
        state = State.Ready;
        if (m_rotateOnLoading)
        {
            transform.localEulerAngles = Vector3.zero;
        }
        if (autoPlay)
        {
            // Start GIF animation
            StartCoroutine(GifLoopCoroutine());
        }
        // fix aspect ratio
        m_imgAspectCtrl.FixAspectRatio(width, height);
    }

    /// <summary>
    /// GIF Animation loop
    /// </summary>
    /// <returns>IEnumerator</returns>
    private IEnumerator GifLoopCoroutine()
    {
        if (state != State.Ready)
        {
            Debug.LogWarning("State is not READY.");
            yield break;
        }
        if (m_rawImage == null || m_gifTexList == null || m_gifTexList.Count <= 0)
        {
            Debug.LogError("TargetRenderer or GIF texture is nothing.");
            yield break;
        }
        // play start
        state = State.Playing;
        int nowLoopCount = 0;
        do
        {
            for (int i = 0; i < m_gifTexList.Count; i++)
            {
                // Change texture
                m_rawImage.texture = m_gifTexList[i].m_texture2d;
                // Delay
                float delayedTime = Time.time + m_gifTexList[i].m_delaySec;
                while (delayedTime > Time.time)
                {
                    yield return 0;
                }
                // Pause
                while (m_pauseAnimation)
                {
                    yield return 0;
                }
            }

            if (loopCount > 0)
            {
                nowLoopCount++;
            }

        } while (loopCount <= 0 || nowLoopCount < loopCount);
    }

    /// <summary>
    /// Play animation
    /// </summary>
    public void Play()
    {
        if (state != State.Ready)
        {
            Debug.LogWarning("State is not READY.");
            return;
        }
        StopAllCoroutines();
        StartCoroutine(GifLoopCoroutine());
    }

    /// <summary>
    /// Stop animation
    /// </summary>
    public void Stop()
    {
        if (m_outputDebugLog && state != State.Playing && state != State.Pause)
        {
            Debug.Log("State is not PLAYING and PAUSE.");
            return;
        }
        StopAllCoroutines();
        state = State.Ready;
    }

    /// <summary>
    /// Pause animation
    /// </summary>
    public void Pause()
    {
        if (m_outputDebugLog && state != State.Playing)
        {
            Debug.Log("State is not PLAYING.");
            return;
        }
        m_pauseAnimation = true;
        state = State.Pause;
    }

    /// <summary>
    /// Resume animation
    /// </summary>
    public void Resume()
    {
        if (m_outputDebugLog && state != State.Pause)
        {
            Debug.Log("State is not PAUSE.");
            return;
        }
        m_pauseAnimation = false;
        state = State.Playing;
    }
}