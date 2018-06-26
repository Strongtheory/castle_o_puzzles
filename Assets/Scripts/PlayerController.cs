﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    [Header("Linked Components")]
    public InputManager input_manager;
    public CharacterController cc;
    public Collider WallRunCollider;
    [HideInInspector]
    public Camera player_camera;
    [Header("Movement constants")]
    public float maxSpeed;
    public float RunSpeed;
    public float AirSpeed;
    public float GroundAcceleration;
    public float AirAcceleration;
    public float SpeedDamp;
    public float AirSpeedDamp;
    public float SlideSpeed;
    public float DownGravityAdd;
    public float ShortHopGravityAdd;
    public float JumpVelocity;
    public float WallJumpThreshold;
    public float WallJumpBoost;
    public float WallRunLimit;
    public float WallRunJumpSpeed;
    public float WallClimbLimit;
    public Vector3 StartPos;

    // Jumping state variables
    private float JumpMeterSize;
    private float JumpMeter;
    private float SlideGracePeriod;
    private float SlideTimeDelta;
    private bool isHanging;
    private bool isJumping;
    private bool isFalling;
    private bool willJump;
    private float LandingTimeDelta;
    private float jumpGracePeriod;
    private float BufferJumpTimeDelta;
    private float BufferJumpGracePeriod;
    private float WallJumpTimeDelta;
    private float WallJumpGracePeriod;
    private float WallRunTimeDelta;
    private float WallRunGracePeriod;
    private float WallClimbTimeDelta;
    private float WallClimbGracePeriod;
    private float ReGrabTimeDelta;
    private float ReGrabGracePeriod;
    // Wall related variables
    private Vector3 WallJumpReflect;
    private Vector3 PreviousWallNormal;
    private Vector3 PreviousWallJumpNormal;
    private Vector3 WallAxis;
    private Vector3 AlongWallVel;
    private Vector3 UpWallVel;
    private float WallRunImpulse;
    private float WallRunSpeed;
    private float LedgeClimbOffset;
    private float LedgeClimbBoost;

    // Physics state variables
    private Vector3 current_velocity;
    private Vector3 accel;
    private ControllerColliderHit lastHit;
    private Collider lastTrigger;
    private ControllerColliderHit currentHit;
    private float GravityMult;

    // Use this for initialization
    private void Start () {
        // Movement values
        maxSpeed = 4;
        RunSpeed = 10;
        AirSpeed = 0.90f;
        GroundAcceleration = 20;
        AirAcceleration = 500;
        SpeedDamp = 10f;
        AirSpeedDamp = 0.01f;
        SlideSpeed = 12f;

        // Gravity modifiers
        DownGravityAdd = 0;
        ShortHopGravityAdd = 0;
        
        // Jump states/values
        JumpVelocity = 12f;
        WallJumpThreshold = 6f;
        WallJumpBoost = 1.0f;
        WallRunLimit = 4f;
        WallClimbLimit = 6f;
        WallRunJumpSpeed = 12f;
        WallRunImpulse = 3.0f;
        WallRunSpeed = 10.0f;
        isJumping = false;
        isFalling = false;
        willJump = false;
        // Wall related vars
        WallAxis = Vector3.zero;
        AlongWallVel = Vector3.zero;
        UpWallVel = Vector3.zero;
        WallJumpReflect = Vector3.zero;
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
        LedgeClimbOffset = 1.0f;
        LedgeClimbBoost = Mathf.Sqrt(2 * cc.height * 1.1f * Physics.gravity.magnitude);
        // Timers
        JumpMeterSize = 0.3f;
        JumpMeter = JumpMeterSize;
        jumpGracePeriod = 0.1f;
        LandingTimeDelta = jumpGracePeriod;
        BufferJumpGracePeriod = 0.1f;
        BufferJumpTimeDelta = BufferJumpGracePeriod;
        SlideGracePeriod = 0.2f;
        SlideTimeDelta = SlideGracePeriod;
        WallJumpGracePeriod = 0.2f;
        WallJumpTimeDelta = WallJumpGracePeriod;
        WallRunGracePeriod = 0.2f;
        WallRunTimeDelta = WallRunGracePeriod;
        WallClimbGracePeriod = 0.2f;
        WallClimbTimeDelta = WallClimbGracePeriod;
        ReGrabGracePeriod = 0.5f;
        ReGrabTimeDelta = ReGrabGracePeriod;

        // Initial state
        current_velocity = Vector3.zero;
        currentHit = new ControllerColliderHit();
        StartPos = transform.position;

        Physics.IgnoreCollision(WallRunCollider, cc);
    }

    // Fixed Update is called once per physics tick
    private void FixedUpdate () {
        // Get starting values
        GravityMult = 1;
        accel = Vector3.zero;
        
        ProcessHits();
        ProcessTriggers();
        HandleMovement();
        HandleJumping();

        // Update character state based on desired movement
        if (!OnGround())
        {
            accel += Physics.gravity * GravityMult;
        }
        else
        {
            // Push the character controller into the normal of the surface
            // This should trigger ground detection
            accel += -Mathf.Sign(currentHit.normal.y) * Physics.gravity.magnitude * currentHit.normal;
        }
        current_velocity += accel * Time.deltaTime;
        Vector3 previous_position = transform.position;
        cc.Move(current_velocity * Time.deltaTime);

        if ((cc.velocity - current_velocity).magnitude > 100.0f)
        {
            Debug.Log("Detected large error in velocity... Aborting move");
            Debug.Log("Previous position: " + previous_position.ToString());
            Debug.Log("Current position: " + transform.position.ToString());
            Debug.Log("Current cc velocity: " + cc.velocity.magnitude.ToString());
            Debug.Log("Current velocity: " + current_velocity.magnitude.ToString());
            Debug.Log("Velocity error: " + (current_velocity - cc.velocity).ToString());
            Debug.Log("WallJumpReflect: " + WallJumpReflect.ToString());
            Debug.Log("Accel: " + accel.ToString());
            StartCoroutine(DeferedTeleport(previous_position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f), 0.5f)));
        }

        // Increment timers
        JumpMeter = Mathf.Clamp(JumpMeter + Time.deltaTime, 0, JumpMeterSize);
        LandingTimeDelta = Mathf.Clamp(LandingTimeDelta + Time.deltaTime, 0, 2 * jumpGracePeriod);
        SlideTimeDelta = Mathf.Clamp(SlideTimeDelta + Time.deltaTime, 0, 2 * SlideGracePeriod);
        BufferJumpTimeDelta = Mathf.Clamp(BufferJumpTimeDelta + Time.deltaTime, 0, 2 * BufferJumpGracePeriod);
        WallJumpTimeDelta = Mathf.Clamp(WallJumpTimeDelta + Time.deltaTime, 0, 2 * WallJumpGracePeriod);
        WallRunTimeDelta = Mathf.Clamp(WallRunTimeDelta + Time.deltaTime, 0, 2 * WallRunGracePeriod);
        WallClimbTimeDelta = Mathf.Clamp(WallClimbTimeDelta + Time.deltaTime, 0, 2 * WallClimbGracePeriod);
        ReGrabTimeDelta = Mathf.Clamp(ReGrabTimeDelta + Time.deltaTime, 0, 2 * ReGrabGracePeriod);
    }

    private void ProcessTriggers()
    {
        if (lastTrigger == null)
        {
            return;
        }

        if (!OnGround())
        {
            RaycastHit hit;
            Boolean hit_wall = false;
            if (IsWallRunning() || IsWallClimbing())
            {
                // Scan toward the wall normal
                if (Physics.Raycast(transform.position, -PreviousWallNormal, out hit, 2.0f))
                {
                    hit_wall = true;
                }
            }
            else
            {
                // Scan forward and sideways to find a wall
                if (Physics.Raycast(transform.position, transform.right, out hit, 2.0f))
                {
                    hit_wall = true;
                }
                else if (Physics.Raycast(transform.position, -transform.right, out hit, 2.0f))
                {
                    hit_wall = true;
                }
                else if (Physics.Raycast(transform.position + transform.up * (cc.height / 2 - 0.5f), transform.forward, out hit, 2.0f))
                {
                    hit_wall = true;
                }
            }
            // Update my current state based on my scan results
            if (hit_wall && hit.normal.y > -0.17f && hit.normal.y <= 0.34f) 
            {
                UpdateWallConditions(hit.normal);
            }
            if (IsWallClimbing() && !isHanging)
            {
                // Scan for ledge
                Vector3 LedgeScanPos = transform.position + (transform.up * cc.height / 2) + LedgeClimbOffset * transform.forward;
                if (Physics.Raycast(LedgeScanPos, -transform.up, out hit, LedgeClimbOffset))
                {
                    if (CanGrabLedge() && Vector3.Dot(hit.normal, Physics.gravity) < -0.866f)
                    {
                        isHanging = true;
                    }
                }
                // If all ledge climb conditions are met, climb it to the surface on top
                // and clear all wall conditions
            }
        }

        lastTrigger = null;
    }

    private void UpdateWallConditions(Vector3 wall_normal)
    {
        if (Vector3.Dot(Vector3.ProjectOnPlane(current_velocity, Physics.gravity), wall_normal) < -WallJumpThreshold)
        {
            // Are we jumping in a new direction (atleast 20 degrees difference)
            if (Vector3.Dot(PreviousWallJumpNormal, wall_normal) < 0.94f)
            {
                WallJumpTimeDelta = 0;
                WallJumpReflect = Vector3.Reflect(current_velocity, wall_normal);
                if (BufferJumpTimeDelta < BufferJumpGracePeriod)
                {
                    // Buffer a jump
                    willJump = true;
                }
                PreviousWallJumpNormal = wall_normal;
            }
        }
        WallAxis = Vector3.Cross(wall_normal, Physics.gravity).normalized;
        AlongWallVel = Vector3.Dot(current_velocity, WallAxis) * WallAxis;
        UpWallVel = current_velocity - AlongWallVel;
        // First attempt a wall run if we pass the limit and are looking along the wall. 
        // If we don't try to wall climb instead if we are looking at the wall.
        // Debug.Log("Previous Wall: " + PreviousWallNormal + ", Wall Normal: " + wall_normal.ToString());
        if (AlongWallVel.magnitude > WallRunLimit && Mathf.Abs(Vector3.Dot(wall_normal, transform.forward)) < 0.866f && Vector3.Dot(AlongWallVel, transform.forward) > 0)
        {
            if (IsWallRunning() || Vector3.Dot(PreviousWallNormal, wall_normal) < 0.94f)
            {
                if (!IsWallRunning())
                {
                    if (AlongWallVel.magnitude < WallRunSpeed)
                    {
                        current_velocity = UpWallVel + Mathf.Sign(Vector3.Dot(current_velocity, WallAxis)) * WallRunSpeed * WallAxis;
                    }
                    current_velocity.y = Math.Max(current_velocity.y + WallRunImpulse, WallRunImpulse);
                }
                WallRunTimeDelta = 0;
            }
        }
        else if (Vector3.Dot(transform.forward, -wall_normal) >= 0.866f)
        {

            WallClimbTimeDelta = 0;
            //Debug.DrawRay(transform.position, wall_normal, Color.blue, 10);
        }
        PreviousWallNormal = wall_normal;
    }

    private void ProcessHits()
    {
        if (lastHit == null)
        {
            return;
        }
        // Save the most recent last hit
        currentHit = lastHit;

        if (currentHit.normal.y > 0.6f)
        {
            ProcessFloorHit();
        }
        else if (currentHit.normal.y > 0.34f)
        {
            ProcessSlideHit();
        }
        else if (currentHit.normal.y > -0.17f)
        {
            ProcessWallHit();
        }
        else
        {
            ProcessCeilingHit();
        }
        // Keep velocity in the direction of the plane if the plane is not a ceiling
        // Or if it is a ceiling only cancel out the velocity if we are moving fast enough into its normal
        if (Vector3.Dot(currentHit.normal, Physics.gravity) < 0 || Vector3.Dot(current_velocity, currentHit.normal) < -1f)
        {
            current_velocity = Vector3.ProjectOnPlane(current_velocity, currentHit.normal);
        }

        if (currentHit.gameObject.tag == "Respawn")
        {
            StartCoroutine(DeferedTeleport(StartPos));
        }
        // Set last hit null so we don't process it again
        lastHit = null;
    }

    private void ProcessFloorHit()
    {
        // On the ground
        LandingTimeDelta = 0;

        // Handle buffered jumps
        if (BufferJumpTimeDelta < BufferJumpGracePeriod)
        {
            // Buffer a jump
            willJump = true;
        }
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
    }

    private void ProcessSlideHit()
    {
        // Slides
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
    }

    private void ProcessWallHit()
    {
        if (!OnGround())
        {
            UpdateWallConditions(currentHit.normal);
        }
    }

    private void ProcessCeilingHit()
    {
        // Overhang
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
    }

    // Apply movement forces from input (FAST edition)
    private void HandleMovement()
    {
        // If we are hanging stay still
        if (isHanging)
        {
            current_velocity = Vector3.zero;
            GravityMult = 0;
            return;
        }
        Vector3 planevelocity;
        Vector3 movVec = (input_manager.GetMoveVertical() * transform.forward +
                          input_manager.GetMoveHorizontal() * transform.right);
        float movmag = movVec.magnitude < 0.8f ? movVec.magnitude : 1f;
        // Do this first so we cancel out incremented time from update before checking it
        if (!OnGround())
        {
            // We are in the air (for atleast LandingGracePeriod). We will slide on landing if moving fast enough.
            SlideTimeDelta = 0;
            planevelocity = current_velocity;
        }
        else
        {
            planevelocity = Vector3.ProjectOnPlane(current_velocity, currentHit.normal);
        }
        // Normal ground behavior
        if (OnGround() && !willJump && (SlideTimeDelta >= SlideGracePeriod || planevelocity.magnitude < SlideSpeed))
        {
            // If we weren't fast enough we aren't going to slide
            SlideTimeDelta = SlideGracePeriod;
            // Use character controller grounded check to be certain we are actually on the ground
            movVec = Vector3.ProjectOnPlane(movVec, currentHit.normal);
            AccelerateTo(movVec, RunSpeed*movmag, GroundAcceleration);
            accel += -current_velocity * SpeedDamp;
        }
        // We are either in the air, buffering a jump, or sliding (recent contact with ground). Use air accel.
        else
        {
            // Handle wall movement and return early
            if (IsWallRunning())
            {
                float away_from_wall_speed = Vector3.Dot(current_velocity, PreviousWallNormal);
                // Only remove velocity if we are attempting to move away from the wall
                if (away_from_wall_speed > 0)
                {
                    // Remove the component of the wall normal velocity that is along the gravity axis
                    float gravity_resist = Vector3.Dot(away_from_wall_speed * PreviousWallNormal, Physics.gravity.normalized);
                    float previous_velocity_mag = current_velocity.magnitude;
                    current_velocity -= (away_from_wall_speed * PreviousWallNormal - gravity_resist * Physics.gravity.normalized);
                    // consider adding a portion of the lost velocity back along the wall axis
                    current_velocity += WallAxis * Mathf.Sign(Vector3.Dot(current_velocity, WallAxis)) * (previous_velocity_mag - current_velocity.magnitude);
                }
                if (Vector3.Dot(UpWallVel, Physics.gravity) >= 0)
                {
                    GravityMult = 0.25f;
                }
            }
            else
            {
                AccelerateTo(movVec, AirSpeed * movmag, AirAcceleration);
            }
            accel += -Vector3.ProjectOnPlane(current_velocity, transform.up) * AirSpeedDamp;
        }
    }

    // Try to accelerate to the desired speed in the direction specified
    private void AccelerateTo(Vector3 direction, float desiredSpeed, float acceleration)
    {
        direction.Normalize();
        float moveAxisSpeed = Vector3.Dot(current_velocity, direction);
        float deltaSpeed = desiredSpeed - moveAxisSpeed;
        if (deltaSpeed < 0)
        {
            // Gotta go fast
            return;
        }

        // Scale acceleration by speed because we want to go fast
        deltaSpeed = Mathf.Clamp(acceleration * Time.deltaTime * desiredSpeed, 0, deltaSpeed);
        current_velocity += deltaSpeed * direction;
    }

    // Handle jumping
    private void HandleJumping()
    {
        // Ground detection for friction and jump state
        if (OnGround())
        {
            isJumping = false;
            isFalling = false;
        }

        // Add additional gravity when going down (optional)
        if (current_velocity.y < 0)
        {
            GravityMult += DownGravityAdd;
        }

        // Handle jumping and falling
        if (input_manager.GetJump())
        {
            BufferJumpTimeDelta = 0;
            if (OnGround() || CanWallJump() || IsWallRunning() || isHanging)
            {
                DoJump();
            }
        }
        if (willJump)
        {
            DoJump();
        }
        // Fall fast when we let go of jump (optional)
        if (isFalling || isJumping && !input_manager.GetJumpHold())
        {
            GravityMult += ShortHopGravityAdd;
            isFalling = true;
        }
    }

    // Double check if on ground using a separate test
    private bool OnGround()
    {
        return (LandingTimeDelta < jumpGracePeriod);
    }

    private bool IsWallRunning()
    {
        return (WallRunTimeDelta < WallRunGracePeriod);
    }

    private bool IsWallClimbing()
    {
        return (WallClimbTimeDelta < WallClimbGracePeriod);
    }

    private bool CanGrabLedge()
    {
        return (ReGrabTimeDelta >= ReGrabGracePeriod);
    }

    private bool CanWallJump()
    {
        return (WallJumpTimeDelta < WallJumpGracePeriod);
    }

    // Set the player to a jumping state
    private void DoJump()
    {
        // Wall jump if we need to
        if (!isHanging)
        {
            if (CanWallJump() && WallJumpReflect.magnitude > 0)
            {
                //Debug.Log("Wall Jump");
                current_velocity += (WallJumpReflect - current_velocity) * WallJumpBoost * (JumpMeter / JumpMeterSize);
            }
            else if (IsWallRunning())
            {
                //Debug.Log("Wall Run Jump");
                current_velocity += PreviousWallNormal * WallRunJumpSpeed * (JumpMeter / JumpMeterSize);
            }
            if (OnGround() || willJump || CanWallJump() || IsWallRunning())
            {
                //Debug.Log("Upward Jump");
                current_velocity.y = Math.Max(current_velocity.y + JumpVelocity * (JumpMeter / JumpMeterSize), JumpVelocity);
            }
        }
        else
        {
            current_velocity.y = LedgeClimbBoost;
        }
        JumpMeter = 0;
        ReGrabTimeDelta = 0;
        isJumping = true;
        willJump = false;
        isHanging = false;

        // Intentionally set the timers over the limit
        BufferJumpTimeDelta = BufferJumpGracePeriod;
        WallJumpTimeDelta = WallJumpGracePeriod;
        WallRunTimeDelta = WallRunGracePeriod;
        WallClimbTimeDelta = WallClimbGracePeriod;
        LandingTimeDelta = jumpGracePeriod;
        WallJumpReflect = Vector3.zero;
    }

    private void OnTriggerStay(Collider other)
    {
        lastTrigger = other;
    }

    // Handle collisions on player move
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        lastHit = hit;
    }


    // Teleport coroutine (needed due to bug in character controller teleport)
    IEnumerator DeferedTeleport(Vector3 position)
    {
        yield return new WaitForEndOfFrame();
        transform.position = position;
    }
}
