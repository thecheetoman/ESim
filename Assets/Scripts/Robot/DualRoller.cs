using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DualRoller : MonoBehaviour
{
    [Header("Target Mesh")]
    public Transform shaftMesh;

    [Header("Roller Settings")]
    public float rotationNormal = 1000f;
    [Tooltip("Set to -1 to reverse direction")]
    public float reverseDirection = 1;
    public float rotation2 = 500f;
    public Vector3 rotationAxis = Vector3.right;
    public KeyCode activeKey = KeyCode.Space;

    private float currentSpeed = 0f;
    private bool isEnabled = false;

    private void OnEnable()
    {
        GameManager.OnRobotStateChanged += OnRobotStateChanged;

        isEnabled = GameManager.IsRobotEnabled;
    }

    private void OnDisable()
    {
        GameManager.OnRobotStateChanged -= OnRobotStateChanged;
    }

    private void OnRobotStateChanged(bool enabled)
    {
        isEnabled = enabled;

        if (!isEnabled)
        {
            currentSpeed = 0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isEnabled || shaftMesh == null) return;

        if (Input.GetKey(activeKey))
        {
            currentSpeed = rotationNormal;
        }
        else
        {
            currentSpeed = rotation2 * reverseDirection;
        }

        shaftMesh.Rotate(rotationAxis * currentSpeed * Time.deltaTime);
    }
}