using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DrothySample : NetworkBehaviour {

    [SyncVar]
    private Transform owner;
	public void SetOwner( Transform owner )
    {
        this.owner = owner;
    }
	
	// Update is called once per frame
	void Update ()
    {
	    if( owner != null )
        {
            transform.SetPositionAndRotation(owner.position, owner.rotation);
        }
	}
}
