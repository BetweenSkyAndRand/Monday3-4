using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Player.Domain
{
    /// <summary>
    /// モデルにマテリアルを自動適用するクラス。
    /// 既存のPlayerModelIntegrationEditorのロジックを再利用・改善。
    /// </summary>
    public static class MaterialApplier
    {
#if UNITY_EDITOR
        /// <summary>
        /// モデルに対してマテリアルを自動適用する。
        /// スコアリング方式で最適なマテリアルを選択。
        /// </summary>
        /// <param name="modelRoot">対象のモデルRoot GameObject</param>
        /// <param name="searchFolders">マテリアル検索フォルダ</param>
        /// <param name="modelKeyword">モデルのキーワード（スコアリングに使用）</param>
        /// <returns>適用したマテリアルスロット数</returns>
        public static int AutoApplyMaterials(GameObject modelRoot, string[] searchFolders, string modelKeyword)
        {
            if (modelRoot == null)
            {
                return 0;
            }

            // マテリアルを検索
            var candidateMaterials = LoadMaterials(searchFolders);
            if (candidateMaterials.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[MaterialApplier] 候補マテリアルが見つかりませんでした");
                return 0;
            }

            var rendererList = modelRoot.GetComponentsInChildren<Renderer>(true);
            var appliedCount = 0;

            foreach (var renderer in rendererList)
            {
                var sharedMaterials = renderer.sharedMaterials;
                var changed = false;

                for (var materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                {
                    var currentMaterial = sharedMaterials[materialIndex];

                    // 既に適正なマテリアルが設定されている場合はスキップ
                    if (currentMaterial != null && IsMaterialInFolders(currentMaterial, searchFolders))
                    {
                        continue;
                    }

                    // スコアリングで最適なマテリアルを検索
                    var hint = currentMaterial != null ? currentMaterial.name : renderer.name;
                    var bestMaterial = FindBestMaterial(candidateMaterials, hint, modelKeyword);

                    if (bestMaterial == null)
                    {
                        continue;
                    }

                    sharedMaterials[materialIndex] = bestMaterial;
                    changed = true;
                    appliedCount++;
                }

                if (changed)
                {
                    renderer.sharedMaterials = sharedMaterials;
                }
            }

            return appliedCount;
        }

        /// <summary>
        /// 指定フォルダからマテリアルを読み込む。
        /// </summary>
        private static Material[] LoadMaterials(string[] folders)
        {
            var materialList = new System.Collections.Generic.List<Material>();
            var materialGuids = AssetDatabase.FindAssets("t:Material", folders);

            foreach (var guid in materialGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                {
                    materialList.Add(material);
                }
            }

            return materialList.ToArray();
        }

        /// <summary>
        /// 候補マテリアルから最適なマテリアルを選択する。
        /// スコアリング方式。
        /// </summary>
        private static Material FindBestMaterial(Material[] candidates, string hint, string modelKeyword)
        {
            Material bestMaterial = null;
            var bestScore = int.MinValue;

            foreach (var candidate in candidates)
            {
                var score = ScoreMaterial(candidate.name, hint, modelKeyword);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMaterial = candidate;
                }
            }

            return bestScore > 0 ? bestMaterial : null;
        }

        /// <summary>
        /// マテリアルのスコアを計算する。
        /// より高いスコアのマテリアルが優先される。
        /// </summary>
        private static int ScoreMaterial(string candidateName, string hint, string modelKeyword)
        {
            if (string.IsNullOrEmpty(candidateName))
            {
                return 0;
            }

            var lowerCandidate = candidateName.ToLowerInvariant();
            var lowerHint = string.IsNullOrEmpty(hint) ? string.Empty : hint.ToLowerInvariant();
            var lowerKeyword = string.IsNullOrEmpty(modelKeyword) ? string.Empty : modelKeyword.ToLowerInvariant();
            var score = 0;

            // Hint（元のマテリアル名やRenderer名）との一致度
            if (!string.IsNullOrEmpty(lowerHint))
            {
                // 完全一致は最高スコア
                if (lowerCandidate == lowerHint)
                {
                    score += 1000;
                }

                // 部分一致
                if (lowerCandidate.Contains(lowerHint) || lowerHint.Contains(lowerCandidate))
                {
                    score += 120;
                }

                // トークン単位での一致
                var tokens = lowerHint.Split(new[] { '_', '-', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokens)
                {
                    if (token.Length >= 3 && lowerCandidate.Contains(token))
                    {
                        score += 30;
                    }
                }
            }

            // モデルキーワードとの一致度
            if (!string.IsNullOrEmpty(lowerKeyword) && lowerCandidate.Contains(lowerKeyword))
            {
                score += 80;
            }

            // URP対応マテリアルを優先
            if (lowerCandidate.Contains("urp") || lowerCandidate.Contains("lit"))
            {
                score += 10;
            }

            return score;
        }

        /// <summary>
        /// マテリアルが指定フォルダ内に存在するかチェック。
        /// </summary>
        private static bool IsMaterialInFolders(Material material, string[] folders)
        {
            if (material == null)
            {
                return false;
            }

            var materialPath = AssetDatabase.GetAssetPath(material);
            foreach (var folder in folders)
            {
                if (!string.IsNullOrEmpty(folder) && materialPath.StartsWith(folder, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
#else
        public static int AutoApplyMaterials(GameObject modelRoot, string[] searchFolders, string modelKeyword)
        {
            UnityEngine.Debug.LogWarning("[MaterialApplier] マテリアル自動適用はEditor専用機能です");
            return 0;
        }
#endif
    }
}

