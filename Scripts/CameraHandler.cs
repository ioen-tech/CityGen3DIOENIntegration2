using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraHandler : MonoBehaviour
{
    private new Camera camera;
    private float orthographicSize;
    private float targetOrthographicSize;

    private void Awake()
    {
        camera = GetComponent<Camera>();
    }
    private void Start()
    {
        orthographicSize = camera.fieldOfView;
        targetOrthographicSize = orthographicSize;
    }
    private void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    private void HandleMovement()
    {
        float x = transform.position.x;
        float y = transform.position.y;
        float z = transform.position.z;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            z += 2;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            z -= 2;
        }
        if (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.LeftShift))
        {
            z += 6;
        }
        if (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.LeftShift))
        {
            z -= 6;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            x += 2;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            x -= 2;
        }
        if (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.LeftShift))
        {
            x += 6;
        }
        if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.LeftShift))
        {
            x -= 6;
        }

        transform.position = new Vector3(x, y, z);
    }

    private void HandleZoom()
    {
        float zoomAmount = 2f;
        float zoomSpeed = 5f;
        float minOrthographicSize = 5;
        float maxOrthographicSize = 120;
        targetOrthographicSize += Input.mouseScrollDelta.y * zoomAmount;
        targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);
        orthographicSize = Mathf.Lerp(targetOrthographicSize, orthographicSize, Time.deltaTime * zoomSpeed);
        camera.fieldOfView = orthographicSize;
    }
}
