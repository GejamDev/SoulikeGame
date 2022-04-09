using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody rb;
    Transform cam;

    [Header("Variables")]
    public Animator an;

    [Header("Camera Settings")]

    public float cameraSensitivity;
    public float cameraDistance;
    public float minCamRot;
    public float maxCamRot;

    [Header("Movement Settings")]
    public bool alwaysRun;
    public float walkSpeed;
    public float runSpeed;

    [Header("Rotation Settings")]
    public float rotateSpeed;

    [Header("Jump Settings")]
    public Transform feetPos;
    public LayerMask groundLayer;
    public float groundCheckDistance;
    public float jumpForce;

    [Header("Climb Settings")]
    public LayerMask ladderLayer;
    public float ladderCheckDistance;
    public Transform ladderCheckPosition;
    public float climbSpeed;
    public float climbDownSpeed;

    [Header("Roll Settings")]
    public float rollTime;
    public float rollCoolTime;

    [Header("States")]
    public float curCameraX;

    public Vector2 moveInput;
    public Vector3 currentRightDirection;
    public Vector3 currentForwardDirection;

    public bool grounded;
    public bool climbing;
    public bool climbingDown;
    public bool stickToLadder;
    public Vector3 ladderCheckDirection;

    public bool rolling;
    public bool rollReloaded;



    public void Awake()
    {

        //get variables

        rb = GetComponent<Rigidbody>();



        cam = Camera.main.transform;



        //set variables
        rollReloaded = true;
    }

    public void Update()
    {
        MoveAxis();
        MoveCamera();
        Rotate();
        Animate();
        CheckGrounded();
        JumpMovement();
        RollMovement();
    }
    public void MoveAxis()
    {
        Vector2 preMoveInput = moveInput;
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));





        bool preClimbingState = climbing;
        bool preClimbingDownState = climbingDown;

        if (!stickToLadder)
        {
            ladderCheckDirection = currentForwardDirection;
        }
        bool preStickToLadderState = stickToLadder;

        stickToLadder = Physics.Raycast(ladderCheckPosition.position, ladderCheckDirection.normalized, ladderCheckDistance, ladderLayer);
        climbing = false;
        climbingDown = false;
        if (stickToLadder)
        {
            climbing = moveInput.y == 1;
            climbingDown = moveInput.y == -1;
        }
        else
        {
            if (preStickToLadderState)
            {
                //fall
                FallOffOfClimb();
            }
        }



        ResetForwardDirection();

        //move
        Vector3 move_horizontal = currentRightDirection * Input.GetAxisRaw("Horizontal");
        Vector3 move_vertical = (climbing || climbingDown) ? Vector3.zero : currentForwardDirection * Input.GetAxisRaw("Vertical");

        float speed = (Input.GetKey(KeyCode.LeftShift) || alwaysRun) ? runSpeed : walkSpeed;

        Vector3 move = (move_horizontal + move_vertical).normalized * speed;




        if (stickToLadder)
        {
            if (climbing)
            {
                rb.velocity = new Vector3(move.x, climbSpeed, move.z);
            }
            else if (climbingDown)
            {
                rb.velocity = new Vector3(move.x, -climbDownSpeed, move.z);
            }
            else
            {
                rb.velocity = new Vector3(move.x, 0, move.z);
            }
        }
        else
        {
            rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);
        }
    }
    public void ResetForwardDirection()
    {
        currentRightDirection = new Vector3(cam.right.x, 0, cam.right.z).normalized;
        currentForwardDirection = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
    }
    public void MoveCamera()
    {
        float xAxis = Input.GetAxis("Mouse X");
        float yAxis = -Input.GetAxis("Mouse Y");
        cam.eulerAngles += new Vector3(0, xAxis, 0) * cameraSensitivity * Time.deltaTime;
        curCameraX += -Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
        curCameraX = Mathf.Clamp(curCameraX, minCamRot, maxCamRot);
        cam.eulerAngles = new Vector3(curCameraX, cam.eulerAngles.y, cam.eulerAngles.z);

        cam.position = transform.position - cam.forward.normalized * cameraDistance;

        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward.normalized, out hit, cameraDistance, groundLayer))
        {
            cam.position = transform.position - cam.forward.normalized * (Vector3.Distance(transform.position, hit.point) - 0.5f);
        }
    }
    public void Rotate()
    {
        if (moveInput == Vector2.zero)
            return;
        Vector2 input = stickToLadder ? new Vector2(ladderCheckDirection.x, ladderCheckDirection.z) : new Vector2(rb.velocity.x, rb.velocity.z);
        float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(0, angle, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }
    public void Animate()
    {
        an.SetBool("walking", moveInput != Vector2.zero && !Input.GetKey(KeyCode.LeftShift) && !stickToLadder && !alwaysRun);
        an.SetBool("running", moveInput != Vector2.zero && (Input.GetKey(KeyCode.LeftShift) || alwaysRun) && !stickToLadder);
        an.SetBool("idle", moveInput == Vector2.zero && !stickToLadder);
        an.SetBool("climbing", stickToLadder);
        an.SetFloat("climbSpeed", climbing ? 1 : (climbingDown ? -1 : 0));
    }

    public void CheckGrounded()
    {
        bool preGroundedState = grounded;
        grounded = Physics.OverlapBox(feetPos.position, new Vector3(0.25f, 0.5f, 0.25f), Quaternion.identity, groundLayer).Length != 0;
        if (grounded && rb.velocity.y < 0)
        {
            EndJump();
        }
    }

    public void JumpMovement()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            StartJump();
        }
    }

    public void StartJump()
    {
        an.ResetTrigger("endJump");
        an.SetTrigger("jump");
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    public void EndJump()
    {
        an.ResetTrigger("jump");
        an.SetTrigger("endJump");
    }
    public void FallOffOfClimb()
    {
        an.ResetTrigger("endJump");
        an.SetTrigger("jump");
    }

    public void RollMovement()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && rollReloaded)
        {
            Roll();
        }
    }

    public void Roll()
    {
        rolling = true;
        rollReloaded = false;
        an.SetTrigger("roll");
        Invoke(nameof(EndRoll), rollTime);
        Invoke(nameof(ReloadRoll), rollCoolTime);
    }
    public void EndRoll()
    {
        rolling = false;
        an.SetTrigger("endRoll");
    }
    public void ReloadRoll()
    {

        rollReloaded = true;
    }
}
