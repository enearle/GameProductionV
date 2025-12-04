using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFlight : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 10f;
    [SerializeField] private float fastMovementSpeed = 20f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float smoothness = 0.1f;
    
    [SerializeField] PlayerInput playerInput;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float currentMovementSpeed;
    private bool isRotating;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting;
    private Vector2 verticalInput;
    private float currentXRotation = 0f;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction closeAction;

    void Awake()
    {
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        sprintAction = playerInput.actions["Sprint"];
        closeAction = playerInput.actions["Close"];
        

        targetPosition = transform.position;
        targetRotation = transform.rotation;
        currentMovementSpeed = movementSpeed;
    }

    void OnEnable()
    {
        lookAction.performed += OnLookPerformed;
        lookAction.canceled += OnLookCanceled;
        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;
        sprintAction.performed += OnSprintPerformed;
        sprintAction.canceled += OnSprintCanceled;
        closeAction.performed += OnClosePerformed;

    }

    private void OnClosePerformed(InputAction.CallbackContext obj)
    {
        Application.Quit();
    }

    void OnDisable()
    {
        lookAction.performed -= OnLookPerformed;
        lookAction.canceled -= OnLookCanceled;
        moveAction.performed -= OnMovePerformed;
        moveAction.canceled -= OnMoveCanceled;
        sprintAction.performed -= OnSprintPerformed;
        sprintAction.canceled -= OnSprintCanceled;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        isRotating = true;
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
        isRotating = false;
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        isSprinting = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
    }

    void Update()
    {
        HandleInput();
        UpdateMovement();
        UpdateRotation();
    }

    private void HandleInput()
    {
        currentMovementSpeed = isSprinting ? fastMovementSpeed : movementSpeed;

        // Movement input
        Vector3 moveDirection = new Vector3(moveInput.x, verticalInput.y, moveInput.y);
        moveDirection = transform.TransformDirection(moveDirection);
        targetPosition += moveDirection.normalized * (currentMovementSpeed * Time.deltaTime);

        // Rotation input
        if (isRotating)
        {
            // Update the accumulated X rotation
            currentXRotation -= lookInput.y * rotationSpeed;
            currentXRotation = Mathf.Clamp(currentXRotation, -89f, 89f);

            // Create rotation from scratch instead of modifying existing euler angles
            targetRotation = Quaternion.Euler(currentXRotation, 
                targetRotation.eulerAngles.y + lookInput.x * rotationSpeed, 
                0f);
        }
    }


    private void UpdateMovement()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - smoothness);
    }

    private void UpdateRotation()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - smoothness);
    }
}

