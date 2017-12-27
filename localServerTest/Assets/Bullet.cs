using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 10f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
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