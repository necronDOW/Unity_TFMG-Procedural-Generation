using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 5.0f;

    private Vector3 offset;
    private Camera cam;

    private void Start()
    {
        if (target)
            offset = transform.position - target.position;

        cam = GetComponent<Camera>();
    }
    
    private void Update()
    {
        if (target)
        {
            Vector3 targetPoint = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPoint, smoothTime * Time.deltaTime);
        }
        
        Vector3 direction = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.nearClipPlane)) - transform.position;
        Quaternion targetRotation = Quaternion.FromToRotation(transform.forward, direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10.0f * Time.deltaTime);
    }
}
