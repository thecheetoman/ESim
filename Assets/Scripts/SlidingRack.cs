using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlidingRack : MonoBehaviour
{
    [Header("Position Settings")]
    [Tooltip("Local position when the intake is deployed/extended.")]
    [SerializeField] private Vector3 extendedPosition = Vector3.zero;

    [Tooltip("Local position when the intake is stowed/retracted.")]
    [SerializeField] private Vector3 retractedPosition = new Vector3(0f, -0.00065f, -0.00307f);

    [Tooltip("Lower values make it slide smoother/slower. Try 5 to 15.")]
    [SerializeField] private float lerpSpeed = 10f;

    [Header("Control Settings")]
    [SerializeField] private KeyCode intakeKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode retractKey = KeyCode.LeftControl;

    private bool isDeployed = false;

    private void Start()
    {
        transform.localPosition = retractedPosition;
    }

    private void Update()
    {
        // Once shift is pressed, deploy permanently
        if (Input.GetKeyDown(intakeKey))
        {
            isDeployed = true;
        }
        else if (Input.GetKeyDown(retractKey))
        {
            isDeployed = false;
        }

        Vector3 targetPosition = isDeployed ? extendedPosition : retractedPosition;

        // Smoothly interpolate position frame by frame
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * lerpSpeed);
    }
}