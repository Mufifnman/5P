using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.WSA.Input;

public class ThrowingSceneManager : ImprovedSingletonBehavior<ThrowingSceneManager>
{
    public enum InputMode
    {
        WindowsMR,
        Unity,
    }

    public GameObject[] ThrowingAssets;
    public InteractionSourceNode ControllerPose = InteractionSourceNode.Grip;
    public Transform RealWorldRoot;
    public InputMode inputMode = InputMode.Unity;

    public GameObject PCCameraRig;
    public Camera PCCamera;
    public SteamVR_PlayArea steamPlayArea;
    public float PCRigYOffset = 1.0f;
    public float DistanceBack = 1.5f; //Todo: math t ofind out what this should be from cameraand distance between points
    private Vector3[] bounds;
    private int boundsIndex = 0;
    
    public float GripSensitivity = 0.2f;
    private float lastGripLeft = -1.0f;
    private float lastGripRight = -1.0f;

    private readonly Dictionary<uint, Transform> devices = new Dictionary<uint, Transform>();
    private readonly Dictionary<uint, int> modelIndecies = new Dictionary<uint, int>();
    private readonly Dictionary<uint, bool> isDetatched = new Dictionary<uint, bool>();

    public float velocityModifier = 0.8f;

    protected override void InitializeInternal()
    {
        if (inputMode == InputMode.Unity)
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

            Physics.gravity = -Vector3.up * 9.8f * velocityModifier;
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

    // Use this for initialization
    void Start ()
    {
        this.Initialize();

		if (ThrowingAssets.Length == 0)
        {
            throw new System.Exception("ThrowingSceneManager needs to have some throwable objects!");
        }

        if (inputMode == InputMode.WindowsMR)
        {
            SetupWindowsMRInput();
        }
        else
        {
            SetupUnityInput();
        }
    }

    private void SetupWindowsMRInput()
    {
        InteractionManager.InteractionSourceReleased += InteractionManager_InteractionSourceReleased;
        InteractionManager.InteractionSourcePressed += InteractionManager_InteractionSourcePressed;
        Application.onBeforeRender += Application_onBeforeRender;
    }

    private void SetupUnityInput()
    {
        UpdateTrackedControllers();
    }

    private void UpdateTrackedControllers()
    {
        var detectedControllers = new List<string>(Input.GetJoystickNames());

        //Debug.Log("detected controllers: " + detectedControllers);

        // add any new controllers
        foreach (var name in detectedControllers)
        {
            if (name.Contains("OpenVR Controller")) //|| name.Contains("Spatial Controller")) // Motion Controllers
            {
                XRNode? nodeType = null;

                if (name.Contains("Left"))
                {
                    nodeType = XRNode.LeftHand;
                }
                else if (name.Contains("Right"))
                {
                    nodeType = XRNode.RightHand;
                }

                if (nodeType.HasValue)
                {
                    if (!devices.ContainsKey(nodeType.Value.GetID()))
                    {
                        AddDevice(nodeType.Value.GetID());
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (inputMode == InputMode.Unity)
        {
            UpdateUnityInput();
        }

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

        DistanceBack = Mathf.Abs( (point2 - point1).magnitude / (2.0f * Mathf.Tan(Mathf.Rad2Deg * PCCamera.fieldOfView / 2)));
        var back = Vector3.Cross(point2 - point1, Vector3.up).normalized * DistanceBack;

        center.y = this.PCRigYOffset;
        PCCameraRig.transform.position = center + back;
        PCCameraRig.transform.rotation = Quaternion.LookRotation(-back, Vector3.up);

        this.boundsIndex = index;
    }

    private void UpdateUnityInput()
    {
        UpdateTrackedControllers();

        // Motion
        foreach (uint id in devices.Keys)
        {
            var nodeType = id.GetNodeType();
            var position = InputTracking.GetLocalPosition(nodeType);
            var rotation = InputTracking.GetLocalRotation(nodeType);

            SetTransform(devices[id], position, rotation);
        }

        // Create new 
        if (Input.GetButtonDown("MotionController-Select-Left"))
        {
            this.CreateNew(NodeExtensions.LEFT_ID);
        }
        if (Input.GetButtonDown("MotionController-Select-Right"))
        {
            this.CreateNew(NodeExtensions.RIGHT_ID);
        }
        if (Input.GetAxis("MotionController-GraspedAmmount-Left") > GripSensitivity && lastGripLeft <= GripSensitivity)
        {
            this.CreateNew(NodeExtensions.LEFT_ID);
        }
        if (Input.GetAxis("MotionController-GraspedAmmount-Right") > GripSensitivity && lastGripRight <= GripSensitivity)
        {
            this.CreateNew(NodeExtensions.RIGHT_ID);
        }

        // Swap
        if (Input.GetButtonDown("MotionController-Menu-Left"))
        {
            this.TrySwitchThrownObject(NodeExtensions.LEFT_ID);
        }
        if (Input.GetButtonDown("MotionController-Menu-Right"))
        {
            this.TrySwitchThrownObject(NodeExtensions.RIGHT_ID);
        }
        if (Input.GetButtonDown("MotionController-TouchpadPressed-Left"))
        {
            this.TrySwitchThrownObject(NodeExtensions.LEFT_ID);
        }
        if (Input.GetButtonDown("MotionController-TouchpadPressed-Right"))
        {
            this.TrySwitchThrownObject(NodeExtensions.RIGHT_ID);
        }

        // Throw
        if (Input.GetButtonUp("MotionController-Select-Left"))
        {
            this.TryThrow(XRNode.LeftHand);
        }
        if (Input.GetButtonUp("MotionController-Select-Right"))
        {
            this.TryThrow(XRNode.RightHand);
        }
        if (Input.GetAxis("MotionController-GraspedAmmount-Left") <= GripSensitivity && lastGripLeft     > GripSensitivity)
        {
            this.TryThrow(XRNode.LeftHand);
        }
        if (Input.GetAxis("MotionController-GraspedAmmount-Right") <= GripSensitivity && lastGripRight > GripSensitivity)
        {
            this.TryThrow(XRNode.RightHand);
        }

        lastGripLeft = Input.GetAxis("MotionController-GraspedAmmount-Left");
        lastGripRight = Input.GetAxis("MotionController-GraspedAmmount-Right");
    }

    private void InteractionManager_InteractionSourcePressed(InteractionSourcePressedEventArgs args)
    {
        uint id = args.state.source.id;
        if (args.pressType == InteractionSourcePressType.Menu || args.pressType == InteractionSourcePressType.Touchpad)
        {
            TrySwitchThrownObject(id);
        }
        else if (args.pressType == InteractionSourcePressType.Grasp || args.pressType == InteractionSourcePressType.Select)
        {
            CreateNew(id);
        }
    }

    private void TrySwitchThrownObject(uint id)
    {
        if (devices.ContainsKey(id))
        {
            int modelIndex = this.modelIndecies[id];
            RemoveDevice(id);
            AddDevice(id, ++modelIndex % ThrowingAssets.Length);
        }
        else if (modelIndecies.ContainsKey(id))
        {
            this.modelIndecies[id] = ++this.modelIndecies[id] % ThrowingAssets.Length;
        }
    }

    private void CreateNew(uint id)
    {
        if (isDetatched.ContainsKey(id))
        {
            isDetatched[id] = false;
        }
    }

    private void InteractionManager_InteractionSourceReleased(InteractionSourceReleasedEventArgs args)
    {
        if (args.pressType == InteractionSourcePressType.Grasp || args.pressType == InteractionSourcePressType.Select)
        {
            uint id = args.state.source.id;
            if (devices.ContainsKey(id))
            {
                var go = devices[id];
                var rigidbody = go.GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    rigidbody = go.GetComponentInChildren<Rigidbody>();
                }
                if (rigidbody.TryThrow(args.state.sourcePose))
                {
                    DetatchDevice(id);
                }
                else
                {
                    throw new System.Exception("Throw failed!!!");
                }
            }
        }
    }

    private void TryThrow(XRNode node)
    {
        uint id = node.GetID();
        if (devices.ContainsKey(id))
        {
            var go = devices[id];
            var rigidbody = go.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = go.GetComponentInChildren<Rigidbody>();
            }
            if (rigidbody.TryThrow(node, velocityModifier))
            {
                DetatchDevice(id);
            }
            else
            {
                throw new System.Exception("Throw failed!!!");
            }
        }
    }



    /// <summary>
    /// Unity will have an updated predicted rotation here, use this function to tweak
    /// the rendered model last minute to have the smoothest visual experience.
    /// </summary>
    private void Application_onBeforeRender()
    {
        foreach (var sourceState in InteractionManager.GetCurrentReading())
        {
            uint id = sourceState.source.id;
            var handedness = sourceState.source.handedness;
            var sourcePose = sourceState.sourcePose;
            Vector3 position;
            Quaternion rotation;
            if (devices.ContainsKey(id))
            {
                if (sourcePose.TryGetPosition(out position, this.ControllerPose) &&
                    sourcePose.TryGetRotation(out rotation, this.ControllerPose)) // defaults to grip
                {
                    SetTransform(devices[id], position, rotation);
                }
            }
            else if (sourceState.source.supportsPointing)
            {
                if (this.modelIndecies.ContainsKey(id))
                {
                    this.AddDevice(id, this.modelIndecies[id]);
                }
                else
                {
                    this.AddDevice(id);
                }

                if (!isDetatched.ContainsKey(id) || !isDetatched[id])
                {
                    if (sourcePose.TryGetPosition(out position, this.ControllerPose) &&
                    sourcePose.TryGetRotation(out rotation, this.ControllerPose)) // defaults to grip
                    {
                        SetTransform(devices[id], position, rotation);
                    }
                }
            }
        }
    }

    private void AddDevice(uint id, int index = 0)
    {
        if (!devices.ContainsKey(id) && (!isDetatched.ContainsKey(id) || !isDetatched[id]))
        {
            GameObject go = Instantiate(this.ThrowingAssets[index], this.RealWorldRoot);
            go.name = "Controller " + id;
            devices[id] = go.transform;
            modelIndecies[id] = index;
            isDetatched[id] = false;
        }
    }

    private void RemoveDevice(uint id)
    {
        if (devices.ContainsKey(id))
        {
            Destroy(devices[id].gameObject);
            devices.Remove(id);
        }
    }

    private void DetatchDevice(uint id)
    { 
        if (devices.ContainsKey(id))
        {
            devices[id].SetParent(null);
            isDetatched[id] = true;
            devices.Remove(id);
        }
    }

    private void SetTransform(Transform t, Vector3 position, Quaternion rotation)
    {
        // This check shouldn't be necessary
        if (!(float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
            float.IsNaN(rotation.w) || float.IsNaN(rotation.x) || float.IsNaN(rotation.y) || float.IsNaN(rotation.z)))
        {
            t.localPosition = position;
            t.localRotation = rotation;
        }
    }

    void TearDownWindowsMRInput()
    {
        InteractionManager.InteractionSourceReleased -= InteractionManager_InteractionSourceReleased;
        InteractionManager.InteractionSourcePressed -= InteractionManager_InteractionSourcePressed;
        Application.onBeforeRender -= Application_onBeforeRender;
    }

    private void TearDownUnityInput()
    {
        foreach (var deviceId in devices.Keys)
        {
            RemoveDevice(deviceId);
        }
    }

    void OnDestroy()
    {
        if (inputMode == InputMode.WindowsMR)
        {
            TearDownWindowsMRInput();
        }
        else
        {
            TearDownUnityInput();
        }
    }
}

public static class NodeExtensions
{
    public static uint LEFT_ID = 0u;
    public static uint RIGHT_ID = 1u;

    public static uint GetID(this XRNode node)
    {
        return node == XRNode.LeftHand ? LEFT_ID : RIGHT_ID;
    }

    public static XRNode GetNodeType(this uint id)
    {
        return id == LEFT_ID ? XRNode.LeftHand : XRNode.RightHand;
    }
}
