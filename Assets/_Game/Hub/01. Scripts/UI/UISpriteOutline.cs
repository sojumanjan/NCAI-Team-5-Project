// UI Image의 바깥 경계에 얇은 외곽선을 입히는 컴포넌트 (Hub/UI Sprite Outline 셰이더와 짝)
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image 가장자리 밖으로 얇은 외곽선을 그린다. 이미지 파일은 건드리지 않는다.
///
/// 메시를 선 굵기만큼 바깥으로 넓히는 이유: 셰이더는 사각형 안쪽에만 그릴 수 있는데, 보상 그림들은
/// 가장자리 여백이 0~1px라 넓히지 않으면 그림이 끝에 닿은 쪽의 선이 잘린다.
///
/// 머티리얼은 이미지마다 코드에서 만들어 쓴다(IMaterialModifier). 에셋이나 씬에 머티리얼이 남지 않고,
/// 편집 모드에서도 바로 보인다. 기본 Outline 컴포넌트는 그림을 복사해 색을 곱하는 방식이라
/// 유색 그림에는 흰 선이 나오지 않아 쓰지 않는다.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class UISpriteOutline : BaseMeshEffect, IMaterialModifier
{
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineUVId = Shader.PropertyToID("_OutlineUV");
    private static readonly int UVRectId = Shader.PropertyToID("_UVRect");

    [Tooltip("Hub/UI Sprite Outline 셰이더. Shader.Find로 찾으면 빌드에서 빠질 수 있어 직접 연결합니다.")]
    [SerializeField] private Shader shader;

    [SerializeField] private Color color = Color.white;

    [Tooltip("선 굵기 (이 이미지의 로컬 단위 = 캔버스 픽셀). 이미지 크기가 바뀌면 같이 커지고 작아집니다.")]
    [SerializeField] private float width = 1.5f;

    private Material _material;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || width <= 0f || vh.currentVertCount == 0)
        {
            return;
        }

        // 정점이 차지하는 사각형과 그에 대응하는 UV 범위를 재서, 같은 비율로 바깥까지 늘린다.
        var vertex = new UIVertex();
        Vector2 posMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 posMax = new Vector2(float.MinValue, float.MinValue);
        Vector2 uvMin = posMin;
        Vector2 uvMax = posMax;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            posMin = Vector2.Min(posMin, vertex.position);
            posMax = Vector2.Max(posMax, vertex.position);
            uvMin = Vector2.Min(uvMin, vertex.uv0);
            uvMax = Vector2.Max(uvMax, vertex.uv0);
        }

        Vector2 size = posMax - posMin;
        if (size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        // 샘플이 선 끝에 걸려 잘리지 않게 굵기보다 1만큼 더 넓힌다.
        float pad = width + 1f;
        Vector2 uvPerUnit = new Vector2((uvMax.x - uvMin.x) / size.x, (uvMax.y - uvMin.y) / size.y);

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            Vector2 position = vertex.position;
            Vector2 push = new Vector2(
                position.x <= posMin.x ? -pad : position.x >= posMax.x ? pad : 0f,
                position.y <= posMin.y ? -pad : position.y >= posMax.y ? pad : 0f);

            vertex.position = (Vector3)(position + push) + new Vector3(0f, 0f, vertex.position.z);
            vertex.uv0 = new Vector4(vertex.uv0.x + push.x * uvPerUnit.x, vertex.uv0.y + push.y * uvPerUnit.y, vertex.uv0.z, vertex.uv0.w);
            vh.SetUIVertex(vertex, i);
        }
    }

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (!IsActive() || shader == null)
        {
            return baseMaterial;
        }

        if (_material == null)
        {
            _material = new Material(shader) { name = "UISpriteOutline (Instance)", hideFlags = HideFlags.HideAndDontSave };
        }
        else if (_material.shader != shader)
        {
            _material.shader = shader;
        }

        // 부모 Mask가 넣어준 스텐실 값을 그대로 이어받아야 가려지는 곳에서 선만 삐져나오지 않는다.
        _material.CopyPropertiesFromMaterial(baseMaterial);

        Vector4 uvRect = SpriteUVRect();
        Vector2 drawn = DrawnSize();
        _material.SetColor(OutlineColorId, color);
        _material.SetVector(OutlineUVId, new Vector4(
            width / Mathf.Max(1f, drawn.x) * (uvRect.z - uvRect.x),
            width / Mathf.Max(1f, drawn.y) * (uvRect.w - uvRect.y), 0f, 0f));
        _material.SetVector(UVRectId, uvRect);

        return _material;
    }

    /// <summary>
    /// 그림이 실제로 그려지는 크기. Preserve Aspect를 켜면 그림이 사각형보다 작게 그려져서,
    /// 사각형 크기로 굵기를 재면 한쪽 방향 선만 두꺼워진다.
    /// </summary>
    private Vector2 DrawnSize()
    {
        Rect rect = graphic.rectTransform.rect;

        if (graphic is Image image && image.preserveAspect && image.type == Image.Type.Simple && image.sprite != null)
        {
            Vector2 size = image.sprite.rect.size;
            if (size.x > 0f && size.y > 0f)
            {
                return size * Mathf.Min(rect.width / size.x, rect.height / size.y);
            }
        }

        return rect.size;
    }

    /// <summary>텍스처 안에서 이 그림이 차지하는 UV 범위(min xy, max zw). 그 밖은 셰이더가 투명으로 본다.</summary>
    private Vector4 SpriteUVRect()
    {
        if (graphic is Image image && image.sprite != null)
        {
            return UnityEngine.Sprites.DataUtility.GetOuterUV(image.sprite);
        }

        return new Vector4(0f, 0f, 1f, 1f);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetMaterialDirty();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SetMaterialDirty();
    }

    // 선 굵기를 UV로 바꿀 때 사각형 크기를 쓰므로, 크기가 바뀌면 머티리얼 값도 다시 넣어야 한다.
    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetMaterialDirty();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_material == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_material);
        }
        else
        {
            DestroyImmediate(_material);
        }
    }

    private void SetMaterialDirty()
    {
        if (graphic != null)
        {
            graphic.SetMaterialDirty();
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        width = Mathf.Max(0f, width);
        base.OnValidate();
        SetMaterialDirty();
    }
#endif
}
