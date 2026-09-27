using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Taegeon
{
    /// <summary>Escape 씬 안에서만 생성되고 씬과 함께 사라지는 화면 전환입니다.</summary>
    public sealed class EscapeSceneFade : MonoBehaviour
    {
        private CanvasGroup overlay;
        private Coroutine transition;
        private bool exiting;

        private void Awake()
        {
            var root = new GameObject("Escape Scene Fade", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            overlay = root.GetComponent<CanvasGroup>();
            overlay.alpha = 1f;
            overlay.blocksRaycasts = true;
            var panel = new GameObject("Black", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = Color.black;
        }

        private void Start()
        {
            if (!exiting) transition = StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            yield return FadeTo(0f, 1f);
            overlay.blocksRaycasts = false;
            overlay.gameObject.SetActive(false);
            transition = null;
        }

        public void CoverAndReturn(Action returnToHub)
        {
            if (exiting) return;
            exiting = true;
            if (transition != null) StopCoroutine(transition);
            overlay.gameObject.SetActive(true);
            overlay.blocksRaycasts = true;
            transition = StartCoroutine(Cover(returnToHub));
        }

        private IEnumerator Cover(Action returnToHub)
        {
            yield return FadeTo(1f, .8f);
            // 완전히 검어진 프레임을 그린 다음 기존 허브 복귀를 실행합니다.
            yield return new WaitForEndOfFrame();
            returnToHub?.Invoke();
        }

        private IEnumerator FadeTo(float target, float seconds)
        {
            float start = overlay.alpha;
            double started = Time.realtimeSinceStartupAsDouble;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                overlay.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / seconds));
                yield return null;
                elapsed = (float)(Time.realtimeSinceStartupAsDouble - started);
            }
            overlay.alpha = target;
        }

        private void OnDestroy()
        {
            if (overlay != null) Destroy(overlay.gameObject);
        }
    }
}