using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class move : MonoBehaviour {

    public Vector3 center;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKey(KeyCode.S))
        {
            int magnitude = 1;
            Vector3 dir = Vector3.Normalize(transform.position - center) * magnitude;
            transform.position = transform.position + dir;
        }
    }
}
