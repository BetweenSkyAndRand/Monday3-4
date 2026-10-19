using System.Collections.Generic;
using UnityEngine;
using Game.Interaction;
using Game.Treasure;
using Game.Utility;
using UnityEngine.Serialization;

namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤーの近くにある IInteractable を探し、
    /// 現在の対象をUIに表示し、インタラクト入力が来たら実行するクラス。
    /// </summary>
    public class PlayerInteract : MonoBehaviour
    {
        [Header("インタラクト設定")]

        [SerializeField] private float interactDistance = 1.5f;

        /// <summary>デバッグログを有効化するフラグ。</summary>
        [SerializeField] private bool enableDebugLog;

        /// <summary>
        /// 入力を受け取るための GameInputReader。
        /// OnInteractイベントを読み取り、Eキー等の入力に反応する。
        /// </summary>
        [FormerlySerializedAs("inputReader")] [SerializeField] private GameInputReader gameInputReader;

        /// <summary>
        /// インタラクトを禁止する要因の一覧。
        /// Inspector で BuildModeMovementBlocker 等をアサインする。
        /// いずれか 1 つでも IsInteractBlocked が true なら UI を非表示にしてインタラクト不可にする。
        /// </summary>
        [SerializeField] private MonoBehaviour[] interactBlockerObjects;

        //現在のインタラクト対象を保持するフィールド。見つからない場合はnull
        private IInteractableObject currentInteractable;

        // ホイールで選択中の対象。候補から外れたら先頭候補に戻る
        private IInteractableObject selectedInteractable;

        private readonly List<InteractCandidate> interactCandidates = new List<InteractCandidate>();

        private readonly struct InteractCandidate
        {
            public readonly IInteractableObject Interactable;
            public readonly int Priority;
            public readonly float Distance;

            public InteractCandidate(IInteractableObject interactable, int priority, float distance)
            {
                Interactable = interactable;
                Priority = priority;
                Distance = distance;
            }
        }

        //前回ログを出力したインタラクト対象を保持するフィールド。
        private IInteractableObject lastLoggedInteractable;

        /// <summary>キャスト済みの IInteractBlocker 配列。</summary>
        private IInteractBlocker[] interactBlockers;

        /// <summary>
        /// OverlapSphereNonAlloc 用の使い回しバッファ。
        /// 毎フレーム新しい配列を作らないようにして GC を減らす。
        /// </summary>
        private static readonly Collider[] OverlapBuffer = new Collider[16];

        private void Awake()
        {
            if (gameInputReader == null)
            {
                gameInputReader = GetComponent<GameInputReader>();
            }

            // Inspector アサインの MonoBehaviour を IInteractBlocker にキャストする。
            interactBlockers = MonoBehaviourCaster.CastArray<IInteractBlocker>(interactBlockerObjects, nameof(PlayerInteract));
        }

        /// <summary>
        /// 有効化されたときに、GameInputReader のインタラクトイベントを購読する。
        /// </summary>
        private void OnEnable()
        {
            if (gameInputReader != null)
            {
                gameInputReader.OnInteract += OnInteractPerformed;
                gameInputReader.OnInteractTargetScroll += OnInteractTargetScrolled;
            }
        }

        /// <summary>
        /// 無効化されたときに、GameInputReader のインタラクトイベントを解除する。
        /// </summary>
        private void OnDisable()
        {
            if (gameInputReader != null)
            {
                gameInputReader.OnInteract -= OnInteractPerformed;
                gameInputReader.OnInteractTargetScroll -= OnInteractTargetScrolled;
            }
        }

        /// <summary>
        /// 毎フレーム、近くのインタラクト対象を探す。
        /// </summary>
        private void Update()
        {
            FindNearestInteractable();
        }

        /// <summary>
        /// プレイヤーの周囲から最も近い IInteractable を持つものを currentInteractable にセット。
        /// その後、 UIとログを更新
        /// </summary>
        private void FindNearestInteractable()
        {
            // インタラクト禁止中は UI を隠して対象もクリアする
            if (IsAnyInteractBlocked())
            {
                currentInteractable = null;
                if (InteractUI.Instance != null)
                {
                    InteractUI.Instance.Hide();
                }
                if (TreasureBoxIconUI.Instance != null)
                {
                    TreasureBoxIconUI.Instance.Hide();
                }
                return;
            }
            // プレイヤーの少し上を中心にする
            Vector3 center = transform.position + Vector3.up * 0.5f;

            // 範囲内の Collider を取得する
            int hitCount = Physics.OverlapSphereNonAlloc(center, interactDistance, OverlapBuffer);

            interactCandidates.Clear();

            // 見つかった Collider を1つずつ確認する
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = OverlapBuffer[i];
                if (col == null) continue;

                // IInteractableObject を持っているものだけ対象にする
                IInteractableObject interactable = col.GetComponent<IInteractableObject>();
                if (interactable == null) continue;

                // 現在インタラクト不可（テキストが空）のものは候補に入れない
                if (string.IsNullOrEmpty(interactable.GetInteractText()))
                {
                    continue;
                }

                var priority = interactable is IInteractPriority priorityProvider ? priorityProvider.InteractPriority : 0;
                var dist = Vector3.Distance(transform.position, col.transform.position);

                // 1つのオブジェクトが複数のコライダーを持つ場合に同じ対象が重複すると、ホイール切り替えで同じ対象を行き来してしまう
                var existingIndex = FindCandidateIndex(interactable);
                if (existingIndex >= 0)
                {
                    if (dist < interactCandidates[existingIndex].Distance)
                    {
                        interactCandidates[existingIndex] = new InteractCandidate(interactable, priority, dist);
                    }

                    continue;
                }

                interactCandidates.Add(new InteractCandidate(interactable, priority, dist));
            }

            // 優先順位が高いものを先頭に、同じ優先順位なら近い順にする
            interactCandidates.Sort((left, right) =>
            {
                var priorityComparison = right.Priority.CompareTo(left.Priority);
                return priorityComparison != 0 ? priorityComparison : left.Distance.CompareTo(right.Distance);
            });

            currentInteractable = ResolveSelectedInteractable();

            // 変化があったときだけログ出力
            if (currentInteractable != lastLoggedInteractable)
            {
                // string typeName = currentInteractable != null
                //     ? currentInteractable.GetType().Name
                //     : "なし";
                // UnityEngine.Debug.Log($"[PlayerInteract] インタラクト対象: {typeName}");
                lastLoggedInteractable = currentInteractable;
            }

            // UI 更新
            // GetInteractText() が空の場合はインタラクト無効扱いとして UI を非表示にし、
            // currentInteractable も null にすることで Interact() が呼ばれないようにする。
            UpdateInteractUI();
        }

        /// <summary>
        /// インタラクトUIを更新する。
        /// GetInteractText()が空の場合はUIを非表示にし、インタラクト対象をクリアする。
        /// </summary>
        private void UpdateInteractUI()
        {
            if (InteractUI.Instance == null)
            {
                return;
            }

            if (currentInteractable != null)
            {
                var interactText = currentInteractable.GetInteractText();

                if (string.IsNullOrEmpty(interactText))
                {
                    // GetInteractText()が空文字またはnullを返す場合
                    // = このオブジェクトは現在インタラクト不可能
                    // 例: 探索中のビルドオブジェクト、既に開いた宝箱など
                    // if (enableDebugLog)
                    // {
                    //     UnityEngine.Debug.Log($"[PlayerInteract] インタラクト不可: {currentInteractable.GetType().Name} (GetInteractText() が空)");
                    // }

                    currentInteractable = null;
                    InteractUI.Instance.Hide();
                    if (TreasureBoxIconUI.Instance != null)
                    {
                        TreasureBoxIconUI.Instance.Hide();
                    }
                }
                else
                {
                    // 有効なテキストが返された場合、UIを表示
                    // if (enableDebugLog)
                    // {
                    //     UnityEngine.Debug.Log($"[PlayerInteract] UIを表示: \"{interactText}\"");
                    // }

                    if (interactCandidates.Count > 1)
                    {
                        interactText = $"{interactText}  [{FindCandidateIndex(currentInteractable) + 1}/{interactCandidates.Count}] Scroll to switch";
                    }

                    InteractUI.Instance.Show(
                        interactText,
                        currentInteractable is IInteractDisplayProvider displayProvider
                            ? displayProvider.GetInteractDisplayData()
                            : (ItemIconDisplayData?)null);

                    // 宝箱など、アイコン表示を提供するオブジェクトの場合は専用UIも表示する。
                    // 提供されていない場合は専用UIを隠す（前回の宝箱の残像を消す）。
                    if (TreasureBoxIconUI.Instance != null)
                    {
                        if (currentInteractable is ITreasureBoxIconProvider iconProvider)
                        {
                            TreasureBoxIconUI.Instance.Show(
                                iconProvider.GetTreasureIcon(),
                                iconProvider.GetTreasureQuantity(),
                                iconProvider.GetTreasureLabel());
                        }
                        else
                        {
                            TreasureBoxIconUI.Instance.Hide();
                        }
                    }
                }
            }
            else
            {
                // インタラクト対象がない場合、UIを非表示
                InteractUI.Instance.Hide();
                if (TreasureBoxIconUI.Instance != null)
                {
                    TreasureBoxIconUI.Instance.Hide();
                }
            }
        }

        /// <summary>
        /// インタラクト入力が発生したときの処理。
        /// currentInteractableがあれば、その対象のInteract()を呼ぶ。
        /// </summary>
        private void OnInteractPerformed()
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
            }
        }

        // 選択中の対象が候補に残っていればそれを、なければ先頭候補を返す
        private IInteractableObject ResolveSelectedInteractable()
        {
            if (interactCandidates.Count == 0)
            {
                selectedInteractable = null;
                return null;
            }

            if (FindCandidateIndex(selectedInteractable) < 0)
            {
                selectedInteractable = interactCandidates[0].Interactable;
            }

            return selectedInteractable;
        }

        private int FindCandidateIndex(IInteractableObject targetInteractable)
        {
            for (var candidateIndex = 0; candidateIndex < interactCandidates.Count; candidateIndex++)
            {
                if (interactCandidates[candidateIndex].Interactable == targetInteractable)
                {
                    return candidateIndex;
                }
            }

            return -1;
        }

        private void OnInteractTargetScrolled(int step)
        {
            if (interactCandidates.Count < 2)
            {
                return;
            }

            var currentIndex = Mathf.Max(0, FindCandidateIndex(selectedInteractable));
            var nextIndex = (currentIndex + step + interactCandidates.Count) % interactCandidates.Count;
            selectedInteractable = interactCandidates[nextIndex].Interactable;
        }

        /// <summary>
        /// いずれかの IInteractBlocker が「インタラクト禁止」を報告しているか確認する。
        /// </summary>
        private bool IsAnyInteractBlocked()
        {
            foreach (var blocker in interactBlockers)
            {
                if (blocker.IsInteractBlocked)
                {
                    return true;
                }
            }

            return false;
        }

        // エディタ上でインタラクト範囲を可視化
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, interactDistance);
        }
    }
}
