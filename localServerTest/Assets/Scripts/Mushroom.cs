using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Mushroom : NetworkBehaviour
{
    public static List<Mushroom> list;

    [SerializeField]
    private float lifeTime = 5f;
    private float timer = 0;

    [ServerCallback]
    private void Awake()
    {
        if( list == null )
        {
            list = new List<Mushroom>();
        }

        list.Add(this);
    }

    public GameObject parent
    {
        get; set;
    }

    [ServerCallback]
    private void Update()
    {
        timer += Time.deltaTime;
        if( timer >= lifeTime )
        {
            CmdRemove();
        }
    }


    [Command]
    public void CmdSetParent(GameObject obj )
    {
        parent = obj;
    }

    [Command]
    public void CmdRemove()
    {
        NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    private void OnDestroy()
    {
        if( list.Contains(this))
        {
            list.Remove(this);
        }
    }
}
