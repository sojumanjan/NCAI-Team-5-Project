using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private static readonly List<CameraShake> activeInstances = new List<CameraShake>();

    [SerializeField] private float defaultDuration = 0.2f;
    [SerializeField] private float defaultPositionAmplitude = 0.08f;
    [SerializeField] private float defaultRotationAmplitude = 2.5f;
    [SerializeField] private float frequency = 30f;

    private float shakeTimer;
    private float shakeDuration;
    private float positionAmplitude;
    private float rotationAmplitude;
    private float noiseSeed;

    private Vector3 basePosition;
    private Quaternion baseRotation;

    private void Awake()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;

        activeInstances.Add(this);
    }

    private void OnDestroy()
    {
        activeInstances.Remove(this);
    }

    private void OnEnable()
    {
        // 비활성화되어 있던 동안 LateUpdate가 멈춰 흔들림 오프셋이
        // 그대로 남아있을 수 있으므로, 다시 켜질 때마다 상태를 초기화한다.
        shakeTimer = 0f;
        transform.localPosition = basePosition;
        transform.localRotation = baseRotation;
    }

    public static void ShakeAll()
    {
        foreach (var instance in activeInstances)
        {
            instance.Shake();
        }
    }

    public static void ShakeAll(float duration, float positionStrength, float rotationStrength)
    {
        foreach (var instance in activeInstances)
        {
            instance.Shake(duration, positionStrength, rotationStrength);
        }
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultPositionAmplitude, defaultRotationAmplitude);
    }

    public void Shake(float duration, float positionStrength, float rotationStrength)
    {
        shakeDuration = duration;
        shakeTimer = duration;
        positionAmplitude = positionStrength;
        rotationAmplitude = rotationStrength;
        noiseSeed = Random.Range(0f, 100f);
    }

    private void LateUpdate()
    {
        if (shakeTimer <= 0f)
        {
            transform.localPosition = basePosition;
            transform.localRotation = baseRotation;
            return;
        }

        shakeTimer -= Time.deltaTime;

        float decay = Mathf.Clamp01(shakeTimer / shakeDuration);
        float t = Time.time * frequency;

        float offsetX = (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f;
        float offsetY = (Mathf.PerlinNoise(noiseSeed + 10f, t) - 0.5f) * 2f;
        float rotZ = (Mathf.PerlinNoise(noiseSeed + 20f, t) - 0.5f) * 2f;

        Vector3 shakeOffset = new Vector3(offsetX, offsetY, 0f) * positionAmplitude * decay;
        Quaternion shakeRotation = Quaternion.Euler(0f, 0f, rotZ * rotationAmplitude * decay);

        transform.localPosition = basePosition + shakeOffset;
        transform.localRotation = baseRotation * shakeRotation;
    }
}
