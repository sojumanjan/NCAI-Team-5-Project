using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Taegeon
{
    // Keep the Canvas root enabled; only the menu content is hidden.
    [DisallowMultipleComponent]
    public sealed class MenuEscapeToggle : MonoBehaviour
    {
        [SerializeField] private GameObject menuRoot;

        private void Awake()
        {
            if (menuRoot == null)
            {
                var child = transform.Find("MenuRoot");
                if (child != null) menuRoot = child.gameObject;
            }
            Close();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Toggle();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
#endif
        }

        public void Toggle()
        {
            if (menuRoot == null) return;
            if (menuRoot.activeSelf) Close(); else Open();
        }

        public void Open()
        {
            if (menuRoot == null) return;
            var panel = menuRoot.transform.Find("Option/Setting Panel");
            if (panel != null) panel.gameObject.SetActive(true);
            menuRoot.SetActive(true);
        }

        public void Close()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
        }
    }
}