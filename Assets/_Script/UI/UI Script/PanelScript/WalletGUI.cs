using System.Collections;
using TMPro;
using UnityEngine;

namespace _Script.UI.UI_Script.PanelScript
{
    public class WalletGUI : MonoBehaviour
    {
        public static WalletGUI Instance;
        [SerializeField] private RectTransform walletPanel;
        [SerializeField] private TextMeshProUGUI currentCoinText;
        [SerializeField] private float displayDuration = 2f;

        private Coroutine _hidePanelCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            WalletManager.OnCoinChanged += HandleCoinChange;

            if (walletPanel != null) walletPanel.gameObject.SetActive(false);
        }

        public void HandleCoinChange(int coin)
        {
            if (walletPanel == null || currentCoinText == null) return;

            currentCoinText.text = coin.ToString();

            if (!walletPanel.gameObject.activeSelf) walletPanel.gameObject.SetActive(true);

            if (_hidePanelCoroutine != null) StopCoroutine(_hidePanelCoroutine);

            _hidePanelCoroutine = StartCoroutine(HidePanelAfterDelay(displayDuration));
        }

        private IEnumerator HidePanelAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            walletPanel.gameObject.SetActive(false);

            _hidePanelCoroutine = null;
        }

        private void OnDestroy()
        {
            WalletManager.OnCoinChanged -= HandleCoinChange;
        }
    }
}