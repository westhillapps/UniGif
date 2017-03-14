/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UniGifTest : MonoBehaviour
{
    [SerializeField]
    private InputField m_inputField;
    [SerializeField]
    private UniGifImage m_uniGifImage;

    private bool m_mutex;

    public void OnButtonClicked()
    {
        if (m_mutex || m_uniGifImage == null || string.IsNullOrEmpty(m_inputField.text))
        {
            return;
        }

        m_mutex = true;
        StartCoroutine(ViewGifCoroutine());
    }

    private IEnumerator ViewGifCoroutine()
    {
        yield return StartCoroutine(m_uniGifImage.SetGifFromUrlCoroutine(m_inputField.text));
        m_mutex = false;
    }
}