using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class updatetext : MonoBehaviour {

    public Text text;
    public Something script;
	// Use this for initialization
	void Start () {
        script = transform.getParent.getComponent("cycle");
	}
	
	// Update is called once per frame
	void Update () {
        text.Text = script.text;

	}
}
