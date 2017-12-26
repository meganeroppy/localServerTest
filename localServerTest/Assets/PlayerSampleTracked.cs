using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSampleTracked : MonoBehaviour {

	[SerializeField]
	private CopyTransform[] objs;

	// Use this for initialization
	void Start () {
		foreach( CopyTransform c in objs )
		{
			c.copySource = GameObject.Find(  "Tracker_" + c.gameObject.name );
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
