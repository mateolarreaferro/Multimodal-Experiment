#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class HapticTimelineAssetCreator {
    [MenuItem("Assets/Create/Test Haptic Timeline for 2s Video")]
    public static void CreateTestHapticTimeline() {
        HapticTimelineSO timeline = ScriptableObject.CreateInstance<HapticTimelineSO>();
        timeline.markers = new HapticMarker[3];

        timeline.markers[0] = new HapticMarker() {
            timeStamp = 0.5f,
            lowFrequency = 0.2f,
            highFrequency = 0.2f,
            duration = 0.3f
        };

        timeline.markers[1] = new HapticMarker() {
            timeStamp = 1.0f,
            lowFrequency = 0.5f,
            highFrequency = 0.5f,
            duration = 0.3f
        };

        timeline.markers[2] = new HapticMarker() {
            timeStamp = 1.5f,
            lowFrequency = 0.8f,
            highFrequency = 0.8f,
            duration = 0.3f
        };

        AssetDatabase.CreateAsset(timeline, "Assets/TestHapticTimeline2s.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = timeline;
    }
}
#endif