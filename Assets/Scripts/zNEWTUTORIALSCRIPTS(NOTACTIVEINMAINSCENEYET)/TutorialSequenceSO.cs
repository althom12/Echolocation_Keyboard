using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TutorialSequence", menuName = "Tutorial/Sequence List")]
public class TutorialSequenceSO : ScriptableObject
{
    [Header("Master Chapter List")]
    [Tooltip("Drag chapters here to define the Menu Order. File names do not matter.")]
    public List<TutorialChapterSO> chapters = new List<TutorialChapterSO>();
}