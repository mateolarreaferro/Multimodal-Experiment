#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class HapticTimelineAssetCreator {
    [MenuItem("Assets/Create/Import Haptic Timelines from JSON Folder")]
    public static void ImportHapticTimelines() {
        // Prompt the user to choose a folder containing JSON files.
        string folderPath = EditorUtility.OpenFolderPanel("Select Folder with Haptic JSON Files", Application.dataPath, "");
        if (string.IsNullOrEmpty(folderPath)) {
            Debug.LogWarning("Folder selection cancelled.");
            return;
        }
        
        // Get all JSON files in the selected folder.
        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");
        if (jsonFiles.Length == 0) {
            Debug.LogWarning("No JSON files found in folder: " + folderPath);
            return;
        }

        // Ensure destination folder exists in the project.
        string destinationFolder = "Assets/HapticTimelines";
        if (!AssetDatabase.IsValidFolder(destinationFolder)) {
            AssetDatabase.CreateFolder("Assets", "HapticTimelines");
        }

        // Process each JSON file.
        foreach (string jsonFile in jsonFiles) {
            // Read JSON file content.
            string jsonContent = File.ReadAllText(jsonFile);

            // Create a new instance of HapticTimelineSO.
            HapticTimelineSO timeline = ScriptableObject.CreateInstance<HapticTimelineSO>();

            // Populate the timeline using JSON data.
            JsonUtility.FromJsonOverwrite(jsonContent, timeline);

            // Determine asset name and path.
            string assetName = Path.GetFileNameWithoutExtension(jsonFile) + ".asset";
            string assetPath = Path.Combine(destinationFolder, assetName).Replace("\\", "/");

            // Create the asset in the project.
            AssetDatabase.CreateAsset(timeline, assetPath);
            Debug.Log("Created HapticTimeline asset: " + assetPath);
        }

        // Save and refresh the asset database.
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
    }
}
#endif
