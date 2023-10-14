using System;
using System.Collections.Generic;
using UnityEngine;

public class HumanoidLandController : MonoBehaviour
{
    public Transform CameraFollow;
    Rigidbody _rigidbody = null;
    CapsuleCollider _capsuleCollider = null;
    [SerializeField] HumanoidLandInput _input;
    [SerializeField] CameraController _cameraController;

    [SerializeField] Vector3 _playerMoveInput = Vector3.zero;

    Vector3 _playerLookInput = Vector3.zero;
    Vector3 _previousPlayerLookInput = Vector3.zero;
    float _cameraPitch = 0.0f;
    [SerializeField] float _playerLookInputLerpTime = 0.35f;

    [Header("Movement")]
    [SerializeField] float _movementMultiplier = 30.0f;
    [SerializeField] float _notGroundedMovementMultiplier = 1.25f;
    [SerializeField] float _rotationSpeedMultiplier = 180.0f;
    [SerializeField] float _pitchSpeedMultiplier = 180.0f;
    [SerializeField] float _runMultiplier = 2.5f;

    [Header("Ground Check")]
    [SerializeField] bool _playerIsGrounded = true;
    [SerializeField] [Range(0.0f, 1.8f)] float _groundCheckRadiusMultiplier = 0.9f;
    [SerializeField] [Range(-0.95f, 1.05f)] float _groundCheckDistanceTolerance = 0.05f;
    [SerializeField] float _playerCenterToGroundDistance = 0.0f;
    RaycastHit _groundCheckHit = new RaycastHit();

    [Header("Gravity")]
    [SerializeField] float _gravityFallCurrent = 0.0f;
    [SerializeField] float _gravityFallMin = 0.0f;
    [SerializeField] float _gravityFallIncrementTime = 0.05f;
    [SerializeField] float _playerFallTimer = 0.0f;
    [SerializeField] float _gravityGrounded = -1.0f;
    [SerializeField] float _maxSlopeAngle = 47.5f;

