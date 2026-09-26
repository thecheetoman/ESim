using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TurretStatus : MonoBehaviour
{
    public TextMeshProUGUI turretStatusText;
    public Turret Rturret;

    // Start is called before the first frame update 
    void Start()
    {
    }

    // Update is called once per frame 
    void Update()
    {
        if (Rturret.mode == Turret.TurretMode.Manual)
        {
            turretStatusText.text = "manual aim";
        }
        else
        {
            turretStatusText.text = "auto aim\n(hub)";
        }
    }
}
