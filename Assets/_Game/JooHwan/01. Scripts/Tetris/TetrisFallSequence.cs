using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FallEntry
{
    [Tooltip("블록 피벗(로컬 0번 셀) 기준 레인 번호 (0~6)")]
    public int lane;

    [Tooltip("스폰할 블록 프리팹 (회전된 형태까지 이미 반영된 프리팹을 직접 참조)")]
    public GameObject prefab;
}

[CreateAssetMenu(fileName = "TetrisFallSequence", menuName = "JooHwan/Tetris Fall Sequence")]
public class TetrisFallSequence : ScriptableObject
{
    public List<FallEntry> entries = new List<FallEntry>();
}
