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
    private bool isObserverPrev = false;

	[SerializeField]
	private MeshRenderer observerSign;

    [SerializeField]
    private DrothySample drothyPrefab;
    private DrothySample drothy;

    [SerializeField]
    private GameObject bulletPrefab;

    [SerializeField]
    private GameObject mushroomPrefab;

    [SerializeField]
    private TextMesh textMesh;

    [SerializeField]
    private GameObject youIcon;

    [SyncVar]
    private string netIdStr;

    [SerializeField]
    private Transform holdPos;

    [SyncVar]
    private int eatCount = 0;

    [SyncVar]
    private float drothyScale = 1f;

    /// <summary>
    /// つかめる距離にあるアイテム
    /// </summary>
    private GameObject holdTarget = null;

    /// <summary>
    /// つかんでいるアイテム
    /// </summary>
    private Mushroom holdItem = null;



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

     //   if (isObserver)
     //   {
     //       gameObject.AddComponent<ObserverSample>();
     //       CmdSetObserverSign(isObserver);
     //   }

        CmdCreateDrothy();

        CmdSetNetIdStr();

        CmdSetIsObserver(NetworkManagerTest.instance.IsObserver);
    }

    [Command]
    private void CmdSetNetIdStr()
    {
        netIdStr = netId.Value.ToString();
    }

    public override void OnStartClient()
    {
        Debug.Log("OnStartClient" + "isObserver = " + isObserver.ToString() + " local= " + isLocalPlayer.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        //	textItem.text = ""+iItemCount;

        Debug.Log("isObserver = " + isObserver.ToString() + " local= " + isLocalPlayer.ToString());

        textMesh.text = netIdStr;

        if (isObserver != isObserverPrev)
        {
            observerSign.material.color = isObserver ? Color.red : Color.white;
        }

        isObserverPrev = isObserver;

        youIcon.SetActive(isLocalPlayer);

        if (holdItem)
        {
            holdItem.transform.position = holdPos.position;
            holdItem.CmdSetPosition(holdPos.position);
        }

        // ■ここから↓はローカルプレイヤーのみ■
        if (drothy != null)
        {
            drothy.transform.localScale = Vector3.one * drothyScale;
        }

        if ( !nTransform.isLocalPlayer )
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

        if (Input.GetKeyDown(KeyCode.Space) && isObserver)
        {
            CmdCreateMushroom();
        }

        if( holdTarget && !holdItem && Input.GetKeyDown(KeyCode.H))
        {
            holdItem = holdTarget.GetComponent<Mushroom>();
            holdTarget = null;
            Debug.Log("CmdSetHoldItemをコール");
            CmdSetHoldItem();
        }

        if (holdItem && Input.GetKeyDown(KeyCode.Y))
        {
            CmdEatItem();
        }

        if ( holdTarget && Vector3.Distance( transform.position, holdTarget.transform.position ) > 5f)
        {
            holdTarget = null;
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
        drothy = Instantiate(drothyPrefab);
        drothy.SetOwner(this.transform);

        NetworkServer.Spawn(drothy.gameObject);
    }

	[Command]
	private void CmdSetIsObserver( bool value )
	{
        isObserver = value;
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

    [Command]
    private void CmdCreateMushroom()
    {
        var obj = Instantiate(mushroomPrefab);
        obj.transform.position = transform.position;
        var mush = obj.GetComponent<Mushroom>();
        mush.CmdSetParent(this.gameObject);

        NetworkServer.Spawn(obj);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isObserver) return;

        if( other.tag.Equals("Item") )
        {
            holdTarget = other.gameObject;
        }
    }

    [Command]
    private void CmdSetHoldItem()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());
        holdItem = holdTarget.GetComponent<Mushroom>();
        holdTarget = null;
    }


    [Command]
    private void CmdEatItem( )
    {
        if (!holdItem) return;

        NetworkServer.Destroy(holdItem.gameObject);
        eatCount++;

        if( eatCount >= 3 && !biggenFlag )
        {
            biggenFlag = true;
            ChangeScale();
        }
    }

    [SyncVar]
    private bool biggenFlag = false;


    [Server]
    private void ChangeScale()
    {
        drothyScale = 10f;
    }
}