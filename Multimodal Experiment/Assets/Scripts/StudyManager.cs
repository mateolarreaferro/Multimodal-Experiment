using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public enum ExperimentGroup {
    Unimodal,   // Video only (mute audio)
    Bimodal,    // Video with audio
    Trimodal    // Video with audio + synchronized haptic feedback
}

public class StudyManager : MonoBehaviour {
    [Header("Experiment Settings")]
    public ExperimentGroup experimentGroup;
    [Tooltip("Array of stimuli for the experiment")]
    public StimulusSO[] stimuli;
    [Tooltip("Time between stimuli in seconds")]
    public float interStimulusInterval = 1.5f;

    [Header("Media Components")]
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    void Start() {
        StartCoroutine(RunExperiment());
    }

    IEnumerator RunExperiment() {
        foreach (StimulusSO stimulus in stimuli) {
            videoPlayer.clip = stimulus.videoClip;

            // Set audio volume based on experimental group.
            if (experimentGroup == ExperimentGroup.Unimodal) {
                audioSource.volume = 0f;
            } else {
                audioSource.volume = 1f;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }

            // Start playing the video.
            videoPlayer.Play();
            Debug.Log("Playing video: " + stimulus.videoClip.name);

            // If using trimodal group, start the haptic sync coroutine.
            Coroutine hapticRoutine = null;
            if (experimentGroup == ExperimentGroup.Trimodal && stimulus.hapticTimeline != null) {
                hapticRoutine = StartCoroutine(SynchronizeHaptics(stimulus.hapticTimeline));
            }

            // Wait for the duration of the video clip.
            yield return new WaitForSeconds((float)stimulus.videoClip.length);

            // Stop video playback and any haptic events.
            videoPlayer.Stop();
            if (hapticRoutine != null) {
                StopCoroutine(hapticRoutine);
                if (Gamepad.current != null) {
                    Gamepad.current.SetMotorSpeeds(0, 0);
                }
            }

            // Wait for the inter-stimulus interval.
            yield return new WaitForSeconds(interStimulusInterval);
        }

        Debug.Log("Experiment sequence complete.");
    }

    IEnumerator SynchronizeHaptics(HapticTimelineSO timeline) {
        // Wait until the video starts playing and its time is > 0.
        yield return new WaitUntil(() => videoPlayer.isPlaying && videoPlayer.time > 0);

        int markerIndex = 0;
        while (videoPlayer.isPlaying && markerIndex < timeline.markers.Length) {
            float currentTime = (float)videoPlayer.time;
            HapticMarker marker = timeline.markers[markerIndex];

            Debug.Log($"[Haptic Sync] Current Time: {currentTime:F2}s, Marker Time: {marker.timeStamp:F2}s, Marker Index: {markerIndex}");

            if (currentTime >= marker.timeStamp) {
                if (Gamepad.current != null) {
                    Gamepad.current.SetMotorSpeeds(marker.lowFrequency, marker.highFrequency);
                    Debug.Log($"[Haptic Trigger] Activating haptics at {marker.timeStamp:F2}s");
                } else {
                    Debug.LogWarning("[Haptic Sync] No gamepad detected!");
                }
                // Wait for the duration of the haptic event.
                yield return new WaitForSeconds(marker.duration);
                if (Gamepad.current != null) {
                    Gamepad.current.SetMotorSpeeds(0, 0);
                }
                markerIndex++;
            } else {
                // Wait a frame.
                yield return null;
            }
        }
        Debug.Log("[Haptic Sync] Finished processing haptic timeline.");
    }


}