    [Header("Stairs")]
    [SerializeField] [Range(0.0f, 1.0f)] float _maxStepHeight = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] float _minStepDepth = 0.3f;
    [SerializeField] float _stairHeightPaddingMultiplier = 1.5f;
    [SerializeField] bool _isFirstStep = true;
    [SerializeField] float _firstStepVelocityDistanceMultiplier = 0.1f;
    [SerializeField] bool _playerIsAscendingStairs = false;
    [SerializeField] bool _playerIsDescendingStairs = false;
    [SerializeField] float _ascendingStairsMovementMultiplier = 0.35f;
    [SerializeField] float _descendingStairsMovementMultiplier = 0.75f;
    [SerializeField] float _maximumAngleOfApproachToAscend = 45.0f;
    float _playerHalfHeightToGround = 0.0f;
    float _maxAscendRayDistance = 0.0f; //Calculated in Awake() and is based off _maxStepHeight and a max approach angle of _maximumAngleOfApproachToAscend
    float _maxDescendRayDistance = 0.0f; // Calculated in Awake(), and is based off _maxStepHeight and a max departure angle of 80*
    int _numberOfStepDetectRays = 0;
    float _rayIncrementAmount = 0.0f;   

    [Header("Jumping")]
    [SerializeField] float _maxSlopeJumpAngle = 60.0f;
    [SerializeField] float _initialJumpForceMultiplier = 750.0f;
    [SerializeField] float _continualJumpForceMultiplier = 0.1f;
    [SerializeField] float _jumpTime = 0.175f;
    [SerializeField] float _jumpTimeCounter = 0.0f;
    [SerializeField] float _coyoteTime = 0.15f;
    [SerializeField] float _coyoteTimeCounter = 0.0f;
    [SerializeField] float _jumpBufferTime = 0.2f;
    [SerializeField] float _jumpBufferTimeCounter = 0.0f;
    [SerializeField] bool _playerIsJumping = false;
    [SerializeField] bool _jumpWasPressedLastFrame = false;

    [Header("Just For Fun")]
    [SerializeField] float _jumpReactionForceMultiplier = 0.75f;
    RaycastHit _lastGroundCheckHit = new RaycastHit();
    Vector3 _playerMoveInputAtLastGroundCheckHit = Vector3.zero;


    private void Awake()
    {
        //quick fix for weird missing mouse input issue in editor due to excessive framerate
        #if UNITY_EDITOR
        QualitySettings.vSyncCount = 0;  // VSync must be disabled
        Application.targetFrameRate = 60;
        #endif
        
        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();

        _maxAscendRayDistance = _maxStepHeight / Mathf.Cos(_maximumAngleOfApproachToAscend * Mathf.Deg2Rad);
        _maxDescendRayDistance = _maxStepHeight / Mathf.Cos(80.0f * Mathf.Deg2Rad);

        _numberOfStepDetectRays = Mathf.RoundToInt(((_maxStepHeight * 100.0f) *0.5f) + 1.0f);
        _rayIncrementAmount = _maxStepHeight / _numberOfStepDetectRays;
    }

    private void FixedUpdate()
    {
        if(!_cameraController.UsingOrbitalCamera)
        {
            _playerLookInput = GetLookInput();
            PlayerLook();
            PitchCamera();
        }

        _playerMoveInput = GetMoveInput();
        _playerIsGrounded = PlayerGroundCheck();
        
        _playerMoveInput = PlayerMove();
        _playerMoveInput = PlayerStairs();
        _playerMoveInput = PlayerSlope();
        _playerMoveInput = PlayerRun();

        PlayerInfoCapture();

        _playerMoveInput.y = PlayerFallGravity();
        _playerMoveInput.y = PlayerJump();

        Debug.DrawRay(_rigidbody.position, _rigidbody.transform.TransformDirection(_playerMoveInput), Color.red, 0.5f);

        _playerMoveInput *= _rigidbody.mass; 

        _rigidbody.AddRelativeForce(_playerMoveInput, ForceMode.Force);
    }

    private Vector3 GetLookInput()
    {
        _previousPlayerLookInput = _playerLookInput;
        _playerLookInput = new Vector3(_input.LookInput.x, (_input.InvertMouseY ? - _input.LookInput.y : _input.LookInput.y), 0.0f);
        return Vector3.Lerp(_previousPlayerLookInput, _playerLookInput * Time.deltaTime, _playerLookInputLerpTime);
    }

    private void PlayerLook()
    {
        _rigidbody.rotation = Quaternion.Euler(0.0f, _rigidbody.rotation.eulerAngles.y + (_playerLookInput.x * _rotationSpeedMultiplier), 0.0f);
    }

    private void PitchCamera()
    {
        Vector3 rotationValues = CameraFollow.rotation.eulerAngles;
        _cameraPitch += _playerLookInput.y * _pitchSpeedMultiplier;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -89.9f, 89.9f);

        CameraFollow.rotation = Quaternion.Euler(_cameraPitch, rotationValues.y, rotationValues.z);
    }

    private Vector3 GetMoveInput()
    {
        return new Vector3(_input.MoveInput.x, 0.0f, _input.MoveInput.y);
    }

    private bool PlayerGroundCheck()
    {
        float sphereCastRadius = _capsuleCollider.radius * _groundCheckRadiusMultiplier;
        Physics.SphereCast(_rigidbody.position, sphereCastRadius, Vector3.down, out _groundCheckHit);
        _playerCenterToGroundDistance = _groundCheckHit.distance + sphereCastRadius;
        return ((_playerCenterToGroundDistance >= _capsuleCollider.bounds.extents.y - _groundCheckDistanceTolerance) &&
                (_playerCenterToGroundDistance <= _capsuleCollider.bounds.extents.y + _groundCheckDistanceTolerance));
    }

    private Vector3 PlayerStairs()
    {
        Vector3 calculatedStepInput = _playerMoveInput;

        _playerHalfHeightToGround = _capsuleCollider.bounds.extents.y;
        if(_playerCenterToGroundDistance < _capsuleCollider.bounds.extents.y)
        {
            _playerHalfHeightToGround = _playerCenterToGroundDistance;
        }

        calculatedStepInput = AscendStairs(calculatedStepInput);
        if(!(_playerIsAscendingStairs))
        {
            calculatedStepInput = DescendStairs(calculatedStepInput);
        }
        return calculatedStepInput;
    }

    private Vector3 AscendStairs(Vector3 calculatedStepInput)
    {
        if (_input.MoveIsPressed)
        {
            float calculatedVelDistance = _isFirstStep ? (_rigidbody.velocity.magnitude * _firstStepVelocityDistanceMultiplier) + _capsuleCollider.radius : _capsuleCollider.radius;

            float ray = 0.0f;
            List<RaycastHit> raysThatHit = new List<RaycastHit>();
            for (int x = 1;
                x <= _numberOfStepDetectRays;
                x++, ray += _rayIncrementAmount)
            {
                Vector3 rayLower = new Vector3(_rigidbody.position.x, ((_rigidbody.position.y - _playerHalfHeightToGround) + ray), _rigidbody.position.z);
                RaycastHit hitLower;

                if (Physics.Raycast(rayLower, _rigidbody.transform.TransformDirection(_playerMoveInput), out hitLower, calculatedVelDistance + _maxAscendRayDistance))
                {
                    float stairSlopeAngle = Vector3.Angle(hitLower.normal, _rigidbody.transform.up);
                    if (stairSlopeAngle == 90.0f)
                    {
                        raysThatHit.Add(hitLower);
                    }
                }
            }
            if(raysThatHit.Count > 0)
            {
                Vector3 rayUpper = new Vector3(_rigidbody.position.x, (((_rigidbody.position.y - _playerHalfHeightToGround) + _maxStepHeight) + _rayIncrementAmount), _rigidbody.position.z);
                RaycastHit hitUpper;
                Physics.Raycast(rayUpper, _rigidbody.transform.TransformDirection(_playerMoveInput), out hitUpper, calculatedVelDistance + (_maxAscendRayDistance * 2.0f));

                if (!(hitUpper.collider) || (hitUpper.distance - raysThatHit[0].distance) > _minStepDepth)
                {
                    if (Vector3.Angle(raysThatHit[0].normal, _rigidbody.transform.TransformDirection(-_playerMoveInput)) <= _maximumAngleOfApproachToAscend)
                    {
                        Debug.DrawRay(rayUpper, _rigidbody.transform.TransformDirection(_playerMoveInput), Color.yellow, 5.0f);

                        _playerIsAscendingStairs = true;
                        Vector3 playerRelX = Vector3.Cross(_playerMoveInput, Vector3.up);

                        if (_isFirstStep)
                        {
                            calculatedStepInput = Quaternion.AngleAxis(45.0f, playerRelX) * calculatedStepInput;
                            _isFirstStep = false;
                        }
                        else
                        {
                            float stairHeight = raysThatHit.Count * _rayIncrementAmount * _stairHeightPaddingMultiplier;

                            float avgDistance = 0.0f;
                            foreach (RaycastHit r in raysThatHit)
                            {
                                avgDistance += r.distance;
                            }
                            avgDistance /= raysThatHit.Count;

                            float tanAngle = Mathf.Atan2(stairHeight, avgDistance) * Mathf.Rad2Deg;
                            calculatedStepInput = Quaternion.AngleAxis(tanAngle, playerRelX) * calculatedStepInput;
                            calculatedStepInput *= _ascendingStairsMovementMultiplier;
                        }
                    }
                    else
                    {   //more than 45* approach
                        _playerIsAscendingStairs = false;
                        _isFirstStep = true;
                    }
                }
                else
                {   //top ray hit something
                    _playerIsAscendingStairs = false;
                    _isFirstStep = true;
                }
            }
            else
            {   //no rays hit
                _playerIsAscendingStairs = false;
                _isFirstStep = true;
            }
        }
        else
        {   //move is not pressed
            _playerIsAscendingStairs = false;
            _isFirstStep = true;
        }
        return calculatedStepInput;
    }

    private Vector3 DescendStairs(Vector3 calculatedStepInput)
    {
        if (_input.MoveIsPressed)
        {
            float ray = 0.0f;
            List<RaycastHit> raysThatHit = new List<RaycastHit>();
            for (int x = 1;
                x <= _numberOfStepDetectRays;
                x++, ray += _rayIncrementAmount)
            {
                Vector3 rayLower = new Vector3(_rigidbody.position.x, ((_rigidbody.position.y - _playerHalfHeightToGround) + ray), _rigidbody.position.z);
                RaycastHit hitLower;
                if (Physics.Raycast(rayLower, _rigidbody.transform.TransformDirection(-_playerMoveInput), out hitLower, _capsuleCollider.radius + _maxDescendRayDistance))
                {
                    float stairSlopeAngle = Vector3.Angle(hitLower.normal, _rigidbody.transform.up);
                    if (stairSlopeAngle == 90.0f)
                    {
                        raysThatHit.Add(hitLower);
                    }
                }
            }
            if (raysThatHit.Count > 0)
            {
                Vector3 rayUpper = new Vector3(_rigidbody.position.x, (((_rigidbody.position.y - _playerHalfHeightToGround) + _maxStepHeight) + _rayIncrementAmount), _rigidbody.position.z);
                RaycastHit hitUpper;
                Physics.Raycast(rayUpper, _rigidbody.transform.TransformDirection(-_playerMoveInput), out hitUpper, _capsuleCollider.radius + (_maxDescendRayDistance * 2.0f));
                if (!(hitUpper.collider) || (hitUpper.distance - raysThatHit[0].distance) > _minStepDepth)
                {
                    if(!(_playerIsGrounded) && hitUpper.distance < _capsuleCollider.radius + (_maxDescendRayDistance * 2.0f))
                    {
                        Debug.DrawRay(rayUpper, _rigidbody.transform.TransformDirection(-_playerMoveInput), Color.yellow, 5.0f);

                        _playerIsDescendingStairs = true;
                        Vector3 playerRelX = Vector3.Cross(_playerMoveInput, Vector3.up);

                        float stairHeight = raysThatHit.Count * _rayIncrementAmount * _stairHeightPaddingMultiplier;

                        float avgDistance = 0.0f;
                        foreach (RaycastHit r in raysThatHit)
                        {
                            avgDistance += r.distance;
                        }
                        avgDistance /= raysThatHit.Count;

                        float tanAngle = Mathf. Atan2(stairHeight, avgDistance) * Mathf.Rad2Deg;
                        calculatedStepInput = Quaternion.AngleAxis(tanAngle - 90.0f, playerRelX) * calculatedStepInput;
                        calculatedStepInput *= _descendingStairsMovementMultiplier;
                    }
                    else
                    {   //more than 45* approach
                        _playerIsDescendingStairs = false;
                    }
                }
                else
                {   //top ray hit something
                    _playerIsDescendingStairs = false;
                }
            }
            else
            {   //no rays hit
                _playerIsDescendingStairs = false;
            }
        }
        else
        {   //move is not pressed
            _playerIsDescendingStairs = false;
        }
        return calculatedStepInput;
    }


    private Vector3 PlayerMove()
    {
        return ((_playerIsGrounded) ? (_playerMoveInput * _movementMultiplier) : (_playerMoveInput * _movementMultiplier * _notGroundedMovementMultiplier));
    }

    private Vector3 PlayerSlope()
    {
        Vector3 calculatedPlayerMovement = _playerMoveInput;

        if(_playerIsGrounded && !_playerIsAscendingStairs && !_playerIsDescendingStairs)
        {
            Vector3 localGroundCheckHitNormal = _rigidbody.transform.InverseTransformDirection(_groundCheckHit.normal);

            float groundSlopeAngle = Vector3.Angle(localGroundCheckHitNormal, _rigidbody.transform.up);
            if(groundSlopeAngle == 0.0f)
            {
                if(_input.MoveIsPressed)
                {
                    RaycastHit rayHit;
                    float rayCalculatedRayHeight = _rigidbody.position.y - _playerCenterToGroundDistance + _groundCheckDistanceTolerance;
                    Vector3 rayOrigin = new Vector3(_rigidbody.position.x, rayCalculatedRayHeight, _rigidbody.position.z);
                    if (Physics.Raycast(rayOrigin, _rigidbody.transform.TransformDirection(calculatedPlayerMovement), out rayHit, 0.75f))
                    {
                        if(Vector3.Angle(rayHit.normal, _rigidbody.transform.up) > _maxSlopeAngle)
                        {
                            calculatedPlayerMovement.y = -_movementMultiplier;
                        }
                    }
                    //Debug.DrawRay(rayOrigin, _rigidbody.transform.TransformDirection(calculatedPlayerMovement), Color.green, 1.0f);
                }

                if(calculatedPlayerMovement.y == 0.0f)
                {
                    calculatedPlayerMovement.y = _gravityGrounded;
                }

            }
            else
            {
                Quaternion slopeAngleRotation = Quaternion.FromToRotation(_rigidbody.transform.up, localGroundCheckHitNormal);
                calculatedPlayerMovement = slopeAngleRotation * calculatedPlayerMovement;

                float relativeSlopeAngle = Vector3.Angle(calculatedPlayerMovement, _rigidbody.transform.up) - 90.0f;
                calculatedPlayerMovement += calculatedPlayerMovement * (relativeSlopeAngle/ 90.0f);

                if(groundSlopeAngle < _maxSlopeAngle)
                {
                    if(_input.MoveIsPressed)
                    {
                        calculatedPlayerMovement.y += _gravityGrounded;
                    }
                }
                else
                {
                    float calculatedSlopeGravity = groundSlopeAngle * -0.2f;
                    if(calculatedSlopeGravity < calculatedPlayerMovement.y)
                    {
                        calculatedPlayerMovement.y = calculatedSlopeGravity;
                    }
                }

            }
        }
        return calculatedPlayerMovement;
    }
    private Vector3 PlayerRun()
    {
        Vector3 calculatedPlayerRunSpeed = _playerMoveInput;
        if(_input.MoveIsPressed && _input.RunIsPressed)
        {
            calculatedPlayerRunSpeed *= _runMultiplier;
        }
        return calculatedPlayerRunSpeed;
    }    
    private void PlayerInfoCapture()
    {
        if(_playerIsGrounded && _groundCheckHit.collider)
        {
            _lastGroundCheckHit = _groundCheckHit;
            _playerMoveInputAtLastGroundCheckHit = _playerMoveInput;
        }
    }
    private float PlayerFallGravity()
    {
        float gravity = _playerMoveInput.y;
        if(_playerIsGrounded || _playerIsAscendingStairs || _playerIsDescendingStairs)
        {
            _gravityFallCurrent = _gravityFallMin; // reset gravity to minimum
        }
        else
        {
            _playerFallTimer -= Time.fixedDeltaTime;
            if(_playerFallTimer < 0.0f)
            {
                float gravityFallMax = _movementMultiplier * _runMultiplier * 5.0f;
                float gravityFallIncrementAmount = (gravityFallMax - _gravityFallMin) * 0.1f;
                if(_gravityFallCurrent < gravityFallMax)
                {
                    _gravityFallCurrent += gravityFallIncrementAmount;
                }
                _playerFallTimer = _gravityFallIncrementTime;
            }
            gravity = -_gravityFallCurrent;
        }
        return gravity;
    }

    private float PlayerJump()
    {
        float calculatedJumpInput = _playerMoveInput.y;

        SetJumpTimeCounter();
        SetCoyoteTimeCounter();
        SetJumpBufferTimeCounter();

        if (_jumpBufferTimeCounter > 0.0f && !_playerIsJumping && _coyoteTimeCounter >0.0f)
        {
            if(Vector3.Angle(_rigidbody.transform.up, _groundCheckHit.normal) < _maxSlopeJumpAngle)
            {
                KickStuffOutFromUnder();

                calculatedJumpInput = _initialJumpForceMultiplier;
                _playerIsJumping = true;
                _jumpBufferTimeCounter = 0.0f;
                _coyoteTimeCounter = 0.0f;
            }
        }
        else if (_input.JumpIsPressed && _playerIsJumping && !_playerIsGrounded && _jumpTimeCounter > 0.0f)
        {
            calculatedJumpInput = _initialJumpForceMultiplier * _continualJumpForceMultiplier;
        }
        else if (_playerIsJumping && _playerIsGrounded)
        {
            _playerIsJumping = false;
        }

        return calculatedJumpInput;
    }

    private void SetJumpTimeCounter()
    {
        if (_playerIsJumping && !_playerIsGrounded)
        {
            _jumpTimeCounter -= Time.fixedDeltaTime;
        }
        else
        {
            _jumpTimeCounter = _jumpTime;
        }
    }
    private void SetCoyoteTimeCounter()
    {
        if (_playerIsGrounded)
        {
            _coyoteTimeCounter = _coyoteTime;
        }
        else
        {
            _coyoteTimeCounter -= Time.fixedDeltaTime;
        }
    }    
    private void SetJumpBufferTimeCounter()
    {
        if (!_jumpWasPressedLastFrame && _input.JumpIsPressed)
        {
            _jumpBufferTimeCounter = _jumpBufferTime;
        }
        else if (_jumpBufferTimeCounter > 0.0f)
        {
            _jumpBufferTimeCounter -= Time.fixedDeltaTime;
        }
        _jumpWasPressedLastFrame = _input.JumpIsPressed;
    }

    private void KickStuffOutFromUnder()
    {
        if (_lastGroundCheckHit.collider.attachedRigidbody)
        {
            Vector3 force = (_rigidbody.transform.TransformDirection(_playerMoveInputAtLastGroundCheckHit) * _lastGroundCheckHit.collider.attachedRigidbody.mass * _jumpReactionForceMultiplier);
            _lastGroundCheckHit.collider.attachedRigidbody.AddForceAtPosition(-force, _lastGroundCheckHit.point, ForceMode.Impulse);
        }
    }



}
