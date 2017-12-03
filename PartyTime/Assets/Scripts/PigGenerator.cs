using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PigGenerator : MonoBehaviour
{
    public GameObject PigPrefab;
    private GameObject currentPig;

	// Use this for initialization
	void Start ()
    {
        for (int i = 0; i < 200; i++)
        {
            currentPig = Instantiate<GameObject>(PigPrefab, this.transform);
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
	}

    void OnClicked()
    {
        //aSSIGn CURRENT PIG TO Hands 
        // InteracitonManger.Instance.clickedHand.SetObject(currentPig);
        Debug.Log("detected a click");
        currentPig = Instantiate<GameObject>(PigPrefab);
    }
}
