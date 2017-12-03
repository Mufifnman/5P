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

    public float time = 0;
    public bool timerOn = false;

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
        Debug.Log("triggered end");
        text.text = "GOOD JOB!!";
        start_button.gameObject.SetActive(true);
        end_button.gameObject.SetActive(false);
        timerOn = false;
    }
	
    void start_button_pushed (object sender, Control3DEventArgs args)
    {
        Debug.Log("triggered start");
        string choice = choices[UnityEngine.Random.Range(0, choices.Length)];
        text.text = "Draw a " + choice + "!!!!";
        start_button.gameObject.SetActive(false);
        end_button.gameObject.SetActive(true);
        time = 0;
        timerOn = true;
    }

	// Update is called once per frame
	void Update ()
    {
		if (timerOn)
        {
            time += Time.deltaTime;
        }
	}

    void OnDestroy()
    {
        start_button.Pushed -= this.start_button_pushed;
        end_button.Pushed -= this.end_button_pushed;
    }
}
