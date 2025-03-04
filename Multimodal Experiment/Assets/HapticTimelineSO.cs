using UnityEngine;

[System.Serializable]
public class HapticMarker {
    [Tooltip("Time (in seconds) from the start of the audio/video at which this haptic event should trigger.")]
    public float timeStamp;
    [Tooltip("Low-frequency motor intensity (0 to 1) for this event.")]
    public float lowFrequency;
    [Tooltip("High-frequency motor intensity (0 to 1) for this event.")]
    public float highFrequency;
    [Tooltip("Duration (in seconds) of the haptic event.")]
    public float duration;
}

[CreateAssetMenu(fileName = "NewHapticTimeline", menuName = "Study/HapticTimeline")]
public class HapticTimelineSO : ScriptableObject {
    public HapticMarker[] markers;
}