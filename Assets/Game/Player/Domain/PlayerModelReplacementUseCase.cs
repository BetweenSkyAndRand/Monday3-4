using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Player.Domain
{
    /// <summary>
    /// プレイヤーモデル置き換えのビジネスロジックを担当するユースケースクラス。
    /// 純粋なロジック処理とEditor APIの分離を意識。
    /// </summary>
    public class PlayerModelReplacementUseCase
    {
        /// <summary>
        /// プレイヤーモデルを置き換える。
        /// </summary>
        /// <param name="targetPlayer">置き換え対象のプレイヤーGameObject</param>
        /// <param name="newModelPrefab">新しいモデルのPrefab</param>
        /// <param name="config">置き換え設定</param>
        /// <returns>置き換え結果</returns>
        public ReplaceResult Execute(
            GameObject targetPlayer,
            GameObject newModelPrefab,
            PlayerModelConfig config)
        {
            // 1. 入力検証
            var validationResult = ValidateInput(targetPlayer, newModelPrefab, config);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            // 2. 既存のビジュアルモデルを削除
            RemoveExistingVisuals(targetPlayer);

            // 3. 新しいモデルをインスタンス化
            var modelInstance = InstantiateModel(newModelPrefab, targetPlayer);
            if (modelInstance == null)
            {
                return ReplaceResult.Failure("モデルのインスタンス化に失敗しました");
            }

            // 4. スケール計算と適用
            var scaleResult = ApplyScale(modelInstance, config);
            if (!scaleResult.IsValid)
            {
                Object.DestroyImmediate(modelInstance);
                return ReplaceResult.Failure($"スケール適用に失敗: {scaleResult.Message}");
            }

            // 5. 地面合わせ（オプション）
            if (config.AlignToGround)
            {
                AlignModelToGround(modelInstance, targetPlayer);
            }

            // 6. マテリアル自動適用（オプション）
            if (config.AutoApplyMaterials)
            {
                ApplyMaterials(modelInstance, config);
            }

            // 7. Animatorコンポーネントの確認
            CheckAnimatorComponent(modelInstance);

#if UNITY_EDITOR
            // 8. Undo登録とDirtyマーク
            Undo.RecordObject(targetPlayer, "Replace Player Model");
            EditorUtility.SetDirty(targetPlayer);
#endif

            // UnityEngine.Debug.Log($"[PlayerModelReplacement] モデル置き換え成功: {newModelPrefab.name} → {targetPlayer.name}");
            return ReplaceResult.SuccessResult(modelInstance);
        }

        /// <summary>
        /// 入力パラメータを検証する。
        /// </summary>
        private ReplaceResult ValidateInput(GameObject targetPlayer, GameObject newModelPrefab, PlayerModelConfig config)
        {
            if (targetPlayer == null)
            {
                return ReplaceResult.Failure("対象プレイヤーが指定されていません");
            }

            if (newModelPrefab == null)
            {
                return ReplaceResult.Failure("新しいモデルが指定されていません");
            }

            if (config == null)
            {
                return ReplaceResult.Failure("設定が指定されていません");
            }

            // ConfigのTargetSizeが有効かチェック
            var targetSize = config.TargetSize;
            if (targetSize.x <= 0 || targetSize.y <= 0 || targetSize.z <= 0)
            {
                return ReplaceResult.Failure("設定のターゲットサイズが無効です");
            }

            return ReplaceResult.SuccessResult(null);
        }

        /// <summary>
        /// 既存のビジュアルモデルを削除する。
        /// Player本体のRendererと子オブジェクトのビジュアルを削除（ロジックは保持）。
        /// </summary>
        private void RemoveExistingVisuals(GameObject targetPlayer)
        {
            // 1. Player本体のビジュアルコンポーネントを削除
            RemovePlayerVisualComponents(targetPlayer);

            // 2. 子オブジェクトのうち、ビジュアルのみのものを削除
            var childrenToRemove = new System.Collections.Generic.List<Transform>();

            foreach (Transform child in targetPlayer.transform)
            {
                // MonoBehaviourを持つオブジェクトは削除しない（ロジックコンポーネント）
                // Playerタグを持つオブジェクトも削除しない
                if (child.GetComponent<MonoBehaviour>() == null && !child.CompareTag("Player"))
                {
                    childrenToRemove.Add(child);
                }
            }

            foreach (var child in childrenToRemove)
            {
                // UnityEngine.Debug.Log($"[PlayerModelReplacement] 既存ビジュアル（子オブジェクト）を削除: {child.name}");
                Object.DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>
        /// Player本体のビジュアルコンポーネント（MeshRenderer、MeshFilter等）を削除する。
        /// CapsuleColliderは物理判定に使用するため保持。
        /// </summary>
        private void RemovePlayerVisualComponents(GameObject targetPlayer)
        {
            // MeshRendererを削除（Capsuleのビジュアル表示）
            var meshRenderer = targetPlayer.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                UnityEngine.Debug.Log("[PlayerModelReplacement] Player本体のMeshRendererを削除");
                Object.DestroyImmediate(meshRenderer);
            }

            // MeshFilterを削除（Capsuleのメッシュ）
            var meshFilter = targetPlayer.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                UnityEngine.Debug.Log("[PlayerModelReplacement] Player本体のMeshFilterを削除");
                Object.DestroyImmediate(meshFilter);
            }

            // SkinnedMeshRendererも削除（念のため）
            var skinnedMeshRenderer = targetPlayer.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                UnityEngine.Debug.Log("[PlayerModelReplacement] Player本体のSkinnedMeshRendererを削除");
                Object.DestroyImmediate(skinnedMeshRenderer);
            }

            // Rendererを削除（基底クラス、念のため）
            var renderer = targetPlayer.GetComponent<Renderer>();
            if (renderer != null && renderer.GetType() == typeof(Renderer))
            {
                UnityEngine.Debug.Log("[PlayerModelReplacement] Player本体のRendererを削除");
                Object.DestroyImmediate(renderer);
            }

            UnityEngine.Debug.Log("[PlayerModelReplacement] Player本体のビジュアルコンポーネント削除完了");
        }

        /// <summary>
        /// 新しいモデルをインスタンス化する。
        /// </summary>
        private GameObject InstantiateModel(GameObject prefab, GameObject parent)
        {
#if UNITY_EDITOR
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
#else
            var instance = Object.Instantiate(prefab, parent.transform);
#endif
            instance.name = "VisualModel";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            return instance;
        }

        /// <summary>
        /// スケールを計算して適用する。
        /// </summary>
        private ScaleResult ApplyScale(GameObject modelInstance, PlayerModelConfig config)
        {
            // モデルのBoundsを取得
            if (!ModelBoundsCalculator.TryGetBounds(modelInstance, out var bounds))
            {
                return ScaleResult.Failure("モデルのBoundsを取得できません（Rendererが存在しない可能性があります）");
            }

            UnityEngine.Debug.Log($"[PlayerModelReplacement] 元のモデルサイズ: {bounds.Size} (Width={bounds.Size.x}, Height={bounds.Size.y}, Depth={bounds.Size.z})");
            UnityEngine.Debug.Log($"[PlayerModelReplacement] ターゲットサイズ: {config.TargetSize} (Width={config.TargetWidth}, Height={config.TargetHeight}, Depth={config.TargetDepth})");

            // スケールを計算
            var scaleResult = ModelScaleCalculator.Calculate(
                bounds,
                config.TargetSize,
                config.PreserveAspectRatio);

            if (!scaleResult.IsValid)
            {
                return scaleResult;
            }

            // スケールを適用
            modelInstance.transform.localScale = scaleResult.LocalScale;

            UnityEngine.Debug.Log($"[PlayerModelReplacement] スケール適用: {scaleResult.LocalScale}");
            UnityEngine.Debug.Log($"[PlayerModelReplacement] アスペクト比維持: {config.PreserveAspectRatio}");
            return scaleResult;
        }

        /// <summary>
        /// モデルの底面を地面（Y=0）に合わせる。
        /// </summary>
        private void AlignModelToGround(GameObject modelInstance, GameObject parent)
        {
            // スケール適用後のBoundsを再取得
            if (!ModelBoundsCalculator.TryGetBounds(modelInstance, out var boundsAfterScale))
            {
                UnityEngine.Debug.LogWarning("[PlayerModelReplacement] 地面合わせ用のBounds取得に失敗しました");
                return;
            }

            // 親のY座標
            var parentY = parent.transform.position.y;

            UnityEngine.Debug.Log($"[PlayerModelReplacement] 地面合わせ前 - モデル底面Y: {boundsAfterScale.Min.y}, 親Y: {parentY}");

            // オフセットを計算
            var offset = ModelScaleCalculator.CalculateGroundAlignmentOffset(boundsAfterScale, parentY);

            // 位置を調整
            modelInstance.transform.position += offset;

            UnityEngine.Debug.Log($"[PlayerModelReplacement] 地面合わせ適用: オフセット={offset}, 最終位置={modelInstance.transform.position}");
        }

        /// <summary>
        /// マテリアルを自動適用する。
        /// </summary>
        private void ApplyMaterials(GameObject modelInstance, PlayerModelConfig config)
        {
#if UNITY_EDITOR
            var appliedCount = MaterialApplier.AutoApplyMaterials(
                modelInstance,
                config.MaterialFolders,
                modelInstance.name);
            
            UnityEngine.Debug.Log($"[PlayerModelReplacement] マテリアル自動適用: {appliedCount}スロット");
#else
            UnityEngine.Debug.LogWarning("[PlayerModelReplacement] マテリアル自動適用はEditor専用機能です");
#endif
        }

        /// <summary>
        /// Animatorコンポーネントの存在を確認する。
        /// </summary>
        private void CheckAnimatorComponent(GameObject modelInstance)
        {
            var animator = modelInstance.GetComponent<Animator>();
            if (animator != null)
            {
                UnityEngine.Debug.Log($"[PlayerModelReplacement] Animatorコンポーネントを検出: {animator.runtimeAnimatorController?.name ?? "（コントローラー未設定）"}");
            }
            else
            {
                UnityEngine.Debug.Log("[PlayerModelReplacement] Animatorコンポーネントは検出されませんでした");
            }
        }
    }
}

