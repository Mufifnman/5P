using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottleFanLogic : ImprovedSingletonBehavior<BottleFanLogic>
{
    public GameObject Bottle;
    public Rigidbody BottleRigidbody;
    public GameObject Tip;
    public float Offset = 0.3134243f;
    public float distanceFactor = 0.1f;
    public float forceFactor = 1.0f;

    protected override void InitializeInternal()
    {
    }

    void Start ()
    {
		
	}
	
    void FixedUpdate()
    {
        foreach(var id in InputManager.Instance.LastVelocity.Keys)
        {
            var vel = InputManager.Instance.LastVelocity[id];
            var pos = InputManager.Instance.LastPositions[id];

            var distance = Bottle.transform.position - pos;
            var velComponent = Vector3.Cross(distance.normalized, vel).magnitude;
            var distComponent = 1.0f/(distance * distanceFactor).sqrMagnitude;

            BottleRigidbody.AddForce(distance.normalized * velComponent * distComponent * forceFactor);
        }   
    }

	void Update ()
    {
		
	}

    public void ResetBottle()
    {
        var setPosition = Tip.transform.position;
        setPosition.y += Offset;
        Bottle.transform.position = setPosition;
        Bottle.transform.rotation = Quaternion.LookRotation(Vector3.forward, -Vector3.up);
    }
}
