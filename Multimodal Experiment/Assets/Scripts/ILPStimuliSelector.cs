using System.Collections.Generic;
using UnityEngine;

public static class ILPStimuliSelector {
    // This property stores the selected stimuli for the ILP.
    public static List<StimulusSO> SelectedStimuli { get; private set; }

    public static List<StimulusSO> SelectStimuli(StimulusSO[] allStimuli) {
        // Copy the array into a list for shuffling.
        List<StimulusSO> stimuliList = new List<StimulusSO>(allStimuli);
        
        // Fisher-Yates shuffle.
        for (int i = 0; i < stimuliList.Count; i++) {
            StimulusSO temp = stimuliList[i];
            int randomIndex = Random.Range(i, stimuliList.Count);
            stimuliList[i] = stimuliList[randomIndex];
            stimuliList[randomIndex] = temp;
        }
        
        // Calculate one third of the stimuli (at least 1).
        int countToSelect = Mathf.Max(1, stimuliList.Count / 3);
        SelectedStimuli = stimuliList.GetRange(0, countToSelect);
        
        return SelectedStimuli;
    }
}