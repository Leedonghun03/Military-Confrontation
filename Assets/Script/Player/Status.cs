using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Status : MonoBehaviour
{
    [Header("Walk, Run, crouch Speed")]
    [SerializeField]
    private float walkSpeed;
    [SerializeField]
    private float runSpeed;
    [SerializeField]
    private float crouchSpeed;

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
    public float CrouchSpeed => crouchSpeed;
}
