using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
public class PlayerController : MonoBehaviour
{
    private PlayerControls playerControls;
    public bool isInSinglePlayerTestMode, canMove, amMasterPlayer, amLocalPlayer, isLevelExited;
    [SerializeField] private GameController gameController;
    [SerializeField] private GameObject cameraHolder, canvasForUI, countdownUI;
    [SerializeField] private Transform orientation;
    [SerializeField] private Camera cam;
    [SerializeField] private float mouseSensitivity, walkSpeed, sprintSpeed, jumpForce, smoothTime, wallDistance, minimumJumpHeight, wallRunGravity, wallRunJumpForce, fov, wallRunFOV, wallRunFOVTime, camTilt, camTiltTime;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private int countdownTime = 3;
    private float Tilt {get; set;}
    [SerializeField] private bool grounded;
    private RaycastHit leftWallHit, rightWallHit;
    private bool wallLeft, wallRight, wallForward = false;
    private float verticalLookRotation;
    private float moveSpeed = 5;
    private Vector3 moveAmount;
    private Vector3 smoothMoveVelocity;
    private Rigidbody rb;
    private PhotonView PV;
    public bool inWallRun;
    [Header("Timer")]
    [SerializeField]
    private TMP_Text timerText;
    public GameObject timerUI;
    private float elapsedTime;
    private bool timerGoing = false;
    private TimeSpan timePlaying;
    public int totalSeconds;
    public string timePlayingStr;
    private void Awake()
    {
        if(!isInSinglePlayerTestMode)
        {
            gameController = GameObject.Find("GameController").GetComponent<GameController>();
        }
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody>();
        if(isInSinglePlayerTestMode)
        {
            canMove = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }
        PV = GetComponent<PhotonView>();
        if(PV.IsMine)
        {
            if(PhotonNetwork.IsMasterClient)
            {
                amMasterPlayer = true;
            }
            amLocalPlayer = true;
            return;
        }
        else
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            Destroy(canvasForUI);
        }
    }
    private void Update()
    {
        if(canMove == false)
        {
            return;
        }
        if(!isInSinglePlayerTestMode)
        {
            if(!PV.IsMine)
            {
                return;
            }
            if(gameObject.transform.position.y <= -20f)
            {
                gameController.RestartLevel();
            }
        }
        Look();
        if (!grounded)
        {
            WallRun();
        }
        Move();
        if(!inWallRun)
        {
            CheckForJumpInput();
        }
    }
    private void WallRun()
    {
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallDistance);
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallDistance);
        wallForward = Physics.Raycast(transform.position, orientation.forward, wallDistance);
        if (CanWallRun())
        {
            if (wallLeft)
            {
                StartWallRun();
            }
            else if (wallRight)
            {
                StartWallRun();
            }
            else
            {
                StopWallRun();
            }
        }
        else
        {
            StopWallRun();
        }
    }
    private bool CanWallRun()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minimumJumpHeight);
    }
    private void StartWallRun()
    {
        inWallRun = true;
        rb.useGravity = false;
        rb.AddForce(Vector3.down * wallRunGravity, ForceMode.Force);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, wallRunFOV, wallRunFOVTime * Time.deltaTime);
        if (wallLeft)
        {
            Tilt = Mathf.Lerp(Tilt, -camTilt, camTiltTime * Time.deltaTime);
        }
        else if(wallRight)
        {
            Tilt = Mathf.Lerp(Tilt, camTilt, camTiltTime * Time.deltaTime);
        }
        playerControls.Movement.Jump.performed += _ => WallRunJump();
    }
    private void WallRunJump()
    {
        if(inWallRun)
        {
            if (wallLeft)
            {
                Vector3 wallRunJumpDirection = transform.up + leftWallHit.normal;
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(wallRunJumpDirection * wallRunJumpForce, ForceMode.Force);
            }
            else if (wallRight)
            {
                Vector3 wallRunJumpDirection = transform.up + rightWallHit.normal;
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(wallRunJumpDirection * wallRunJumpForce, ForceMode.Force);
            }
        }
    }
    private void StopWallRun()
    {
        inWallRun = false;
        rb.useGravity = true;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, wallRunFOVTime * Time.deltaTime);
        Tilt = Mathf.Lerp(Tilt, 0, camTiltTime);
    }
    private void Look()
    {
        transform.Rotate((Vector3.up * playerControls.Movement.LookX.ReadValue<float>()) * mouseSensitivity);
        verticalLookRotation += playerControls.Movement.LookY.ReadValue<float>() * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
        cameraHolder.transform.Rotate(Vector3.forward * Tilt);
    }
    private void Move()
    {
        playerControls.Movement.Sprint.started += _ => moveSpeed = sprintSpeed;
        playerControls.Movement.Sprint.canceled += _ => moveSpeed = walkSpeed;
        Vector3 moveDir = new Vector3((playerControls.Movement.GroundMovement.ReadValue<Vector2>().x), 0, (playerControls.Movement.GroundMovement.ReadValue<Vector2>().y)).normalized;
        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * moveSpeed, ref smoothMoveVelocity, smoothTime);
    }
    private void CheckForJumpInput()
    {
        playerControls.Movement.Jump.performed += _ => Jump();
    }
    private void Jump()
    {
        if (grounded && !inWallRun)
        {
            rb.AddForce(transform.up * jumpForce);
        }
    }
    public void SetGroundedState(bool _grounded)
    {
        grounded = _grounded;
    }
    private void FixedUpdate()
    {
        if(!isInSinglePlayerTestMode)
        {
            if(!PV.IsMine)
            {
                return;
            }
        }
        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount)* Time.fixedDeltaTime);
    }
    public void BeginCountdown()
    {
        StartCoroutine(CountdownToStart());
    }
    private IEnumerator CountdownToStart()
    {
        while(countdownTime > 0)
        {
            countdownText.text = countdownTime.ToString();
            yield return new WaitForSecondsRealtime(1f);
            countdownTime--;
        }
        canMove = true;
        gameController.StartGame();
        countdownText.text = "Go!";
        timerText.text = "00 : 00.00";
        timerUI.SetActive(true);
        timerGoing = true;
        StartCoroutine(Timer());
        yield return new WaitForSeconds(.5f);
        countdownUI.SetActive(false);
    }
    private IEnumerator Timer()
    {
        while(timerGoing)
        {
            elapsedTime += Time.deltaTime;
            timePlaying = TimeSpan.FromSeconds(elapsedTime);
            timePlayingStr = timePlaying.ToString("mm':'ss'.'ff");
            totalSeconds = (int)timePlaying.TotalSeconds;
            timerText.text = timePlayingStr;
            yield return null;
        }
    }
    public void StopTimer()
    {
        timerGoing = false;
        timerUI.SetActive(false);
    }
    public void ExitLevel()
    {
        canMove = false;
        isLevelExited = true;
    }
    private void OnEnable()
    {
        playerControls.Enable();
    }
    private void OnDisable()
    {
        playerControls.Disable();
    }
}