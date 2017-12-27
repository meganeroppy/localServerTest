using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObserverSample : MonoBehaviour
{
	// Use this for initialization
	void Start () {
        Debug.Log( gameObject.name + System.Reflection.MethodBase.GetCurrentMethod());
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
