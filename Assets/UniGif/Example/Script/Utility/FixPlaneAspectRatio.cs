/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class FixPlaneAspectRatio : MonoBehaviour
{
    public enum HEIGHT_PARAM
    {
        Y,
        Z,
    }

    
    public int originalWidth;
    
    public int originalHeight;
    
    public HEIGHT_PARAM toHeightParam = HEIGHT_PARAM.Z;
    
    public bool fixOnUpdate;

    Vector3 lastScale = Vector3.zero;
    Vector3 newScale = Vector3.zero;
    
    Transform myTransform;

    new Transform transform
    {
        get
        {
            return myTransform != null ? myTransform : (myTransform = GetComponent<Transform> ());
        }
    }

    void Update ()
    {
#if UNITY_EDITOR
        if (Application.isPlaying == false) {
            FixAspectRatio ();
            return;
        }
#endif

        if (fixOnUpdate) {
            FixAspectRatio ();
        }
    }

    public void FixAspectRatio (int orgWidth = -1, int orgHeight = -1)
    {
        bool forceUpdate = false;
        if (orgWidth > 0 && orgHeight > 0) {
            originalWidth = orgWidth;
            originalHeight = orgHeight;
            forceUpdate = true;
        }
        if (originalWidth <= 0 || originalHeight <= 0) {
            return;
        }

        bool changeX;
        if (forceUpdate || lastScale.x != transform.localScale.x) {
            changeX = true;
        } else if (lastScale.z != transform.localScale.z) {
            changeX = false;
        } else {
            return;
        }

        if (changeX) {
            float ratio = transform.localScale.x / (float) originalWidth;
            if (toHeightParam == HEIGHT_PARAM.Y) {
                newScale.Set (transform.localScale.x, (float) originalHeight * ratio, transform.localScale.z);
            } else {
                newScale.Set (transform.localScale.x, transform.localScale.y, (float) originalHeight * ratio);
            }

        } else {
            float ratio = toHeightParam == HEIGHT_PARAM.Y ? transform.localScale.y / (float) originalHeight : transform.localScale.z / (float) originalHeight;
            newScale.Set ((float) originalWidth * ratio, transform.localScale.y, transform.localScale.z);
        }
        transform.localScale = newScale;

        lastScale = transform.localScale;
    }
}