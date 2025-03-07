using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "NewStimulus", menuName = "Study/Stimulus")]
public class StimulusSO : ScriptableObject {
    [Header("Visual Stimulus")]
    public VideoClip videoClip;

    [Header("Haptic Timeline")]
    [Tooltip("Pre-computed timeline of haptic markers synchronized to the video’s audio.")]
    public HapticTimelineSO hapticTimeline;
}