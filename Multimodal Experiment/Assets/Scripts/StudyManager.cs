using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ExperimentGroup {
    Unimodal,   // Video only (mute audio)
    Bimodal,    // Video with audio
    Trimodal    // Video with audio + synchronized haptic feedback
}

public enum ExperimentSection {
    PreILP,
    ImplicitLearningPhase,
    PreIP,
    ImmediatePhase,
    PreDP,
    DelayedPhase
}

public class StudyManager : MonoBehaviour {
    [Header("Experiment Settings")]
    public ExperimentGroup experimentGroup;

    [Header("Section")]
    public ExperimentSection experimentSection = ExperimentSection.PreILP;
    
    [Tooltip("Array of stimuli for the experiment")]
    public StimulusSO[] stimuli;
    
    [Tooltip("Time between stimuli in seconds")]
    public float interStimulusInterval = 1.5f;

    [Header("Media Components")]
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI currentSectionText;
    [SerializeField] private TextMeshProUGUI instructionText;
    
    [Header("Controller Input")]
    [SerializeField] private PS5ControllerInput ps5Controller;

    void Start() {
        // Update the UI for the current section.
        AssignText();

        // In the Start section, wait for the user to press the X button.
        if (experimentSection == ExperimentSection.PreILP) {
            ps5Controller.OnStartButtonPressed += HandleStartInput;
        } else {
            StartCoroutine(RunExperiment());
        }
    }

    private void HandleStartInput() {
        // Unsubscribe so the input is only processed once.
        ps5Controller.OnStartButtonPressed -= HandleStartInput;

        // Switch to the Implicit Learning Phase and update the UI.
        experimentSection = ExperimentSection.ImplicitLearningPhase;
        AssignText();

        // Randomly select 1/3 of the stimuli and store them for the ILP.
        ILPStimuliSelector.SelectStimuli(stimuli);

        // Start playing only the selected stimuli.
        StartCoroutine(RunExperiment());
    }

    IEnumerator RunExperiment() {
        // If we are in the Start section, do nothing.
        if (experimentSection == ExperimentSection.PreILP) yield break;

        // Determine which stimuli to play: if in ILP, use the selected subset; otherwise, use all stimuli.
        List<StimulusSO> stimuliToPlay = (experimentSection == ExperimentSection.ImplicitLearningPhase && ILPStimuliSelector.SelectedStimuli != null)
                                          ? ILPStimuliSelector.SelectedStimuli
                                          : new List<StimulusSO>(stimuli);

        foreach (StimulusSO stimulus in stimuliToPlay) {
            videoPlayer.clip = stimulus.videoClip;

            // Set audio volume based on the experimental group.
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

            // If using the trimodal group, start the haptic sync coroutine.
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

    private void AssignText() {
        switch (experimentSection) {
            case ExperimentSection.PreILP:
                currentSectionText.text = "Pre-Experiment";
                instructionText.text = "Welcome to the experiment! Your task is to determine which objects are man-made and which are not. Press X if the object is man-made and O if it is not.";
                break;
            case ExperimentSection.ImplicitLearningPhase:
                currentSectionText.text = "Implicit Learning Phase";
                instructionText.text = "Press X for 'man-made' and O for 'not man-made'.";
                break;
            case ExperimentSection.ImmediatePhase:
                currentSectionText.text = "Immediate Test Phase";
                instructionText.text = "Decide whether you have seen this video before: press X for Yes and O for No.";
                break;
            case ExperimentSection.DelayedPhase:
                currentSectionText.text = "Delayed Test Phase";
                instructionText.text = "Decide whether you have seen this video before: press X for Yes and O for No.";
                break;
            default:
                currentSectionText.text = "Experiment Section";
                instructionText.text = "Please follow the provided instructions.";
                break;
        }
    }
}
