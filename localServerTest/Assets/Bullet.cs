using UnityEngine;
using UnityEngine.Networking;

public class Bullet : NetworkBehaviour
{
    public float lifeTime = 10f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnDestroy()
    {
        NetworkServer.Destroy(gameObject);
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