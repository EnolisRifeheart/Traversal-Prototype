using UnityEngine;

public class Hazard : MonoBehaviour
{
    public Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody playerRB = other.GetComponent<Rigidbody>();

            if (playerRB != null)
            {

                playerRB.linearVelocity = Vector3.zero;
                playerRB.angularVelocity = Vector3.zero;


                    playerRB.position = respawnPoint.position;

                    playerRB.rotation = respawnPoint.rotation;
            }


        }

    }

}
