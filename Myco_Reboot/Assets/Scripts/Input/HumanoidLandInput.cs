using UnityEngine;
using UnityEngine.InputSystem;

public class HumanoidLandInput : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; } = Vector2.zero;
    public bool MoveIsPressed = false;
    public Vector2 LookInput {get; private set; } = Vector2.zero;
    public bool InvertMouseY { get; private set; } = false;
    public float ZoomCameraInput {get; private set;} = 0.0f;
    public bool InvertScroll {get; private set;} = false;
    public bool RunIsPressed {get; private set;} = false;

    public bool ChangeCameraWasPressedThisFrame {get; private set;} = false;

    InputActions _input = null;


    private void OnEnable()
    {
        _input = new InputActions();
        _input.HumanoidLand.Enable();

        _input.HumanoidLand.Move.performed += SetMove;
        _input.HumanoidLand.Move.canceled += SetMove;

        _input.HumanoidLand.Look.performed += SetLook;
        _input.HumanoidLand.Look.canceled += SetLook;

        _input.HumanoidLand.Run.started += SetRun;
        _input.HumanoidLand.Run.canceled += SetRun;

        _input.HumanoidLand.ZoomCamera.started += SetZoomCamera;
        _input.HumanoidLand.ZoomCamera.canceled += SetZoomCamera;        

    }

    private void OnDisable()
    {
        _input.HumanoidLand.Move.performed -= SetMove;
        _input.HumanoidLand.Move.canceled -= SetMove;

        _input.HumanoidLand.Look.performed -= SetLook;
        _input.HumanoidLand.Look.canceled -= SetLook;

        _input.HumanoidLand.Run.started -= SetRun;
        _input.HumanoidLand.Run.canceled -= SetRun;

        _input.HumanoidLand.ZoomCamera.started -= SetZoomCamera;
        _input.HumanoidLand.ZoomCamera.canceled -= SetZoomCamera;

        _input.HumanoidLand.Disable();        

    }

    private void Update()
    {
        ChangeCameraWasPressedThisFrame = _input.HumanoidLand.ChangeCamera.WasPressedThisFrame();
    }

    private void SetMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
        MoveIsPressed = !(MoveInput == Vector2.zero);
    }

    private void SetLook(InputAction.CallbackContext ctx)
    {
        LookInput = ctx.ReadValue<Vector2>();
    }

    private void SetRun(InputAction.CallbackContext ctx)
    {
        RunIsPressed = ctx.started;
    }


    private void SetZoomCamera(InputAction.CallbackContext ctx)
    {
        ZoomCameraInput = ctx.ReadValue<float>();
    }
}
