using UnityEngine;

public class KillZone : MonoBehaviour
{
    [SerializeField] private bool destroyOnTrigger;

    private void OnTriggerEnter(Collider other)
    {
        PlayerRespawn playerRespawn = other.GetComponent<PlayerRespawn>();
        if (playerRespawn == null)
        {
            playerRespawn = other.GetComponentInParent<PlayerRespawn>();
        }

        if (playerRespawn == null)
        {
            return;
        }

        playerRespawn.Kill();

        if (destroyOnTrigger)
        {
            Destroy(gameObject);
        }
    }
}
