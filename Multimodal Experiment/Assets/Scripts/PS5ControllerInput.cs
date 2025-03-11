using UnityEngine;
using UnityEngine.InputSystem;

public class PS5ControllerInput : MonoBehaviour {
    public delegate void StartButtonPressedHandler();
    public event StartButtonPressedHandler OnStartButtonPressed;

    void Update() {
        // Check if a PS5 controller is connected and if the X (Cross) button (buttonSouth) was pressed this frame.
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) {
            OnStartButtonPressed?.Invoke();
        }
    }
}