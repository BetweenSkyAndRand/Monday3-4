using UnityEngine;

namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤーの攻撃時に当たり判定（HitBox）を視覚化するコンポーネント。
    /// HitBox のコライダー位置にキューブを表示し、攻撃範囲をプレイヤーにフィードバックする。
    /// </summary>
    [RequireComponent(typeof(HitBoxController))]
    public class AttackHitBoxVisualizer : MonoBehaviour
    {
        [Header("視覚化設定")]
        [Tooltip("HitBox 可視化を有効にするか")]
        [SerializeField] private bool enableVisualization = true;

        [Tooltip("HitBox 表示用のキューブマテリアル")]
        [SerializeField] private Material hitBoxMaterial;

        [Tooltip("HitBox のデフォルト色")]
        [SerializeField] private Color idleColor = new Color(0, 1, 0, 0.3f);  // 透明な緑

        [Tooltip("攻撃中の HitBox 色")]
        [SerializeField] private Color attackColor = new Color(1, 0, 0, 0.5f);  // 半透明な赤

        [Tooltip("HitBox キューブのサイズ")]
        [SerializeField] private float visualizationScale = 0.9f;

        [Tooltip("攻撃していないときも常に表示するか（デバッグ用）")]
        [SerializeField] private bool alwaysShow;

        private HitBoxController hitBoxController;
        private PlayerAttack playerAttack;
        private GameObject[] visualizationObjects;
        private Renderer[] visualizationRenderers;
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            hitBoxController = GetComponent<HitBoxController>();
            playerAttack = GetComponent<PlayerAttack>();
            propertyBlock = new MaterialPropertyBlock();

            if (hitBoxMaterial == null)
            {
                // デフォルトマテリアルを作成
                hitBoxMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                hitBoxMaterial.SetFloat("_Surface", 1); // Transparent
                hitBoxMaterial.SetFloat("_AlphaClip", 0);
                hitBoxMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                hitBoxMaterial.SetFloat("_ZWrite", 0);
                hitBoxMaterial.renderQueue = 3000;
            }
        }

        private void Start()
        {
            if (enableVisualization)
            {
                CreateVisualizationObjects();
            }
        }

        private void LateUpdate()
        {
            if (!enableVisualization || visualizationObjects == null)
            {
                return;
            }

            UpdateVisualizationPositions();
            UpdateVisualizationVisibility();
        }

        private void CreateVisualizationObjects()
        {
            // HitBoxController から現在の HitBox を取得
            var hitBoxes = hitBoxController.GetComponentsInChildren<PlayerWeaponHitBox>();
            if (hitBoxes.Length == 0)
            {
                return;
            }

            // 各 HitBox に対応する視覚化オブジェクトを作成
            visualizationObjects = new GameObject[hitBoxes.Length];
            visualizationRenderers = new Renderer[hitBoxes.Length];

            for (var i = 0; i < hitBoxes.Length; i++)
            {
                var hitBox = hitBoxes[i];
                var visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObj.name = $"HitBoxVisualization_{i}";
                visualObj.transform.SetParent(hitBox.transform, false);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;
                visualObj.transform.localScale = Vector3.one * visualizationScale;

                // コライダーは不要（視覚化のみ）
                var hitBoxCollider = visualObj.GetComponent<Collider>();
                if (hitBoxCollider != null)
                {
                    Destroy(hitBoxCollider);
                }

                // レンダラーにマテリアルを設定
                var meshRenderer = visualObj.GetComponent<Renderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.material = hitBoxMaterial;
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    meshRenderer.receiveShadows = false;

                    // 攻撃時の色を設定
                    propertyBlock.SetColor("_BaseColor", attackColor);
                    meshRenderer.SetPropertyBlock(propertyBlock);
                }

                visualizationObjects[i] = visualObj;
                visualizationRenderers[i] = meshRenderer;
            }

            // 初期状態では非表示
            UpdateVisualizationVisibility();
        }

        private void UpdateVisualizationPositions()
        {
            // HitBox の位置は親の Transform に追従するため、特別な更新は不要
            // ただし、HitBox が再生成された場合は再作成が必要
            if (visualizationObjects != null && visualizationObjects.Length > 0 && visualizationObjects[0] == null)
            {
                CreateVisualizationObjects();
            }
        }

        private void UpdateVisualizationVisibility()
        {
            if (visualizationObjects == null)
            {
                return;
            }

            // プレイヤーが攻撃中かどうかで表示/非表示を切り替え
            var isAttacking = playerAttack != null && playerAttack.IsAttacking;
            var shouldShow = alwaysShow || isAttacking;

            foreach (var visualObj in visualizationObjects)
            {
                if (visualObj != null)
                {
                    visualObj.SetActive(shouldShow);
                }
            }
        }

        /// <summary>
        /// 視覚化の有効/無効を切り替える。
        /// </summary>
        public void SetVisualizationEnabled(bool isEnabled)
        {
            enableVisualization = isEnabled;

            if (visualizationObjects != null)
            {
                foreach (var obj in visualizationObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(isEnabled);
                    }
                }
            }

            if (isEnabled && (visualizationObjects == null || visualizationObjects.Length == 0))
            {
                CreateVisualizationObjects();
            }
        }

        private void OnDestroy()
        {
            // 視覚化オブジェクトのクリーンアップ
            if (visualizationObjects != null)
            {
                foreach (var obj in visualizationObjects)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }
        }

        private void OnValidate()
        {
            // Inspector で設定変更時に表示を更新
            if (Application.isPlaying && visualizationObjects != null)
            {
                UpdateVisualizationVisibility();
            }
        }
    }
}





