using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewerController : MonoBehaviour
{
    public float speedLimit = 5.0f;
    public float acceleration = 1.0f;

    private float velocity = 0.0f;

    private void Update()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;

        velocity += (input == Vector3.zero) ? 0 : acceleration;
        velocity = Mathf.Clamp(velocity, 0.0f, speedLimit);

        transform.position += input * velocity * Time.deltaTime;
        
        velocity -= (input == Vector3.zero) ? acceleration : 0;
        velocity = Mathf.Clamp(velocity, 0.0f, speedLimit);
    }
}
