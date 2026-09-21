using UnityEngine;

/// <summary>
/// 소리 하나의 정의. 팀원이 자기 폴더에 에셋으로 찍어내고 자기 스크립트에 꽂아 쓴다.
///
/// 중앙에 소리 목록이나 enum을 두지 않는 것이 이 설계의 핵심이다. 목록이 있으면 소리를
/// 하나 추가할 때마다 여섯 명이 같은 공용 파일을 편집하게 되고, 그건 곧 머지 충돌이다.
/// 소리를 참조로 넘기면 목록 자체가 필요 없다.
///
/// 생성: Assets > Create > Audio > Sound Data
/// </summary>
[CreateAssetMenu(fileName = "Sound_", menuName = "Audio/Sound Data")]
public class SoundData : ScriptableObject
{
    [Header("소리")]
    [Tooltip("재생할 클립. 둘 이상 넣으면 그중 하나를 무작위로 고릅니다.")]
    [SerializeField] private AudioClip[] clips;

    [Tooltip("어느 갈래인지. 믹서 그룹과 볼륨 설정이 여기서 갈립니다.")]
    [SerializeField] private SoundCategory category = SoundCategory.SFX;

    [Header("세기")]
    [Range(0f, 1f)]
    [Tooltip("이 소리 자체의 크기. 갈래별 볼륨이 여기에 한 번 더 곱해집니다.")]
    [SerializeField] private float volume = 1f;

    [Tooltip("음높이를 이 범위에서 무작위로 고릅니다. 둘을 같게 두면 변화가 없습니다.")]
    [SerializeField] private Vector2 pitchRange = new Vector2(1f, 1f);

    [Header("재생 방식")]
    [Tooltip("끌 때까지 반복합니다. 반드시 SoundHandle을 들고 있다가 직접 멈추세요.")]
    [SerializeField] private bool loop;

    [Range(0f, 1f)]
    [Tooltip("0이면 어디서나 같게 들리는 2D, 1이면 거리와 방향이 있는 3D입니다.")]
    [SerializeField] private float spatialBlend;

    [Tooltip("3D일 때 이 거리를 넘으면 안 들립니다 (m).")]
    [SerializeField] private float maxDistance = 20f;

    [Tooltip("이 간격 안에 같은 소리를 또 요청하면 무시합니다 (초). 0이면 항상 재생합니다.")]
    [SerializeField] private float minInterval = 0.05f;

    public SoundCategory Category => category;

    public float Volume => Mathf.Clamp01(volume);

    public bool Loop => loop;

    public float SpatialBlend => Mathf.Clamp01(spatialBlend);

    public float MaxDistance => Mathf.Max(1f, maxDistance);

    public float MinInterval => Mathf.Max(0f, minInterval);

    /// <summary>쓸 수 있는 클립이 하나라도 있는지.</summary>
    public bool HasClip => PickClip() != null;

    /// <summary>
    /// 이번에 재생할 클립. 여럿이면 무작위로 고른다.
    ///
    /// 같은 파일이 연달아 나오면 몇 초 만에 기계처럼 들린다. 발소리나 재료 넣는 소리처럼
    /// 자주 반복되는 것에 특히 그렇다.
    /// </summary>
    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        if (clips.Length == 1)
        {
            return clips[0];
        }

        // 비어 있는 칸을 건너뛰고 세야, 인스펙터에 null이 섞여 있어도 무음이 나오지 않는다.
        int usable = 0;
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                usable++;
            }
        }

        if (usable == 0)
        {
            return null;
        }

        int chosen = Random.Range(0, usable);
        foreach (AudioClip clip in clips)
        {
            if (clip == null)
            {
                continue;
            }

            if (chosen == 0)
            {
                return clip;
            }

            chosen--;
        }

        return null;
    }

    /// <summary>이번에 쓸 음높이.</summary>
    public float PickPitch() => Random.Range(pitchRange.x, pitchRange.y);

    private void OnValidate()
    {
        pitchRange.x = Mathf.Clamp(pitchRange.x, 0.1f, 3f);
        pitchRange.y = Mathf.Clamp(pitchRange.y, pitchRange.x, 3f);

        // BGM은 거의 항상 끊기지 않고 2D로 흘러야 한다. 실수로 3D로 두면 원인을 찾기 어렵다.
        if (category == SoundCategory.BGM)
        {
            spatialBlend = 0f;
        }
    }
}
