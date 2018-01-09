using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerSampleTracked : NetworkBehaviour {

	[SerializeField]
	private CopyTransform[] objs;

	// Use this for initialization
	void Start () {
		foreach( CopyTransform c in objs )
		{
			if( isLocalPlayer )
			{
				c.copySource = GameObject.Find(  "Tracker_" + c.gameObject.name );
			}
			else
			{
				c.enabled = false;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
