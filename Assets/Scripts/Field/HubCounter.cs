using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubCounter : MonoBehaviour
{
    public HubSystem hub;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GamePiece"))
        {
            bool wasLegal = false;
            if (other.TryGetComponent<ShotData>(out ShotData shotData))
            {
                wasLegal = shotData.wasShotFromLegalZone;
            }

            if (hub != null)
            {
                hub.countFuel(wasLegal);
            }
        }
    }
}