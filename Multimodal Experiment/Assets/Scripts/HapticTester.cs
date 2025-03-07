using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HapticTester : MonoBehaviour {
    [Tooltip("Low frequency intensity (0 to 1)")]
    public float lowFrequency = 0.5f;
    [Tooltip("High frequency intensity (0 to 1)")]
    public float highFrequency = 0.5f;
    [Tooltip("Duration of the haptic effect in seconds")]
    public float duration = 1.0f;

    void Update() {
        // Trigger haptics with the T key.
        if (Keyboard.current.tKey.wasPressedThisFrame) {
            TriggerHaptics();
        }
    }

    private void Start()
    {
        TriggerHaptics();
    }

    void TriggerHaptics() {
        if (Gamepad.current != null) {
            Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);
            Debug.Log("Haptics triggered on PS5 controller.");
            Invoke("StopHaptics", duration);
        } else {
            Debug.Log("No gamepad connected!");
        }
    }

    void StopHaptics() {
        if (Gamepad.current != null) {
            Gamepad.current.SetMotorSpeeds(0, 0);
            Debug.Log("Haptics stopped.");
        }
    }
}