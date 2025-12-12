using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectPosition : MonoBehaviour
{
    public RectTransform targetRect;
    private Transform particleTransform;
    void Start()
    {
        particleTransform = GetComponent<Transform>();
        if(targetRect != null)
        {
            enabled = false;
        }
    }
    private void LateUpdate()
    {
        if(targetRect != null)
        {
            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(null, targetRect.position);
            screenPoint.z = 1;
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(screenPoint);
            particleTransform.position = worldPoint;
        }
    }
}
