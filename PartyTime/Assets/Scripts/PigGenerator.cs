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
        currentPig = Instantiate<GameObject>(PigPrefab);
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    void OnClicked()
    {
        //aSSIGn CURRENT PIG TO Hands 
        // InteracitonManger.Instance.clickedHand.SetObject(currentPig);

        currentPig = Instantiate<GameObject>(PigPrefab);
    }
}
