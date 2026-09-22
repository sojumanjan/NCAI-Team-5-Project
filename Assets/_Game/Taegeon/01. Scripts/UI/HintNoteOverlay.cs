using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Taegeon
{
    public sealed class HintNoteOverlay : MonoBehaviour
    {
        #region 설정과 상태
        [SerializeField] private Font noteFont;
        [SerializeField] private FirstPersonExplorer explorer;
        [SerializeField] private GameCameraSwitcher cameraSwitcher;
        [SerializeField, Range(1.5f, 4f)] private float magnification = 2.8f;
        private GameObject view;
        private RectTransform paper, lens, lensContent, closeArea;
        private Text body, enlargedBody;
        private Sprite circle;
        private Texture2D circleTexture;
        private bool explorerWasEnabled, switcherWasEnabled, oldCursorVisible, holding;
        private CursorLockMode oldCursorLock;
        public bool IsOpen => view != null && view.activeSelf;
        public static bool IsAnyOpen { get; private set; }
        #endregion

        #region 쪽지 표시와 입력 복구
        /// <summary>노란 메모지와 돋보기 UI를 준비합니다.</summary>
        private void Awake() { Build(); }

        /// <summary>쪽지 본문과 돋보기 속 본문을 함께 갱신합니다.</summary>
        public void SetText(string value)
        {
            Build();
            body.text = value;
            enlargedBody.text = value;
        }

        /// <summary>쪽지를 열고 마우스로 읽을 수 있도록 게임 조작을 잠시 막습니다.</summary>
        public void Open()
        {
            Build();
            if (IsOpen) return;
            oldCursorLock = Cursor.lockState;
            oldCursorVisible = Cursor.visible;
            explorerWasEnabled = explorer != null && explorer.enabled;
            switcherWasEnabled = cameraSwitcher != null && cameraSwitcher.enabled;
            if (explorer != null) explorer.enabled = false;
            if (cameraSwitcher != null) cameraSwitcher.enabled = false;
            view.SetActive(true);
            IsAnyOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>쪽지와 돋보기를 닫고 열기 전의 조작 상태를 복구합니다.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            holding = false;
            lens.gameObject.SetActive(false);
            view.SetActive(false);
            IsAnyOpen = false;
            if (explorer != null) explorer.enabled = explorerWasEnabled;
            if (cameraSwitcher != null) cameraSwitcher.enabled = switcherWasEnabled;
            Cursor.lockState = oldCursorLock;
            Cursor.visible = oldCursorVisible;
        }

        /// <summary>비활성화될 때 돋보기와 입력 잠금을 해제합니다.</summary>
        private void OnDisable() { Close(); }

        /// <summary>실행 중 만든 돋보기 마스크를 정리합니다.</summary>
        private void OnDestroy()
        {
            Close();
            if (circle != null) Destroy(circle);
            if (circleTexture != null) Destroy(circleTexture);
        }
        #endregion

        #region 마우스 돋보기
        /// <summary>누르는 동안 커서 아래 글씨를 확대하고 손을 떼면 원래 표시로 돌아갑니다.</summary>
        private void Update()
        {
            if (!IsOpen) return;
            Vector2 pointer = Vector2.zero;
            bool down = false, pressed = false, escape = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                pointer = Mouse.current.position.ReadValue();
                down = Mouse.current.leftButton.wasPressedThisFrame;
                pressed = Mouse.current.leftButton.isPressed;
            }
            escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            pointer = Input.mousePosition;
            down = Input.GetMouseButtonDown(0);
            pressed = Input.GetMouseButton(0);
            escape = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (escape || (down && RectTransformUtility.RectangleContainsScreenPoint(closeArea, pointer)))
            { Close(); return; }
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(paper, pointer);
            if (down && inside) holding = true;
            if (!pressed) holding = false;
            if (!holding || !inside) { lens.gameObject.SetActive(false); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(paper, pointer, null, out Vector2 point);
            ShowMagnifier(point);
        }

        /// <summary>지정한 종이 위치를 원형 렌즈 안에 확대해서 표시합니다.</summary>
        private void ShowMagnifier(Vector2 point)
        {
            lens.gameObject.SetActive(true);
            lens.anchoredPosition = new Vector2(Mathf.Clamp(point.x + 100f, -300f, 300f), Mathf.Clamp(point.y + 100f, -265f, 290f));
            lensContent.localScale = Vector3.one * magnification;
            lensContent.anchoredPosition = -point * magnification;
        }

        /// <summary>창을 벗어나면 누르기 상태와 돋보기를 해제합니다.</summary>
        private void OnApplicationFocus(bool focus)
        {
            if (focus) return;
            holding = false;
            if (lens != null) lens.gameObject.SetActive(false);
        }
        #endregion

        #region 메모지 구성
        /// <summary>줄과 여백이 있는 노란 종이 및 원형 돋보기를 생성합니다.</summary>
        private void Build()
        {
            if (view != null) return;
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            var shade = Box("Note backdrop", transform, Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, .58f));
            shade.raycastTarget = true;
            view = shade.gameObject;
            Box("Paper shadow", view.transform, new Vector2(12, -14), new Vector2(660, 850), new Color(0, 0, 0, .35f));
            paper = Box("Yellow lined paper", view.transform, Vector2.zero, new Vector2(650, 840), new Color(.98f, .87f, .48f)).rectTransform;
            Box("Binding strip", paper, new Vector2(0, 378), new Vector2(650, 84), new Color(.91f, .74f, .31f));
            Box("Red margin", paper, new Vector2(-265, -20), new Vector2(1.5f, 745), new Color(.70f, .29f, .20f, .5f));
            for (int i = 0; i < 28; i++)
                Box("Ruled line " + i, paper, new Vector2(0, 270 - i * 23), new Vector2(604, 1), new Color(.43f, .48f, .38f, .3f));
            Label("Note title", paper, "발견한 단서", new Vector2(0, 365), new Vector2(530, 54), 30, TextAnchor.MiddleCenter);
            Label("Note instruction", paper, "글씨 위에서 왼쪽 버튼을 누른 채 움직여 보세요", new Vector2(0, 305), new Vector2(580, 32), 17, TextAnchor.MiddleCenter);
            body = Label("Handwritten clues", paper, "", new Vector2(22, -28), new Vector2(525, 584), 18, TextAnchor.UpperLeft);
            body.lineSpacing = 1.05f;
            Label("Footer", paper, "마우스를 떼면 축소  ·  H / Esc 닫기", new Vector2(0, -390), new Vector2(580, 28), 16, TextAnchor.MiddleCenter);
            closeArea = Label("Close note", paper, "×", new Vector2(287, 379), new Vector2(58, 58), 34, TextAnchor.MiddleCenter).rectTransform;
            circleTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            circleTexture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                pixels[y * 128 + x] = new Color(1, 1, 1, Mathf.Clamp01(63 - Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f))));
            circleTexture.SetPixels(pixels); circleTexture.Apply();
            circle = Sprite.Create(circleTexture, new Rect(0, 0, 128, 128), new Vector2(.5f, .5f));
            lens = Box("Magnifying glass", paper, Vector2.zero, new Vector2(414, 414), new Color(.22f, .16f, .09f)).rectTransform;
            lens.GetComponent<Image>().sprite = circle;
            var handle = Box("Lens handle", lens, new Vector2(171, -180), new Vector2(34, 140), new Color(.25f, .16f, .08f));
            handle.rectTransform.localRotation = Quaternion.Euler(0, 0, 40);
            var glass = Box("Circular clipping", lens, Vector2.zero, new Vector2(390, 390), Color.white);
            glass.sprite = circle;
            glass.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            // 돋보기 자신을 복제하지 않도록 종이의 기존 요소만 복사합니다.
            lensContent = Box("Enlarged paper", glass.transform, Vector2.zero, paper.sizeDelta, paper.GetComponent<Image>().color).rectTransform;
            foreach (Transform child in paper)
            {
                if (child == lens) continue;
                var copy = Instantiate(child.gameObject, lensContent, false);
                if (child.name == "Handwritten clues") enlargedBody = copy.GetComponent<Text>();
            }
            foreach (var graphic in lens.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            lens.gameObject.SetActive(false);
            view.SetActive(false);
        }

        /// <summary>중앙 기준의 사각형 UI 요소를 만듭니다.</summary>
        private Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }

        /// <summary>종이에 쓸 작은 글씨를 생성합니다.</summary>
        private Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            // 글자는 네 배 해상도로 생성하고 축소하여 작은 본문과 돋보기를 함께 선명하게 표시합니다.
            const int textResolution = 4;
            rect.anchoredPosition = position; rect.sizeDelta = size * textResolution;
            rect.localScale = Vector3.one / textResolution;
            var text = go.GetComponent<Text>(); text.font = noteFont; text.text = value;
            text.fontSize = fontSize * textResolution; text.alignment = alignment; text.color = new Color(.10f, .075f, .04f);
            text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        #endregion
    }
}
