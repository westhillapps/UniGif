/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;

[ExecuteInEditMode]
public class UniGifImageAspectController : MonoBehaviour
{
    public int m_originalWidth;
    public int m_originalHeight;

    public bool m_fixOnUpdate;

    private Vector2 m_lastSize = Vector2.zero;
    private Vector2 m_newSize = Vector2.zero;

    private RectTransform m_rectTransform;

    public RectTransform rectTransform
    {
        get
        {
            return m_rectTransform != null ? m_rectTransform : (m_rectTransform = GetComponent<RectTransform>());
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            FixAspectRatio();
            return;
        }
#endif

        if (m_fixOnUpdate)
        {
            FixAspectRatio();
        }
    }

    public void FixAspectRatio(int originalWidth = -1, int originalHeight = -1)
    {
        bool forceUpdate = false;
        if (originalWidth > 0 && originalHeight > 0)
        {
            m_originalWidth = originalWidth;
            m_originalHeight = originalHeight;
            forceUpdate = true;
        }
        if (m_originalWidth <= 0 || m_originalHeight <= 0)
        {
            return;
        }

        bool changeX;
        if (forceUpdate || m_lastSize.x != rectTransform.sizeDelta.x)
        {
            changeX = true;
        }
        else if (m_lastSize.y != rectTransform.sizeDelta.y)
        {
            changeX = false;
        }
        else
        {
            return;
        }

        if (changeX)
        {
            float ratio = rectTransform.sizeDelta.x / m_originalWidth;
            m_newSize.Set(rectTransform.sizeDelta.x, m_originalHeight * ratio);
        }
        else
        {
            float ratio = rectTransform.sizeDelta.y / m_originalHeight;
            m_newSize.Set(m_originalWidth * ratio, rectTransform.sizeDelta.y);
        }
        rectTransform.sizeDelta = m_newSize;

        m_lastSize = rectTransform.sizeDelta;
    }
}