using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.WSA.Input;

public class ThrowingSceneManager : ImprovedSingletonBehavior<ThrowingSceneManager>
{
    public GameObject PCCameraRig;
    public Camera PCCamera;
    public SteamVR_PlayArea steamPlayArea;
    public float PCRigYOffset = 1.0f;
    private Vector3[] bounds;
    private int boundsIndex = 0;

    protected override void InitializeInternal()
    {                                                    
        if (InputManager.Instance.CurrentInputMode == InputManager.InputMode.Unity)
        {
            if (steamPlayArea == null)
            {
                throw new InvalidOperationException("Please set the SteamVR_PlayArea in the throwing Scene Manager!");
            }

            bounds = steamPlayArea.GetRectangle();
            var tempBounds = new List<Vector3>();
            foreach (var point in bounds)
            {
                if (point != Vector3.zero)
                {
                    tempBounds.Add(point);
                }
            }
            bounds = tempBounds.ToArray();
        }

        if (bounds == null || bounds.Length < 4)
        {
            // Todo: somethign about this 
            // Todo: give min size;
            throw new InvalidOperationException("Neew bounds to play the throwing game!!");
        }

        if (PCCamera == null)
        {
            PCCamera = PCCameraRig.GetComponentInChildren<Camera>();
        }

        UpdateBoundSide(boundsIndex);
    }

    void Start()
    {
        this.Initialize();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            UpdateBoundSide(boundsIndex = (boundsIndex + 1) % bounds.Length);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            UpdateBoundSide(boundsIndex = (boundsIndex + bounds.Length - 1) % bounds.Length);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            PCRigYOffset += 0.05f;
            UpdateBoundSide(boundsIndex);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            PCRigYOffset -= 0.05f;
            UpdateBoundSide(boundsIndex);
        }
    }

    public void UpdateBoundSide(int index)
    {
        var point1 = bounds[index];
        var point2 = bounds[(index + 1) % bounds.Length];
        var center = point2 - (point2 - point1) / 2.0f;
        //center.y = this.PCRigYOffset;

        var distanceBack = Mathf.Abs((point2 - point1).magnitude / (2.0f * Mathf.Tan(Mathf.Rad2Deg * PCCamera.fieldOfView / 2)));
        var back = Vector3.Cross(point2 - point1, Vector3.up).normalized * distanceBack;

        center.y = this.PCRigYOffset;
        PCCameraRig.transform.position = center + back;
        PCCameraRig.transform.rotation = Quaternion.LookRotation(-back, Vector3.up);

        this.boundsIndex = index;
    }
}
