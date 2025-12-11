using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    
    public Transform player;
    public float distance;

    private void LateUpdate()
    {
        transform.position = player.position + Vector3.up * distance;
    }
}
