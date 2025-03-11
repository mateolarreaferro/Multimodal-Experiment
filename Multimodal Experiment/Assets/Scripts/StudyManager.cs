using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class ResponseData {
    public string stimulusId;   // e.g., video clip name.
    public string phase;        // "IP" for Immediate Test or "DP" for Delayed Test.
    public string isCorrect;    // "Correct", "Incorrect", or "" if unanswered.
    public string responseTime; // Reaction time (in seconds) as a formatted string, or "" if no response.
}

[System.Serializable]
public class TestResults {
    public List<ResponseData> responses;
}

public enum ExperimentGroup {
    Unimodal,   // Video only (mute audio)
    Bimodal,    // Video with audio
    Trimodal    // Video with audio + synchronized haptic feedback
}

public enum ExperimentSection {
    PreILP,                // Pre-Implicit Learning Phase screen
    ImplicitLearningPhase, // Implicit Learning Phase
    PreIP,                 // Pre-Immediate Test Phase screen
    ImmediatePhase,        // Immediate Test Phase
    PreDP,                 // Pre-Delayed Test Phase screen
    DelayedPhase           // Delayed Test Phase
}

public class StudyManager : MonoBehaviour {
    [Header("Participant Info")]
    public string participantName;

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

    // List to store responses from the Immediate (IP) and Delayed (DP) test phases.
    private List<ResponseData> responses = new List<ResponseData>();

    void Start() {
        // Set the UI text based on the current experiment section.
        AssignText();

        // For pre-phase screens, wait for the user to press the start button.
        if (experimentSection == ExperimentSection.PreILP ||
            experimentSection == ExperimentSection.PreIP ||
            experimentSection == ExperimentSection.PreDP) {
            ps5Controller.OnStartButtonPressed += HandleStartInput;
        } else {
            StartExperimentPhase();
        }
    }

    private void HandleStartInput() {
        // Unsubscribe to avoid multiple triggers.
        ps5Controller.OnStartButtonPressed -= HandleStartInput;

        // Transition to the next phase.
        switch (experimentSection) {
            case ExperimentSection.PreILP:
                experimentSection = ExperimentSection.ImplicitLearningPhase;
                // For ILP, randomly select 1/3 of the stimuli.
                ILPStimuliSelector.SelectStimuli(stimuli);
                break;
            case ExperimentSection.PreIP:
                experimentSection = ExperimentSection.ImmediatePhase;
                break;
            case ExperimentSection.PreDP:
                experimentSection = ExperimentSection.DelayedPhase;
                break;
            default:
                break;
        }
        
        AssignText();
        StartExperimentPhase();
    }

    private void StartExperimentPhase() {
        // Start the appropriate phase based on the current section.
        switch (experimentSection) {
            case ExperimentSection.ImplicitLearningPhase:
                StartCoroutine(RunILP());
                break;
            case ExperimentSection.ImmediatePhase:
                StartCoroutine(RunTestPhase(ExperimentSection.ImmediatePhase));
                break;
            case ExperimentSection.DelayedPhase:
                StartCoroutine(RunTestPhase(ExperimentSection.DelayedPhase));
                break;
            default:
                break;
        }
    }

    // ILP: Play only the selected stimuli (with haptics if applicable), no responses are recorded.
    IEnumerator RunILP() {
        List<StimulusSO> stimuliToPlay = ILPStimuliSelector.SelectedStimuli != null ?
                                             ILPStimuliSelector.SelectedStimuli : new List<StimulusSO>();
        foreach (StimulusSO stimulus in stimuliToPlay) {
            videoPlayer.clip = stimulus.videoClip;
            if (experimentGroup == ExperimentGroup.Unimodal) {
                audioSource.volume = 0f;
            } else {
                audioSource.volume = 1f;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }
            // Start haptics if in Trimodal mode.
            Coroutine hapticRoutine = null;
            if (experimentGroup == ExperimentGroup.Trimodal && stimulus.hapticTimeline != null) {
                hapticRoutine = StartCoroutine(SynchronizeHaptics(stimulus.hapticTimeline));
            }
            videoPlayer.Play();
            Debug.Log("ILP - Playing video: " + stimulus.videoClip.name);
            yield return new WaitForSeconds((float)stimulus.videoClip.length);
            videoPlayer.Stop();
            if (hapticRoutine != null) {
                StopCoroutine(hapticRoutine);
                if (Gamepad.current != null) {
                    Gamepad.current.SetMotorSpeeds(0, 0);
                }
            }
            yield return new WaitForSeconds(interStimulusInterval);
        }
        Debug.Log("Implicit Learning Phase complete.");
        // Transition to Pre-Immediate Test Phase.
        experimentSection = ExperimentSection.PreIP;
        AssignText();
        ps5Controller.OnStartButtonPressed += HandleStartInput;
    }

