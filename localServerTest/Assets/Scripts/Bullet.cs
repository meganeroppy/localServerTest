using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Bullet : NetworkBehaviour
{
    public float lifeTime = 10f;

    [ServerCallback]
    private void Start()
    {
        //    Destroy(gameObject, lifeTime);
        StartCoroutine(WaitAndDestroy());
    }
    
    private IEnumerator WaitAndDestroy()
    {
        yield return new WaitForSeconds(lifeTime);

        NetworkServer.Destroy(gameObject);
    }
    
    [ServerCallback]
    private void OnDestroy()
    {
    //    NetworkServer.Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
	{
		var hit = collision.gameObject;
		var hitCombat = hit.GetComponent<Combat>();
		if (hitCombat != null)
		{
			hitCombat.TakeDamage(10);
			Destroy(gameObject);
		}
	}
}