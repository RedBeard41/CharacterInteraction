using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{

    PlayerInput _playerInput;
    CharacterController _characterController;
    Animator _animator;
    Vector2 _currentMovementInput;
    Vector3 _currentMovement;

    Vector3 _appliedMovement;
    bool _isMovementPressed;
    bool _isRunPressed;
    bool _isJumpPressed;

    bool _isJumping;

    int _walkSpeed;
    int _runSpeed;

    int _isWalkingHash;
    int _isRunningHash;

    int _isDancingHash;
    int _isJumpingHash;
    int _jumpCountHash;

    bool _isJumpAnimating;
    float _rotationFactorPerFrame = 15.0f;

    bool _isDancePressed;

    float _gravity = -9.8f;
    float _groundedGravity = -9.8f;

    float _initialJumpVelocity;
    float _maxJumpHeight;
    float _maxJumpTime;

    int _jumpCount;
    Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> _jumpGravities = new Dictionary<int, float>();
    Coroutine currentJumpResetRoutine;

    void Awake()
    {
        _playerInput = new PlayerInput();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _isWalkingHash = Animator.StringToHash("isWalking");
        _isRunningHash = Animator.StringToHash("isRunning");
        _isDancingHash = Animator.StringToHash("isDancing");
        _isJumpingHash = Animator.StringToHash("isJumping");
        _jumpCountHash = Animator.StringToHash("jumpCount");
        _isJumping = false;
        _isJumpAnimating = false;
        _jumpCount = 0;
        _walkSpeed = 2;
        _runSpeed = 5;
        _currentMovement = transform.position;
        _playerInput.CharacterControls.Move.started += OnMovementInput;
        _playerInput.CharacterControls.Move.canceled += OnMovementInput;
        _playerInput.CharacterControls.Move.performed += OnMovementInput;

        _playerInput.CharacterControls.Run.started += OnRun;
        _playerInput.CharacterControls.Run.canceled += OnRun;

        _playerInput.CharacterControls.Dance.started += OnDance;
        _playerInput.CharacterControls.Dance.canceled += OnDance;

        _playerInput.CharacterControls.Jump.started += OnJump;
        _playerInput.CharacterControls.Jump.canceled += OnJump;

        _maxJumpTime = 0.75f;
        _maxJumpHeight = 2f;
        currentJumpResetRoutine = null;
        SetupJumpVariables();
    }

    // Start is called before the first frame update


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        HandleRotation();

        _characterController.Move(_appliedMovement * Time.deltaTime);
    }

    void OnMovementInput(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
        _isMovementPressed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
    }

    void OnDance(InputAction.CallbackContext context)
    {
        _isDancePressed = context.ReadValueAsButton();

    }

    void OnRun(InputAction.CallbackContext context)
    {
        _isRunPressed = context.ReadValueAsButton();


    }

    void OnJump(InputAction.CallbackContext context)
    {

        _isJumpPressed = context.ReadValueAsButton();
    }

    void SetupJumpVariables()
    {
        float timeToApex = _maxJumpTime / 2;
        _gravity = (-2 * _maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        _initialJumpVelocity = (2 * _maxJumpHeight) / timeToApex;
        float secondJumpGravity = (-2 * (_maxJumpHeight + 1)) / Mathf.Pow(timeToApex * 1.25f, 2);
        float secondJumpInitialVelocity = (2 * (_maxJumpHeight + 1)) / timeToApex * 1.25f;
        float thirdJumpGravity = (-2 * (_maxJumpHeight + 1)) / Mathf.Pow(timeToApex * 1.5f, 2);
        float thirdJumpInitialVelocity = (2 * (_maxJumpHeight + 1)) / timeToApex * 1.5f;

        _initialJumpVelocities.Add(1, _initialJumpVelocity);
        _initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        _initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        _jumpGravities.Add(0, _gravity);
        _jumpGravities.Add(1, _gravity);
        _jumpGravities.Add(2, secondJumpGravity);
        _jumpGravities.Add(3, thirdJumpGravity);

    }

    void HandleRotation()
    {
        Vector3 positionToLookAt;
        positionToLookAt.x = _currentMovement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = _currentMovement.z;

        Quaternion currentRotation = transform.rotation;

        if (_isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, _rotationFactorPerFrame * Time.deltaTime);
        }
    }

    void OnEnable()
    {
        _playerInput.CharacterControls.Enable();
    }

    void OnDisable()
    {
        _playerInput.CharacterControls.Disable();
    }
}
