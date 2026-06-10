using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Script.UI.UI_Script 
{
    public class ConfirmDialog : MonoBehaviour
    {
        public static ConfirmDialog Instance { get; private set; }

        [Header("UI References")]
        public GameObject dialogPanel;
        public TextMeshProUGUI questionText;
        public UnityEngine.UI.Button yesButton;
        public UnityEngine.UI.Button noButton;

        private Action onYesCallback;
        
        private Action onNoCallback; 

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            yesButton.onClick.AddListener(OnYesClicked);
            noButton.onClick.AddListener(OnNoClicked);

            Hide();
        }

        /// <summary>
        /// Gọi hàm này để hiển thị bảng câu hỏi
        /// </summary>
        /// <param name="question">Nội dung câu hỏi</param>
        /// <param name="onYes">Hàm sẽ chạy khi bấm Yes</param>
        /// <param name="onNo">Hàm sẽ chạy khi bấm No (Có thể để trống)</param>
        public void Show(string question, Action onYes, Action onNo = null)
        {
            questionText.text = question;
            
            onYesCallback = onYes;
            onNoCallback = onNo;

            dialogPanel.SetActive(true);
            AudioManager.Instance.PlaySFX(SoundNames.SfxWarning);
            //transform.SetAsLastSibling(); 
        }

        public void Hide()
        {
            dialogPanel.SetActive(false);
            
            onYesCallback = null;
            onNoCallback = null;
        }

        private void OnYesClicked()
        {
            onYesCallback?.Invoke();
            Hide(); 
        }

        private void OnNoClicked()
        {
            onNoCallback?.Invoke();
            Hide(); 
        }
    }
}