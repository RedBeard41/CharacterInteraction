using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationAndMovementController : MonoBehaviour
{
    PlayerInput _playerInput;
    CharacterController _characterController;
    Animator _animator;
    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _currentRunMovement;

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
        _playerInput.CharacterControls.Move.started += onMovementInput;
        _playerInput.CharacterControls.Move.canceled += onMovementInput;
        _playerInput.CharacterControls.Move.performed += onMovementInput;

        _playerInput.CharacterControls.Run.started += onRun;
        _playerInput.CharacterControls.Run.canceled += onRun;

        _playerInput.CharacterControls.Dance.started += onDance;
        _playerInput.CharacterControls.Dance.canceled += onDance;

        _playerInput.CharacterControls.Jump.started += onJump;
        _playerInput.CharacterControls.Jump.canceled += onJump;

        _maxJumpTime = 0.75f;
        _maxJumpHeight = 2f;
        currentJumpResetRoutine = null;
        setupJumpVariables();
    }
    void onMovementInput(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
        _currentMovement.x = _currentMovementInput.x * _walkSpeed;
        _currentMovement.z = _currentMovementInput.y * _walkSpeed;
        _currentRunMovement.x = _currentMovementInput.x * _runSpeed;
        _currentRunMovement.z = _currentMovementInput.y * _runSpeed;

        _isMovementPressed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
    }

    void onDance(InputAction.CallbackContext context)
    {
        _isDancePressed = context.ReadValueAsButton();

    }

    void onRun(InputAction.CallbackContext context)
    {
        _isRunPressed = context.ReadValueAsButton();


    }

    void onJump(InputAction.CallbackContext context)
    {

        _isJumpPressed = context.ReadValueAsButton();
    }

    void setupJumpVariables()
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

    void handleJump()
    {

        if (!_isJumping && _characterController.isGrounded && _isJumpPressed)
        {
            if (_jumpCount < 3 && currentJumpResetRoutine != null)
            {
                StopCoroutine(currentJumpResetRoutine);
            }
            _animator.SetBool(_isJumpingHash, true);
            _isJumpAnimating = true;
            _jumpCount += 1;
            _animator.SetInteger(_jumpCountHash, _jumpCount);

            _isJumping = true;
            _currentMovement.y = _initialJumpVelocities[_jumpCount];
            _appliedMovement.y = _initialJumpVelocities[_jumpCount];
        }
        else if (!_isJumpPressed && _isJumping && _characterController.isGrounded)
        {
            _isJumping = false;
        }
    }

    IEnumerator jumpResetRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        _jumpCount = 0;
    }
    void handleGravity()
    {
        bool isFalling = _currentMovement.y <= 0.0f || !_isJumpPressed;
        float fallMuliplier = 2.0f;
        if (_characterController.isGrounded)
        {
            if (_isJumpAnimating)
            {
                _animator.SetBool(_isJumpingHash, false);
                _isJumpAnimating = false;
                currentJumpResetRoutine = StartCoroutine(jumpResetRoutine());
                if (_jumpCount == 3)
                {
                    _jumpCount = 0;
                    _animator.SetInteger(_jumpCountHash, _jumpCount);
                }
            }
            _currentMovement.y = _groundedGravity;
            _appliedMovement.y = _groundedGravity;

        }
        else if (isFalling)
        {
            float previousYVelocity = _currentMovement.y;
            _currentMovement.y += (fallMuliplier * _jumpGravities[_jumpCount] * Time.deltaTime);
            _appliedMovement.y = (previousYVelocity + _currentMovement.y) * 0.5f;

        }
        else
        {
            // Debug.Log("Handling Gravity");
            // Velocity Verlet Integration
            float previousYVelocity = _currentMovement.y;
            _currentMovement.y += (_gravity * Time.deltaTime);
            _appliedMovement.y = (previousYVelocity + _currentMovement.y) * 0.5f;

            // _currentMovement.y += _gravity * Time.deltaTime;
            // _currentRunMovement.y += _gravity * Time.deltaTime;
        }
    }
    void handleRotation()
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

    void handleAnimation()
    {
        bool isWalking = _animator.GetBool(_isWalkingHash);
        bool isRunning = _animator.GetBool(_isRunningHash);
        bool isDancing = _animator.GetBool(_isDancingHash);
        bool _isJumping = _animator.GetBool("_isJumping");

        if (_isMovementPressed)
        {


            if (!isWalking)
            {

                _animator.SetBool(_isWalkingHash, true);

            }
            if (_isRunPressed)
            {
                _animator.SetBool(_isRunningHash, true);
            }
            else
            {
                _animator.SetBool(_isRunningHash, false);

            }
        }
        else if (!_isMovementPressed)
        {

            _animator.SetBool(_isWalkingHash, false);
            _animator.SetBool(_isRunningHash, false);

            if (_isDancePressed)
            {
                // Debug.Log("Let's dance");
                _animator.SetBool(_isDancingHash, true);
            }

            else
            {
                _animator.SetBool(_isDancingHash, false);
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        handleAnimation();
        handleRotation();

        if (_isRunPressed)
        {
            _appliedMovement.x = _currentRunMovement.x;
            _appliedMovement.z = _currentRunMovement.z;
        }
        else
        {
            _appliedMovement.x = _currentMovement.x;
            _appliedMovement.z = _currentMovement.z;
        }
        _characterController.Move(_appliedMovement * Time.deltaTime);
        handleGravity();
        handleJump();

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
