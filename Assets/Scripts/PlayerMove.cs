/*
 * Basic player movement system.
 * Horizontal movement, jumping, sprinting, crouching.
 * Should be added to player controller object.
*/

// Imports
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    
    // Horizontal movement values, walking, running, and crouching, respectively
    [SerializeField] private string horizontalInputName;
    [SerializeField] private string verticalInputName;

    [SerializeField] private float walkSpeed, sprintSpeed, crouchSpeed;
    [SerializeField] private float sprintLerpSpeed;
    [SerializeField] private KeyCode sprintKey;

    [SerializeField] private float crouchLerpSpeed;
    [SerializeField] private KeyCode crouchKey;

    // Constant for default walk speed
    private float movementSpeed;

    // Values for slope control
    [SerializeField] private float slopeForce;
    [SerializeField] private float slopeForceRayLength;

    // Player controller, obviously
    private CharacterController charController;

    // Physics values for jumping
    [SerializeField] private AnimationCurve jumpFallOff;
    [SerializeField] private float jumpMultiplier;
    [SerializeField] private KeyCode jumpKey;

    // Bools for checking if player is in air or crouching
    private bool inAir;
    protected bool crouched;

    // Values related to double jumping
    private const int MAX_JUMP = 2;
    private int currentJump;

    // Get player object controller component
    private void Awake()
    {
        charController = GetComponent<CharacterController>();
    }

    // Check for movement input every frame
    private void Update()
    {
        PlayerMovement();
    }

    // Basic horizontal movement, checks for jumps, sprints, and crouches too
    private void PlayerMovement()
    {
        float horizInput = Input.GetAxis(horizontalInputName);
        float vertInput = Input.GetAxis(verticalInputName);

        Vector3 forwardMovement = transform.forward * vertInput;
        Vector3 rightMovement = transform.right * horizInput;

        // Prevent diagonal walking being faster than regular walking
        charController.SimpleMove(Vector3.ClampMagnitude(forwardMovement + rightMovement, 1.0f) * movementSpeed);

        // If player is on a slope, apply a downward force
        if ((vertInput != 0 || horizInput != 0) && OnSlope())
            charController.Move(Vector3.down * charController.height / 2 * slopeForce * Time.deltaTime);

        SetMovementSpeed();
        JumpInput();
    }

    // Lerp between walking, sprinting, and crouching movement speeds
    private void SetMovementSpeed()
    {
        if (Input.GetKey(sprintKey))
            movementSpeed = Mathf.Lerp(movementSpeed, sprintSpeed, Time.deltaTime * sprintLerpSpeed);
        else if (Input.GetKey(crouchKey))
        {
            movementSpeed = Mathf.Lerp(movementSpeed, crouchSpeed, Time.deltaTime * crouchLerpSpeed);
            charController.height = 1.0f;
            crouched = true;
        }
        else
        {
            movementSpeed = Mathf.Lerp(movementSpeed, walkSpeed, Time.deltaTime * sprintLerpSpeed);
            charController.height = 2.0f;
        }
    }

    // Detect if player is on a sloped surface
    private bool OnSlope()
    {
        if (inAir)
            return false;

        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, charController.height / 2 * slopeForceRayLength))
            if (hit.normal != Vector3.up)
            {
                print("OnSlope");
                return true;
            }
        return false;
    }

    // Jump if player inputs jump key
    private void JumpInput()
    {
        if (Input.GetKeyDown(jumpKey) && (!inAir || MAX_JUMP > currentJump))
        {
            inAir = true;
            StartCoroutine(JumpEvent());
        }
    }

    // Coroutine for jumping
    private IEnumerator JumpEvent()
    {
        // Change slope limit to allow mounting convex right angles while in air
        charController.slopeLimit = 90.0f;
        float timeInAir = 0.0f;
        currentJump++;
        do
        {
            float jumpForce = jumpFallOff.Evaluate(timeInAir);
            if (currentJump < 1)
                charController.Move(Vector3.up * jumpForce * jumpMultiplier * Time.deltaTime);
            else
            {
                charController.Move(Vector3.up * jumpForce * jumpMultiplier * Time.deltaTime);
            }
            timeInAir += Time.deltaTime;
            yield return null;
        } while (!charController.isGrounded && charController.collisionFlags != CollisionFlags.Above);

        charController.slopeLimit = 45.0f;
        inAir = false;
        currentJump = 0;
    }
}