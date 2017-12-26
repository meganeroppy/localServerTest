using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DrothySample : NetworkBehaviour {

    [SyncVar]
    private Transform owner;

	[SyncVar]
	private bool initialized = false;

	public void SetOwner( Transform owner )
    {
        this.owner = owner;
		initialized = true;
    }
	
	// Update is called once per frame
	void Update ()
    {
	    if( owner != null )
        {
            transform.SetPositionAndRotation(owner.position, owner.rotation);
        }
		else if( initialized )
		{
			// 初期化後にマスターがいなくなったら削除
			Destroy( gameObject );
		}
	}
}
