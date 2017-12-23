using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManagerTest : MonoBehaviour 
{
	private NetworkManager nManager;
	[SerializeField]
	private bool isServer = false;
	// Use this for initialization
	void Start () {
		nManager = GetComponent<NetworkManager>();
		if( isServer )
		{
			nManager.StartServer();
		}
		else
		{
			nManager.StartClient();
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
