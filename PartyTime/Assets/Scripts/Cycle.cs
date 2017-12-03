using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cycle : MonoBehaviour {
    public static string[] choices = new string[] {
        "Shark",
        "Eiffel Tower",
        "Dick"
    };

    public Text text;
    public GameObject start_button;
    public GameObject end_button;

	// Use this for initialization
	void Start ()
    {
        if (text == null)
        {
            text = GetComponent<Text>();
        }

        if (text == null)
        {
            throw new InvalidOperationException("Cycle needs to have a Text object!!!");
        }

	}

    void end_button_pushed()
    {

    }
	
    void start_button_pushed ()
    {
        string choice = choices[UnityEngine.Random.Range(0, choices.Length)];
        text.text = "Draw a " + choice + "!!!!";
        start_button.SetActive(false);
        end_button.SetActive(true);
    }

	// Update is called once per frame
	void Update () {
		
	}
}
