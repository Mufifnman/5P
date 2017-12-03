using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRTK;

public class Cycle : MonoBehaviour {
    public static string[] choices = new string[] {
        "Shark",
        "Eiffel Tower",
        "Dick"
    };

    public Text text;
    //public VRTK.UnityEventHelper.VRTK_Button_UnityEvents start_button;
    public VRTK_Button start_button;
    public VRTK_Button end_button;

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

        start_button.Pushed += this.start_button_pushed;
        end_button.Pushed += this.end_button_pushed;
	}

    void end_button_pushed(object sender, Control3DEventArgs args)
    {
        text.text = "GOOD JOB!!";
        start_button.gameObject.SetActive(true);
        end_button.gameObject.SetActive(false);
    }
	
    void start_button_pushed (object sender, Control3DEventArgs args)
    {
        string choice = choices[UnityEngine.Random.Range(0, choices.Length)];
        text.text = "Draw a " + choice + "!!!!";
        start_button.gameObject.SetActive(false);
        end_button.gameObject.SetActive(true);
    }

	// Update is called once per frame
	void Update () {
		
	}
}
