using UnityEngine;

public class HumanoidLandController : MonoBehaviour
{
    public Transform CameraFollow;
    Rigidbody _rigidbody = null;
    CapsuleCollider _capsuleCollider = null;
    [SerializeField] HumanoidLandInput _input;
    [SerializeField] CameraController _cameraController;

    Vector3 _playerMoveInput = Vector3.zero;

    Vector3 _playerLookInput = Vector3.zero;
    Vector3 _previousPlayerLookInput = Vector3.zero;
    float _cameraPitch = 0.0f;
    [SerializeField] float _playerLookInputLerpTime = 0.35f;

    [Header("Movement")]
    [SerializeField] float _movementMultiplier = 30.0f;
    [SerializeField] float _rotationSpeedMultiplier = 180.0f;
    [SerializeField] float _pitchSpeedMultiplier = 180.0f;
    [SerializeField] float _runMultiplier = 2.5f;

    [Header("Ground Check")]
    [SerializeField] bool _playerIsGrounded = true;
    [SerializeField] [Range(0.0f, 1.8f)] float _groundCheckRadiusMultiplier = 0.9f;
    [SerializeField] [Range(-0.95f, 1.05f)] float _groundCheckDistance = 0.05f;
    RaycastHit _groundCheckHit = new RaycastHit();

    [Header("Gravity")]
    [SerializeField] float _gravityFallCurrent = -100.0f;
    [SerializeField] float _gravityFallMin = -100.0f;
    [SerializeField] float _gravityFallMax = -500.0f;
    [SerializeField] [Range(-5.0f, -35.0f)] float _gravityFallIncrementAmount = -20.0f;
    [SerializeField] float _gravityFallIncrementTime = 0.05f;
    [SerializeField] float _playerFallTimer = 0.0f;
    [SerializeField] float _gravityGrounded = -1.0f;
    [SerializeField] float _maxSlopeAngle = 47.5f;

    private void Awake()
    {
        //quick fix for weird missing mouse input issue in editor due to excessive framerate
        #if UNITY_EDITOR
        QualitySettings.vSyncCount = 0;  // VSync must be disabled
        Application.targetFrameRate = 60;
        #endif
        
        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
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
        _playerMoveInput = PlayerSlope();
        _playerMoveInput = PlayerRun();
        _playerMoveInput.y = PlayerFallGravity();

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
    private Vector3 PlayerMove()
    {
        return new Vector3 (_playerMoveInput.x * _movementMultiplier,
                                        _playerMoveInput.y,
                                        _playerMoveInput.z * _movementMultiplier);
    }

    private bool PlayerGroundCheck()
    {
        float sphereCastRadius = _capsuleCollider.radius * _groundCheckRadiusMultiplier;
        float sphereCastTravelDistance = _capsuleCollider.bounds.extents.y - sphereCastRadius + _groundCheckDistance;
        return Physics.SphereCast(_rigidbody.position, sphereCastRadius, Vector3.down, out _groundCheckHit, sphereCastTravelDistance);
    }

    private Vector3 PlayerSlope()
    {
        Vector3 calculatedPlayerMovement = _playerMoveInput;

        if(_playerIsGrounded)
        {
            Vector3 localGroundCheckHitNormal = _rigidbody.transform.InverseTransformDirection(_groundCheckHit.normal);

            float groundSlopeAngle = Vector3.Angle(localGroundCheckHitNormal, _rigidbody.transform.up);
            if(groundSlopeAngle == 0.0f)
            {
                if(_input.MoveIsPressed)
                {
                    RaycastHit rayHit;
                    float rayHeightFromGround = 0.1f;
                    float rayCalculatedRayHeight = _rigidbody.position.y - _capsuleCollider.bounds.extents.y + rayHeightFromGround;
                    Vector3 rayOrigin = new Vector3(_rigidbody.position.x, rayCalculatedRayHeight, _rigidbody.position.z);
                    if (Physics.Raycast(rayOrigin, _rigidbody.transform.TransformDirection(calculatedPlayerMovement), out rayHit, 0.75f))
                    {
                        if(Vector3.Angle(rayHit.normal, _rigidbody.transform.up) > _maxSlopeAngle)
                        {
                            calculatedPlayerMovement.y = -_movementMultiplier;
                        }
                    }
                    Debug.DrawRay(rayOrigin, _rigidbody.transform.TransformDirection(calculatedPlayerMovement), Color.green, 1.0f);
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
                    calculatedPlayerMovement.y = groundSlopeAngle *-0.2f;
                }

            }
#if UNITY_EDITOR
    Debug.DrawRay(_rigidbody.position, _rigidbody.transform.TransformDirection(calculatedPlayerMovement), Color.red, 0.5f);
#endif
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
    private float PlayerFallGravity()
    {
        float gravity = _playerMoveInput.y;
        if(_playerIsGrounded)
        {
            _gravityFallCurrent = _gravityFallMin; // reset gravity to minimum
        }
        else
        {
            _playerFallTimer -= Time.fixedDeltaTime;
            if(_playerFallTimer < 0.0f)
            {
                if(_gravityFallCurrent > _gravityFallMax)
                {
                    _gravityFallCurrent += _gravityFallIncrementAmount;
                }
                _playerFallTimer = _gravityFallIncrementTime;
            }
            gravity = _gravityFallCurrent;
        }
        return gravity;
    }



}
