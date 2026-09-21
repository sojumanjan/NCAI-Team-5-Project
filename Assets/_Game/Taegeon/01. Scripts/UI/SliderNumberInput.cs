using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Taegeon
{
    [DisallowMultipleComponent]
    public sealed class SliderNumberInput : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_InputField input;
        private float observedValue;
        private bool editing;

        private void OnEnable()
        {
            if (slider == null || input == null) return;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 3;
            slider.onValueChanged.AddListener(OnSliderChanged);
            input.onValueChanged.AddListener(OnTextChanged);
            input.onEndEdit.AddListener(OnEndEdit);
            editing = false;
            Refresh();
        }

        private void OnDisable()
        {
            if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
            if (input == null) return;
            input.onValueChanged.RemoveListener(OnTextChanged);
            input.onEndEdit.RemoveListener(OnEndEdit);
        }

        private void LateUpdate()
        {
            // Settings can restore a saved value using SetValueWithoutNotify.
            if (slider != null && input != null && !Mathf.Approximately(observedValue, slider.value))
                Refresh();
        }

        private void OnSliderChanged(float value)
        {
            observedValue = value;
            if (!editing) Refresh();
        }

        private void OnTextChanged(string text)
        {
            int number;
            if (!int.TryParse(text, out number)) return;
            number = Mathf.Clamp(number, 1, 100);
            editing = true;
            slider.normalizedValue = (number - 1) / 99f;
            observedValue = slider.value;
            editing = false;
        }

        private void OnEndEdit(string text)
        {
            OnTextChanged(text);
            Refresh();
        }

        private void Refresh()
        {
            observedValue = slider.value;
            int number = Mathf.Clamp(Mathf.RoundToInt(1f + slider.normalizedValue * 99f), 1, 100);
            input.SetTextWithoutNotify(number.ToString());
        }
    }
}