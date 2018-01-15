using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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

    [SyncVar]
    private int currentSceneIndex = 0;

    [SerializeField]
    private bool forceBehaveLikePlayer = false;

    [SyncVar]
    private NetworkInstanceId drothyNetId;

    /// <summary>
    /// つかめる距離にあるアイテム
    /// </summary>
    private Mushroom holdTarget = null;

    /// <summary>
    /// つかんでいるアイテム
    /// </summary>
    private Mushroom holdItem = null;

    [SerializeField]
    private string[] sceneNameList;

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
            var camera = GetComponentInChildren<Camera>();
            if (camera.gameObject) camera.gameObject.SetActive(false);
		}
	}

    public override void OnStartLocalPlayer()
    {
        Debug.Log("OnStartLocalPlayer" + "isObserver = " + isObserver.ToString() + " local= " + isLocalPlayer.ToString());

        CmdCreateDrothy();

        CmdSetNetIdStr();

        CmdSetIsObserver(NetworkManagerTest.instance.IsObserver);

        if(NetworkManagerTest.instance.IsObserver)
        {
            var obj = GameObject.Find("BaseSceneManager");
            if( obj )
            {
                var manager = obj.GetComponent<BaseSceneManager>();
                if( manager != null )
                {
                    manager.ActivatePresetCameras();
                }
            }
        }
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
 //           holdItem.GetComponent<NetworkIdentity>().AssignClientAuthority( connectionToClient );
//            CmdUpdateHoldItemPosition();
        }

        if (isClient)
        {
            if (drothy == null)
            {
                var obj = ClientScene.FindLocalObject(drothyNetId);
                if( obj )
                {
                    drothy = obj.GetComponent<DrothySample>();
                }
            }

            if (drothy != null)
            {
                drothy.transform.localScale = Vector3.one * drothyScale;
            }
        }
        // ■ここから↓はローカルプレイヤーのみ■

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

        if (Input.GetKeyDown(KeyCode.N) && isObserver)
        {
            CmdGotoNextScene();
        }

        if ( holdTarget && !holdItem && Input.GetKeyDown(KeyCode.H))
        {
            CmdSetHoldItem();
        }

        if (holdItem && Input.GetKeyDown(KeyCode.Y))
        {
            CmdEatItem();
        }

        if ( holdTarget && Vector3.Distance( transform.position, holdTarget.transform.position ) > 5f)
        {
            holdTarget = null;
            CmdReleaseHoldTarget();
        }
    }

    [Command]
    private void CmdCreateDrothy()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());

        drothy = Instantiate(drothyPrefab);
        drothy.SetOwner(this.transform);

//        NetworkServer.Spawn(drothy.gameObject);
        NetworkServer.SpawnWithClientAuthority(drothy.gameObject, gameObject);

        drothyNetId = drothy.netId;

        RpcPassDrothyReference(drothy.netId);
    }

    [ClientRpc]
    private void RpcPassDrothyReference( NetworkInstanceId netId )
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());

        var obj = ClientScene.FindLocalObject(netId);

        drothy = obj.GetComponent<DrothySample>();
    }

    [Command]
    private void CmdRequestDrothyReference()
    {
        if (drothy == null) return;
        RpcPassDrothyReference(drothy.netId);
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
        obj.GetComponent<MeshRenderer>().material.color = isObserver ? Color.red : Color.white;


        NetworkServer.Spawn(obj);
    }

    /// <summary>
    /// きのこ配置
    /// </summary>
    [Command]
    private void CmdCreateMushroom()
    {
        var obj = Instantiate(mushroomPrefab);
        obj.transform.position = transform.position;
        var mush = obj.GetComponent<Mushroom>();
        mush.CmdSetParent(this.gameObject);

        NetworkServer.Spawn(obj);
    }

    /// <summary>
    /// ローカルでのみ判定する
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());

        if ((isObserver && !forceBehaveLikePlayer) || !isLocalPlayer ) return;

        if (other.tag.Equals("Item"))
        {
            var mush = other.GetComponent<Mushroom>();
            if (mush != null)
            {
                holdTarget = mush;
                CmdSetHoldTarget(mush.netId);
            } 
        }
    }

    [Command]
    private void CmdSetHoldTarget( NetworkInstanceId id )
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());

        var obj = NetworkServer.FindLocalObject(id);
        if (!obj) return;
        var mush = obj.GetComponent<Mushroom>();
        if( mush != null )
        {
            holdTarget = mush;
        }
    }

    [Command]
    private void CmdReleaseHoldTarget()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());

        holdTarget = null;
    }

    [Command]
    private void CmdSetHoldItem()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());
        if (!holdTarget) return;

        holdItem = holdTarget.GetComponent<Mushroom>();

        holdTarget = null;

        // つかんでいるプレイヤーに権限を与える
        var nIdentity = holdItem.GetComponent<NetworkIdentity>();
        if (nIdentity != null && !nIdentity.hasAuthority)
        {
            nIdentity.AssignClientAuthority(connectionToClient);
        }

        RpcSetHoldItem();
    }

    [ClientRpc]
    private void RpcSetHoldItem()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());
        if (!holdTarget) return;

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

    [Command]
    private void CmdUpdateHoldItemPosition()
    {
        holdItem.CmdSetPosition(holdPos.position);
    }

    [Command]
    private void CmdGotoNextScene()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());

        currentSceneIndex++;
        RpcGotoNextScene(currentSceneIndex, true);
    }

    [ClientRpc]
    private void RpcGotoNextScene( int newSceneIndex, bool allowLoadSameScene )
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod());

        if (currentSceneIndex != newSceneIndex || allowLoadSameScene)
        {
            currentSceneIndex = newSceneIndex;

            SceneManager.LoadScene(sceneNameList[currentSceneIndex % sceneNameList.Length], LoadSceneMode.Additive);

            if (currentSceneIndex >= 1)
            {
            SceneManager.UnloadSceneAsync(sceneNameList[(currentSceneIndex - 1) % sceneNameList.Length]);
            }
        }
    }
}