/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Scale2dCamera : MonoBehaviour
{
    [SerializeField]
    int targetWidth = 640;
    
    [SerializeField]
    float pixelsToUnits = 100;
    
    [SerializeField]
    Camera targetCamera;

    void Awake ()
    {
        if (targetCamera == null) {
            targetCamera = camera;
        }
    }

    void Update ()
    {
        if (targetCamera == null) {
            return;
        }
        int height = Mathf.RoundToInt (targetWidth / (float) Screen.width * Screen.height);
        targetCamera.orthographicSize = (float) height / pixelsToUnits / 2f;
    }
}
