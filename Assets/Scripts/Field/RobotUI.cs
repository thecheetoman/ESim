using RobotFramework.Controllers.Drivetrain;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotUI : MonoBehaviour
{
    public Transform robot;
    public Transform turret;
    public GameObject turretSprite;

    public float offsetRobot = 0f;
    public float turretOffset = 90f;
    [Header("sweve visaulziation")]
    public SwerveWheel FR;
    public SwerveWheel FL;
    public SwerveWheel RR;
    public SwerveWheel RL;
    public GameObject FRS;
    public GameObject FLS;
    public GameObject RRS;
    public GameObject RLS;

    void Update()
    {
        float robotY = robot.eulerAngles.y;
        if (robot != null)
        {
            // Get the Y rotation angle from the target object

            // Apply it to the 2D object's Z axis (multiplied if needed)
            transform.rotation = Quaternion.Euler(0f, 0f, -robotY + offsetRobot);
        }
        if (turret != null)
        {
            float turretY = turret.eulerAngles.y;
            turretSprite.transform.rotation = Quaternion.Euler(0f, 0f, -turretY + turretOffset);
        }
        float sOffset = -robotY+offsetRobot;
        FRS.transform.rotation = Quaternion.Euler(0f, 0f, -FR.wheelAngle + sOffset);
        FLS.transform.rotation = Quaternion.Euler(0f, 0f, -FL.wheelAngle + sOffset);
        RRS.transform.rotation = Quaternion.Euler(0f, 0f, -RR.wheelAngle + sOffset);
        RLS.transform.rotation = Quaternion.Euler(0f, 0f, -RL.wheelAngle + sOffset);

    }
}
