using System;
using System.Reflection;
using UnityEngine;

namespace Taegeon
{
    /// <summary>씬의 컨트롤러 감도 변수를 공통 설정창에 연결합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class MouseSensitivityTarget : MonoBehaviour
    {
        #region 연결 설정
        [Header("감도를 사용할 컨트롤러")]
        [Tooltip("가능하면 씬의 컨트롤러 컴포넌트를 직접 연결합니다.")]
        [SerializeField] private MonoBehaviour targetController;
        [Tooltip("직접 연결하지 않을 때 찾을 클래스 이름입니다. 네임스페이스 포함 이름도 가능합니다.")]
        [SerializeField] private string scriptName = "Taegeon.FirstPersonExplorer";
        [Tooltip("float 형식의 감도 필드 이름입니다. private 필드도 연결할 수 있습니다.")]
        [SerializeField] private string variableName = "mouseSensitivity";
        [Header("슬라이더 양 끝에 대응하는 실제 감도")]
        [SerializeField] private float minimum = 0.02f;
        [SerializeField] private float maximum = 0.5f;
        private FieldInfo field;
        private bool initialized;
        public bool IsReady => Initialize();
        private string SaveKey => "Taegeon.Sensitivity." + gameObject.scene.path + "." + scriptName + "." + variableName;
        #endregion

        #region 연결 및 초기값
        /// <summary>씬 시작 시 저장된 감도를 복원합니다.</summary>
        private void Start()
        {
            Initialize();
        }

        /// <summary>감도 필드를 확인하고 이 씬에 저장된 값을 한 번 불러옵니다.</summary>
        private bool Initialize()
        {
            if (initialized) return targetController != null && field != null;
            if (maximum <= minimum) return false;
            if (targetController == null)
            {
                MonoBehaviour match = null;
                foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null) continue;
                    var type = component.GetType();
                    if (type.FullName != scriptName && type.Name != scriptName) continue;
                    // 같은 클래스가 여러 개면 잘못 연결하지 않고 직접 지정하도록 합니다.
                    if (match != null) return false;
                    match = component;
                }
                targetController = match;
            }
            if (targetController == null) return false;
            for (Type type = targetController.GetType(); type != null; type = type.BaseType)
            {
                field = type.GetField(variableName, BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) break;
            }
            if (field == null || field.FieldType != typeof(float) || field.IsInitOnly) return false;
            initialized = true;
            if (PlayerPrefs.HasKey(SaveKey))
                field.SetValue(targetController, Mathf.Clamp(PlayerPrefs.GetFloat(SaveKey), minimum, maximum));
            return true;
        }
        #endregion

        #region 감도 읽기 및 변경
        /// <summary>현재 컨트롤러의 실제 감도를 읽습니다.</summary>
        public float ReadValue()
        {
            return Initialize() ? (float)field.GetValue(targetController) : minimum;
        }

        /// <summary>실제 감도에 대응하는 슬라이더 위치를 반환합니다.</summary>
        public float ReadNormalized()
        {
            return Mathf.InverseLerp(minimum, maximum, ReadValue());
        }

        /// <summary>슬라이더 위치에 해당하는 감도를 저장 없이 미리 적용합니다.</summary>
        public void Preview(float normalized)
        {
            if (Initialize()) field.SetValue(targetController, Mathf.Lerp(minimum, maximum, normalized));
        }

        /// <summary>취소 시 이전의 실제 감도를 그대로 복원합니다.</summary>
        public void Restore(float value)
        {
            if (Initialize()) field.SetValue(targetController, value);
        }

        /// <summary>현재 감도를 이 씬의 설정으로 저장합니다.</summary>
        public void Save()
        {
            if (!Initialize()) return;
            PlayerPrefs.SetFloat(SaveKey, ReadValue());
            PlayerPrefs.Save();
        }
        #endregion
    }
}
