using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class FallEntry
{
    [Tooltip("블록 피벗(로컬 0번 셀) 기준 레인 번호 (0~6)")]
    [FormerlySerializedAs("lane")]
    public int Lane;

    [Tooltip("스폰할 블록 프리팹 (회전된 형태까지 이미 반영된 프리팹을 직접 참조)")]
    [FormerlySerializedAs("prefab")]
    public GameObject Prefab;
}

[CreateAssetMenu(fileName = "TetrisFallSequence", menuName = "JooHwan/Tetris Fall Sequence")]
public class TetrisFallSequence : ScriptableObject
{
    [FormerlySerializedAs("entries")]
    public List<FallEntry> Entries = new List<FallEntry>();
}