    // Test phases for Immediate (IP) and Delayed (DP) where responses are recorded.
    // All stimuli are played in a random order.
    IEnumerator RunTestPhase(ExperimentSection phase) {
        // Randomize the order of all stimuli.
        List<StimulusSO> testStimuli = new List<StimulusSO>(stimuli);
        for (int i = 0; i < testStimuli.Count; i++) {
            StimulusSO temp = testStimuli[i];
            int randomIndex = Random.Range(i, testStimuli.Count);
            testStimuli[i] = testStimuli[randomIndex];
            testStimuli[randomIndex] = temp;
        }

        foreach (StimulusSO stimulus in testStimuli) {
            videoPlayer.clip = stimulus.videoClip;
            if (experimentGroup == ExperimentGroup.Unimodal) {
                audioSource.volume = 0f;
            } else {
                audioSource.volume = 1f;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }
            // Start haptics if applicable.
            Coroutine hapticRoutine = null;
            if (experimentGroup == ExperimentGroup.Trimodal && stimulus.hapticTimeline != null) {
                hapticRoutine = StartCoroutine(SynchronizeHaptics(stimulus.hapticTimeline));
            }
            videoPlayer.Play();

            // Use the clip's duration as the response window.
            float clipDuration = (float)videoPlayer.clip.length;
            float stimulusStartTime = Time.time;
            bool answered = false;
            string responseButton = "";
            float responseTime = 0f;

            // Monitor for input during the full duration of the clip.
            while (Time.time - stimulusStartTime < clipDuration) {
                if (!answered && Gamepad.current != null) {
                    if (Gamepad.current.buttonSouth.wasPressedThisFrame) {
                        responseButton = "X";
                        responseTime = Time.time - stimulusStartTime;
                        answered = true;
                        Debug.Log("User answered X at " + responseTime.ToString("F2") + " seconds");
                    } else if (Gamepad.current.buttonEast.wasPressedThisFrame) {
                        responseButton = "O";
                        responseTime = Time.time - stimulusStartTime;
                        answered = true;
                        Debug.Log("User answered O at " + responseTime.ToString("F2") + " seconds");
                    }
                }
                yield return null;
            }
            videoPlayer.Stop();
            if (hapticRoutine != null) {
                StopCoroutine(hapticRoutine);
                if (Gamepad.current != null) {
                    Gamepad.current.SetMotorSpeeds(0, 0);
                }
            }
            
            // Determine the correct answer.
            // If the stimulus was in the ILP-selected list, the correct answer is "X" (seen before).
            bool stimulusSeenInILP = ILPStimuliSelector.SelectedStimuli != null && ILPStimuliSelector.SelectedStimuli.Contains(stimulus);
            string correctAnswer = stimulusSeenInILP ? "X" : "O";
            string isCorrect = "";
            if (answered) {
                isCorrect = (responseButton == correctAnswer) ? "Correct" : "Incorrect";
            }
            
            // Record this trial's data.
            ResponseData result = new ResponseData();
            result.stimulusId = stimulus.videoClip != null ? stimulus.videoClip.name : "Unknown";
            result.phase = (phase == ExperimentSection.ImmediatePhase) ? "IP" : "DP";
            result.isCorrect = answered ? isCorrect : "";
            result.responseTime = answered ? responseTime.ToString("F2") : "";
            responses.Add(result);

            yield return new WaitForSeconds(interStimulusInterval);
        }
        
        // Print all results in the console.
        Debug.Log("Test Phase " + ((phase == ExperimentSection.ImmediatePhase) ? "Immediate" : "Delayed") + " complete. Results:");
        foreach (ResponseData res in responses) {
            Debug.Log("Stimulus: " + res.stimulusId + ", Phase: " + res.phase + ", Correct: " + res.isCorrect + ", Response Time: " + res.responseTime);
        }
        
        // Write results after completing this test phase.
        if (phase == ExperimentSection.ImmediatePhase) {
            WriteResultsToFile("IP");
            // Transition to Pre-Delayed Test Phase.
            experimentSection = ExperimentSection.PreDP;
            AssignText();
            ps5Controller.OnStartButtonPressed += HandleStartInput;
        } else if (phase == ExperimentSection.DelayedPhase) {
            WriteResultsToFile("DP");
        }
    }

