using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PlayerTest : NetworkBehaviour
{
	private Rigidbody myRigidbody;
    public float moveSpeed = 10f;
    public float rotSpeed = 10f;
    private NetworkView netView = null;
	public int iItemCount = 0;
	private NetworkTransform nTransform = null;

	[SyncVar]
	private bool isObserver = false;

	[SerializeField]
	private MeshRenderer observerSign;

    [SerializeField]
    private DrothySample drothyPrefab;

    [SerializeField]
    private GameObject bulletPrefab;

    private void Awake()
    {
        Debug.Log("Awake" + "isObserver = " + isObserver.ToString() + " local= " + isLocalPlayer.ToString());
    }

    // Use this for initialization
    void Start () 
	{
		myRigidbody = GetComponent<Rigidbody>();
		netView = GetComponent<NetworkView>();
		nTransform = GetComponent<NetworkTransform>();

        if( !isLocalPlayer )        
		{
			// カメラ無効
			GetComponent<Camera>().enabled = false;
		}
	}

    public override void OnStartLocalPlayer()
    {
        Debug.Log("OnStartLocalPlayer" + "isObserver = " + isObserver.ToString() + " local= " + isLocalPlayer.ToString());
        isObserver = NetworkManagerTest.instance.IsObserver;
        if (isObserver)
        {
            gameObject.AddComponent<ObserverSample>();
            CmdSetObserverSign(isObserver);
        }

        CmdCreateDrothy();

    }

    public override void OnStartClient()
    {
        Debug.Log("OnStartClient" + "isObserver = " + isObserver.ToString() + " local= " + isLocalPlayer.ToString());
    }

    // Update is called once per frame
    void Update ()
    {
        //	textItem.text = ""+iItemCount;

        Debug.Log("isObserver = " + isObserver.ToString() + " local= " + isLocalPlayer.ToString());

		if( !nTransform.isLocalPlayer )
		{
			return;
		}
		Vector3 move = new Vector3(
			Input.GetAxisRaw("Horizontal"),
            0,
			Input.GetAxisRaw("Vertical"));

        transform.Translate( move * moveSpeed * Time.deltaTime);
        //	myRigidbody.velocity = (transform.forward + move.normalized) * fSpeed ;


        var rot = Input.GetKey(KeyCode.I) ? -1 : Input.GetKey(KeyCode.O) ? 1 : 0;
        if( rot != 0 )
        {
            transform.Rotate(Vector3.up * rot * rotSpeed * Time.deltaTime);
        }

        if( Input.GetKeyDown( KeyCode.Space ) )
        {
            CmdFire();
        }
    }

	[RPC]//
	void GetItem(int add) {
		iItemCount += add;
		netView.RPC("SetItemCount", RPCMode.OthersBuffered, iItemCount);
	}

	[RPC]
//	[Command]
	void SetItemCount(int ic) {
		iItemCount = ic;
	}

    [Command]
    private void CmdCreateDrothy()
    {
        var drothy = Instantiate(drothyPrefab);

        drothy.SetOwner(this.transform);

        NetworkServer.Spawn(drothy.gameObject);
    }

	[Command]
	private void CmdSetObserverSign( bool isObserver )
	{
        // TODO ローカルだけしか赤くならないぞ？なんとかしろ

        if( isObserver )
    		observerSign.material.color = Color.red;
	}

    [Command]
    private void CmdFire()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());

        var obj = Instantiate(bulletPrefab);
        obj.transform.position = transform.position;
        obj.GetComponent<Rigidbody>().AddForce(transform.forward * 80f);
        obj.GetComponent<MeshRenderer>().material.color = observerSign.material.color;

        NetworkServer.Spawn(obj);
    }
}