using System;
using UnityEngine;

namespace DOD.HitBox
{
    /// <summary>
    /// ヒットボックス Json ファイルから動的にヒットボックスを生成するサンプルクラス
    /// </summary>
    public class SampleHitBox : MonoBehaviour
    {
        // トリガー用
        [SerializeField] private bool isTrigger;

        // ヒットボックスが格納された親オブジェクト
        private GameObject hitboxParent;

        #region エディター確認用
        // ヒットボックスJson
        [SerializeField] private TextAsset hitboxJson;

        private void Awake()
        {
            // hitboxJson が未設定の場合は AddComponent 経由での動的生成として扱い、
            // Initialize() を外部から呼び出す前提のためここでは生成しない
            if (hitboxJson == null)
            {
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<HitBoxData>(hitboxJson.text);
                // Debug.Log($"HitBox データを読み込みました。コライダー数: {data.colliders.Count}");

                Initialize(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"HitBox JSON の読み込みに失敗しました: {ex.Message}");
            }
        }
        #endregion

        /// <summary>
        /// ランタイム用。HitBoxData を渡してコライダーを生成する。
        /// isTrigger を true にすると OnTriggerEnter/Exit で当たり判定を受け取れる。
        /// </summary>
        public void Initialize(HitBoxData data, bool useTrigger = false)
        {
            isTrigger = useTrigger;
            CreateHitBox(data);
        }

        /// <summary> ヒットボックスの有効/無効 </summary>
        public void SetActive(bool active)
        {
            if (hitboxParent == null) return;
            hitboxParent.SetActive(active);
        }

        /// <summary>
        /// 親の変更後にコライダーのワールドサイズが変わらないよう、親スケール補正を更新する。
        /// </summary>
        public void RefreshScaleCompensation()
        {
            if (hitboxParent == null)
            {
                return;
            }

            ApplyInverseParentScale(hitboxParent.transform);
        }

        /// <summary> ヒットボックス生成 </summary>
        private void CreateHitBox(HitBoxData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[SampleHitBox] HitBoxData が null のためコライダー生成をスキップします。", this);
                return;
            }

            // プレイヤー原点を基準に HitBox 親を生成する。オフセットは設けず個々のコライダー座標で制御する。
            hitboxParent = CreateGameObject("HitBox", transform, Vector3.zero);

            // 親（VisualModel など）に lossyScale が掛かっていると、HitBox 全体もその倍率で拡大されてしまう。
            // 「1 セル = GridMover の 1 ユニット」を保証するため、ここで親スケールを打ち消す localScale を適用する。
            RefreshScaleCompensation();

            if (data.colliders == null || data.colliders.Count == 0)
            {
                Debug.LogWarning("[SampleHitBox] colliders が空です。JSON の colliders 設定を確認してください。", this);
                return;
            }

            foreach (var hitbox in data.colliders)
            {
                // ColliderData の x/y は「セル単位のグリッドオフセット」。
                // Unity 座標系では x=横(X軸), y=前方(Z軸) に対応する。
                // 1 セル = colliserSize ワールドユニットとしてスケールを掛ける。
                var position = new Vector3(hitbox.x * data.colliserSize, 0f, hitbox.y * data.colliserSize);
                var hitboxObject = CreateGameObject(hitbox.name, hitboxParent.transform, position);
                var hitboxCollider = hitboxObject.AddComponent<BoxCollider>();
                hitboxCollider.isTrigger = isTrigger;
                // colliserSize に基づいてコライダーの大きさを設定する（未設定だとデフォルト 1×1×1 固定になる）
                hitboxCollider.size = new Vector3(data.colliserSize, data.colliserSize, data.colliserSize);
            }
        }

        /// <summary>
        /// 親 Transform のワールドスケールを打ち消す localScale を設定する。
        /// HitBox を VisualModel（拡大されたモデル）の子として配置しても、
        /// 各 BoxCollider のサイズが GridMover のセル基準と一致するようにする。
        /// </summary>
        private static void ApplyInverseParentScale(Transform target)
        {
            if (target == null || target.parent == null)
            {
                return;
            }

            var parentScale = target.parent.lossyScale;
            // ゼロ除算回避（極小値は 1 として扱う）
            var safeX = Mathf.Approximately(parentScale.x, 0f) ? 1f : parentScale.x;
            var safeY = Mathf.Approximately(parentScale.y, 0f) ? 1f : parentScale.y;
            var safeZ = Mathf.Approximately(parentScale.z, 0f) ? 1f : parentScale.z;
            target.localScale = new Vector3(1f / safeX, 1f / safeY, 1f / safeZ);
        }

        /// <summary> ゲームオブジェクト生成 </summary>
        private GameObject CreateGameObject(string objectName, Transform parent, Vector3 position)
        {
            var newGameObject = new GameObject(objectName);

            var parentTransform = newGameObject.transform;
            parentTransform.SetParent(parent, worldPositionStays: false);
            parentTransform.localPosition = position;
            parentTransform.localRotation = Quaternion.identity;
            parentTransform.localScale = Vector3.one;

            return newGameObject;
        }
    }
}