    // Writes the responses to a JSON file in the Assets/Results folder with a suffix indicating the phase.
    void WriteResultsToFile(string phaseSuffix) {
        TestResults testResults = new TestResults();
        testResults.responses = responses;
        string json = JsonUtility.ToJson(testResults, true);

        // Create a folder named "Results" inside Assets if it doesn't exist.
        string folderPath = Application.dataPath + "/Results";
        if (!Directory.Exists(folderPath)) {
            Directory.CreateDirectory(folderPath);
            Debug.Log("Created folder: " + folderPath);
        }
        string filePath = folderPath + "/" + participantName + "_results_" + phaseSuffix + ".json";
        
        try {
            File.WriteAllText(filePath, json);
            Debug.Log("Results successfully written to: " + filePath);
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
        } catch (System.Exception e) {
            Debug.LogError("Error writing results: " + e.Message);
        }
    }

    // Haptic synchronization coroutine.
    IEnumerator SynchronizeHaptics(HapticTimelineSO timeline) {
        yield return new WaitUntil(() => videoPlayer.isPlaying && videoPlayer.time > 0);
        int markerIndex = 0;
        while (videoPlayer.isPlaying && markerIndex < timeline.markers.Length) {
            float currentTime = (float)videoPlayer.time;
            HapticMarker marker = timeline.markers[markerIndex];
            if (currentTime >= marker.timeStamp) {
                if (Gamepad.current != null) {
                    Gamepad.current.SetMotorSpeeds(marker.lowFrequency, marker.highFrequency);
                } else {
                    Debug.LogWarning("[Haptic Sync] No gamepad detected!");
                }
                yield return new WaitForSeconds(marker.duration);
                if (Gamepad.current != null) {
                    Gamepad.current.SetMotorSpeeds(0, 0);
                }
                markerIndex++;
            } else {
                yield return null;
            }
        }
    }

    // Updates UI text based on the current experiment section.
    private void AssignText() {
        switch (experimentSection) {
            case ExperimentSection.PreILP:
                currentSectionText.text = "Pre-ILP";
                instructionText.text = "Welcome! Determine which objects are man-made. Press X to begin the experiment.";
                break;
            case ExperimentSection.ImplicitLearningPhase:
                currentSectionText.text = "ILP";
                instructionText.text = "Watch the videos carefully. Press X for 'man-made' and O for 'not man-made'.";
                break;
            case ExperimentSection.PreIP:
                currentSectionText.text = "Pre-ITP";
                instructionText.text = "Get ready for the next phase. Press X to begin.";
                break;
            case ExperimentSection.ImmediatePhase:
                currentSectionText.text = "ITP";
                instructionText.text = "Decide if you have seen this video before. Press X for Yes and O for No.";
                break;
            case ExperimentSection.PreDP:
                currentSectionText.text = "Pre-DTP";
                instructionText.text = "Prepare for the final phase. Press X to begin.";
                break;
            case ExperimentSection.DelayedPhase:
                currentSectionText.text = "DTP";
                instructionText.text = "Was this video in the initial phase? Press X for Yes and O for No.";
                break;
            default:
                currentSectionText.text = "Experiment Section";
                instructionText.text = "Please follow the instructions.";
                break;
        }
    }
}
