using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.WSA.Input;

public class InputManager : ImprovedSingletonBehavior<InputManager>
{
    public enum InputMode
    {
        WindowsMR,
        Unity,
    }

    public GameObject[] HeldAssets;
    public InteractionSourceNode ControllerPose = InteractionSourceNode.Grip;
    public Transform RealWorldRoot;
    public InputMode CurrentInputMode = InputMode.Unity;

    public bool CanThrow;
    public float VelocityModifier = 0.8f;

    public float GripSensitivity = 0.2f;
    private float lastGripLeft = -1.0f;
    private float lastGripRight = -1.0f;

    private readonly Dictionary<uint, Transform> devices = new Dictionary<uint, Transform>();
    private readonly Dictionary<uint, int> modelIndecies = new Dictionary<uint, int>();
    private readonly Dictionary<uint, bool> isDetatched = new Dictionary<uint, bool>();

    protected override void InitializeInternal()
    {
        Physics.gravity = -Vector3.up * 9.8f * VelocityModifier;
    }

    // Use this for initialization
    void Start()
    {
        this.Initialize();

        if (HeldAssets.Length == 0)
        {
            throw new System.Exception("ThrowingSceneManager needs to have some throwable objects!");
        }

        if (CurrentInputMode == InputMode.WindowsMR)
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
        if (CurrentInputMode == InputMode.Unity)
        {
            UpdateUnityInput();
        }
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

        if (CanThrow)
        {
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
        }

        // Swap
        if (Input.GetButtonDown("MotionController-Menu-Left"))
        {
            this.TrySwitchHeldObject(NodeExtensions.LEFT_ID);
        }
        if (Input.GetButtonDown("MotionController-Menu-Right"))
        {
            this.TrySwitchHeldObject(NodeExtensions.RIGHT_ID);
        }
        if (Input.GetButtonDown("MotionController-TouchpadPressed-Left"))
        {
            this.TrySwitchHeldObject(NodeExtensions.LEFT_ID);
        }
        if (Input.GetButtonDown("MotionController-TouchpadPressed-Right"))
        {
            this.TrySwitchHeldObject(NodeExtensions.RIGHT_ID);
        }

        if (CanThrow)
        {
            // Throw
            if (Input.GetButtonUp("MotionController-Select-Left"))
            {
                this.TryThrow(XRNode.LeftHand);
            }
            if (Input.GetButtonUp("MotionController-Select-Right"))
            {
                this.TryThrow(XRNode.RightHand);
            }
            if (Input.GetAxis("MotionController-GraspedAmmount-Left") <= GripSensitivity && lastGripLeft > GripSensitivity)
            {
                this.TryThrow(XRNode.LeftHand);
            }
            if (Input.GetAxis("MotionController-GraspedAmmount-Right") <= GripSensitivity && lastGripRight > GripSensitivity)
            {
                this.TryThrow(XRNode.RightHand);
            }
        }

        lastGripLeft = Input.GetAxis("MotionController-GraspedAmmount-Left");
        lastGripRight = Input.GetAxis("MotionController-GraspedAmmount-Right");
    }

    private void InteractionManager_InteractionSourcePressed(InteractionSourcePressedEventArgs args)
    {
        uint id = args.state.source.id;
        if (args.pressType == InteractionSourcePressType.Menu || args.pressType == InteractionSourcePressType.Touchpad)
        {
            TrySwitchHeldObject(id);
        }
        else if (args.pressType == InteractionSourcePressType.Grasp || args.pressType == InteractionSourcePressType.Select)
        {
            if (CanThrow)
            {
                CreateNew(id);
            }
        }
    }

    private void TrySwitchHeldObject(uint id)
    {
        if (devices.ContainsKey(id))
        {
            int modelIndex = this.modelIndecies[id];
            RemoveDevice(id);
            AddDevice(id, ++modelIndex % HeldAssets.Length);
        }
        else if (modelIndecies.ContainsKey(id))
        {
            this.modelIndecies[id] = ++this.modelIndecies[id] % HeldAssets.Length;
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
        if ((args.pressType == InteractionSourcePressType.Grasp || args.pressType == InteractionSourcePressType.Select)
            && CanThrow)
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
            if (rigidbody.TryThrow(node, VelocityModifier))
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
            GameObject go = Instantiate(this.HeldAssets[index], this.RealWorldRoot);
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
        if (CurrentInputMode == InputMode.WindowsMR)
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
