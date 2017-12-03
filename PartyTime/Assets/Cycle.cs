using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cycle : MonoBehaviour {
    public string[] choices = new string[] {
        "Shark",
        "Eiffel Tower",
        "Dick"
    };

    public string text = "";
	// Use this for initialization
	void Start () {
        this.text = "Draw a " + choices[0] + "!!!!";
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
