using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// relay script cuz fuck you unity
public class HubPanel : MonoBehaviour
{
    public HubSystem hub;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("GamePiece"))
        {
            hub.playSFX(collision);
        }
    }
}
