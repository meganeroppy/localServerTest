using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManagerTest : NetworkBehaviour 
{
	private NetworkManager nManager;

    public enum Role
    {
        Server,
        Host,
        Client,
    }

	[SerializeField]
	private Role role;

    /// <summary>
    /// 後々動的に設定できるようにする
    /// </summary>
    [SerializeField]
    private string localServerAddress;

    [SerializeField]
    private bool autoExecRole = false;

   // Use this for initialization
    void Start()
    {
        nManager = GetComponent<NetworkManager>();

        // 何か文字列が入っていたらサーバアドレスを上書き
        if (!string.IsNullOrEmpty(localServerAddress))
        {
            nManager.networkAddress = localServerAddress;
        }

        if (autoExecRole) ExecRole();
	}
	
	// Update is called once per frame
	void Update ()
    {
        CheckInput();
	}

    private void CheckInput()
    {
        if( Input.GetKeyDown( KeyCode.T ) )
        {
            ExecRole();
        }
    }

    public void ExecRole()
    {
        switch( role )
        {
            case Role.Server:
                nManager.StartServer();
                break;
            case Role.Host:
                nManager.StartHost();
                break;
            case Role.Client:
            default:
                nManager.StartClient();
                break;            
        }
    }
}
