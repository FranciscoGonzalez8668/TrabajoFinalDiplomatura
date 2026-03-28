using UnityEngine;


[CreateAssetMenu(fileName = "PlayerData", menuName = "QerlkKeeper/PlayerData")]
public class PlayerData : ScriptableObject
{
    
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float acceleration = 20f;
    public float deceleration = 15f;

    [Header("Sprint")]
    public float sprintSpeed = 10f;
    public float sprintAcceleration = 8;


    [Header("Jump")]

    public float jumpHeight = 5f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.12f;
    public float wallJumpSpeed = 7f;
    public float wallJumpForce = 3f;
    public float wallJumpHeight = 1.5f;

    [Header("Gravity")]
    public float gravityScale = 2.5f;
    public float fallMultiplier = 1.5f;
    public float maxFallSpeed = 20f;


    [Header("Wall Run")]
    public float wallRunSpeed = 8f;
    public float wallRunDuration = 1.5f;
    public float wallDetectionDistance = 0.6f;
    public float wallRunGravity = 0.3f;
    public float minWallRunHeight = 0.5f;
    public float wallRunUpBoost = 3f;


    [Header("Time Global")]
    public float globalTimerDuration=120f;

}