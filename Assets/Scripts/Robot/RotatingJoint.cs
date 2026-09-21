using Robot.InputHandling;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingJoint : MonoBehaviour
{
    [Header("Hinge Settings")]
    [Tooltip("Local axis to rotate around (e.g. Vector3.right = local X).")]
    [SerializeField] private Vector3 rotationAxis = Vector3.right;
    [SerializeField] private float retractedAngle = 0f;
    [SerializeField] private float extendedAngle = 90f;
    [SerializeField] private float rotationSpeed = 5f;
    [Tooltip("Constant Euler angle offset (X, Y, Z) applied on top of the axis rotation. Useful when the object's rest orientation isn't zero, or to fine-tune alignment without changing the two target angles.")]
    [SerializeField] private Vector3 angleOffset = Vector3.zero;

    [Header("Control Settings")]
    [Tooltip("Key that deploys/extends the arm.")]
    [SerializeField] private KeyCode deployKey = KeyCode.LeftShift;

    private float currentAngle;
    private bool isDeployed = false;

    void Start()
    {
        currentAngle = retractedAngle;
    }

    void Update()
    {
        bool deployPressed = Input.GetKeyDown(deployKey)
            || (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.DeployPressedThisFrame());

        if (deployPressed)
        {
            isDeployed = !isDeployed;
        }

        float targetAngle = isDeployed ? extendedAngle : retractedAngle;

        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, rotationSpeed * Time.deltaTime * 100f);

        transform.localRotation = Quaternion.Euler(angleOffset) * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }
}