using System;
using Game.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace Game.Search
{
    /// <summary>
    /// 探索終了の確認ダイアログ。表示中はゲーム内の時間を止め、閉じると元の速度へ戻す。
    /// 死亡や崩壊による強制終了では確認を出さず、開いていれば閉じるだけにする。
    /// </summary>
    public class SearchEndConfirmDialogView : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private SearchController searchController;
        [SerializeField] private InteractTextSettings textSettings;

        [Header("表示要素")]
        [Tooltip("ダイアログ全体のルート。このコンポーネントとは別のオブジェクトにすること。")]
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI confirmButtonText;
        [SerializeField] private TextMeshProUGUI cancelButtonText;

        private Action onConfirmed;
        private float timeScaleBeforeOpen = 1f;

        /// <summary>
        /// ダイアログが開いているかどうか。インタラクトの二重入力やポーズ入力を弾くために参照する。
        /// </summary>
        public static bool IsOpen { get; private set; }

        private void Awake()
        {
            // 前回プレイの状態が static に残らないようにする
            IsOpen = false;

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }

            ApplyTexts();
            SetDialogVisible(false);
        }

        private void OnEnable()
        {
            if (searchController != null)
            {
                searchController.OnSearchEnded += Close;
            }
        }

        private void OnDisable()
        {
            if (searchController != null)
            {
                searchController.OnSearchEnded -= Close;
            }
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelClicked);
            }

            // 開いたまま破棄されても時間が止まったままにならないようにする
            Close();
        }

        /// <summary>
        /// ダイアログを開き、時間を停止する。「終了する」が押されたときだけ onConfirmedAction を呼ぶ。
        /// </summary>
        public void Open(Action onConfirmedAction)
        {
            if (IsOpen)
            {
                return;
            }

            onConfirmed = onConfirmedAction;
            IsOpen = true;
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
            SetDialogVisible(true);
        }

        private void OnConfirmClicked()
        {
            var confirmedAction = onConfirmed;
            Close();
            confirmedAction?.Invoke();
        }

        private void OnCancelClicked()
        {
            Close();
        }

        private void Close()
        {
            onConfirmed = null;
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            Time.timeScale = timeScaleBeforeOpen;
            SetDialogVisible(false);
        }

        private void SetDialogVisible(bool visible)
        {
            if (dialogRoot != null)
            {
                dialogRoot.SetActive(visible);
            }
        }

        private void ApplyTexts()
        {
            if (textSettings == null)
            {
                return;
            }

            if (messageText != null)
            {
                messageText.text = textSettings.searchEndConfirmMessage;
            }

            if (confirmButtonText != null)
            {
                confirmButtonText.text = textSettings.searchEndConfirmYesText;
            }

            if (cancelButtonText != null)
            {
                cancelButtonText.text = textSettings.searchEndConfirmNoText;
            }
        }
    }
}
