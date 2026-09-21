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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(activeKey))
        {
            currentSpeed = rotationNormal;
        }
        else
        {
            currentSpeed = rotation2*reverseDirection;
        }
        shaftMesh.Rotate(rotationAxis * currentSpeed * Time.deltaTime);
    }
}
