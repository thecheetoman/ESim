using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamController : MonoBehaviour
{
    public enum CameraMode { TPS, DS }
    public CameraMode currentMode = CameraMode.TPS;
    public KeyCode toggleModeKey = KeyCode.C;

    public Vector3 TPSoffset = new Vector3(0f, 5f, -10f);
    public Vector3 TPSangle = new Vector3(30f, 0f, 0f);

    public Transform dsTransform;

    private Transform target;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("CamController: no GameObject tagged 'Player' found.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleModeKey))
        {
            SetMode(currentMode == CameraMode.TPS ? CameraMode.DS : CameraMode.TPS);
        }
    }

    void LateUpdate()
    {
        switch (currentMode)
        {
            case CameraMode.TPS:
                UpdateTPS();
                break;
            case CameraMode.DS:
                UpdateDS();
                break;
        }
    }

    private void UpdateTPS()
    {
        if (target == null) return;

        // Force snap directly to the offset position and angle
        transform.position = target.position + TPSoffset;
        transform.rotation = Quaternion.Euler(TPSangle);
    }

    private void UpdateDS()
    {
        if (dsTransform == null) return;

        // Force snap directly to the DS transform target
        transform.position = dsTransform.position;
        transform.rotation = dsTransform.rotation;
    }

    public void SetMode(CameraMode mode)
    {
        currentMode = mode;
    }
}
