using UnityEngine;
using UnityEditor;

namespace MapTest.EditorTools
{
    /// <summary>
    /// プレイヤーモデルと武器モデルの統合を支援するエディター拡張。
    /// Tools > MapTest > Player Model Integration からアクセス可能。
    /// </summary>
    public class PlayerModelIntegrationEditor : EditorWindow
    {
        private const float TargetCellWidth = 1f;
        private const float TargetCellDepth = 1f;
        private const float TargetCellHeight = 2f;

        private static readonly string[] CommonMaterialFolders =
        {
            "Assets/Material",
        };

        private static readonly string[] PlayerMaterialFolders =
        {
            "Assets/FreeAnimatedSpaceMan/Materials",
            "Assets/Material",
        };

        private static readonly string[] WeaponMaterialFolders =
        {
            "Assets/NanoPlasmaProductions/ElectricitySword2/Materials",
            "Assets/Material",
        };

        private GameObject spaceManPrefab;
        private GameObject electricitySwordPrefab;
        private GameObject targetPlayerObject;

        [MenuItem("Tools/4. Player Tools/02. Model Integration")]
        public static void ShowWindow()
        {
            var window = GetWindow<PlayerModelIntegrationEditor>("プレイヤーモデル統合");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // デフォルトでプレハブを読み込む
            spaceManPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/FreeAnimatedSpaceMan/Prefab/space_man_model.prefab");
            electricitySwordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/NanoPlasmaProductions/ElectricitySword2/Prefabs/ElectricitySword2.prefab");
        }

        private void OnGUI()
        {
            GUILayout.Label("プレイヤーモデル統合ツール", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // ========== プレイヤーモデルセクション ==========
            GUILayout.Label("1. プレイヤーモデル設定", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "SpaceManモデルを既存のプレイヤーオブジェクトに統合します。\n" +
                "既存の機能（移動、攻撃、入力等）は保持されます。",
                MessageType.Info);

            spaceManPrefab = (GameObject)EditorGUILayout.ObjectField(
                "SpaceMan プレハブ", spaceManPrefab, typeof(GameObject), false);

            targetPlayerObject = (GameObject)EditorGUILayout.ObjectField(
                "統合先プレイヤー", targetPlayerObject, typeof(GameObject), true);

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(spaceManPrefab == null || targetPlayerObject == null);
            if (GUILayout.Button("プレイヤーモデルを統合", GUILayout.Height(30)))
            {
                IntegratePlayerModel();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(20);

            // ========== 武器モデルセクション ==========
            GUILayout.Label("2. 武器モデル設定", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ElectricitySword2を武器システムに統合します。\n" +
                "WeaponDataアセットとWeaponPickupプレハブを作成します。",
                MessageType.Info);

            electricitySwordPrefab = (GameObject)EditorGUILayout.ObjectField(
                "ElectricitySword2 プレハブ", electricitySwordPrefab, typeof(GameObject), false);

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(electricitySwordPrefab == null);
            if (GUILayout.Button("武器データを作成", GUILayout.Height(30)))
            {
                CreateWeaponData();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(20);

            // ========== 完全統合ボタン ==========
            GUILayout.Label("3. 一括統合（推奨）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "プレイヤーモデルと武器を一括で統合します。\n" +
                "統合先プレイヤーオブジェクトを選択してください。",
                MessageType.Warning);

            EditorGUI.BeginDisabledGroup(
                spaceManPrefab == null ||
                electricitySwordPrefab == null ||
                targetPlayerObject == null);

            if (GUILayout.Button("全て統合して完了", GUILayout.Height(40)))
            {
                IntegrateAll();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void IntegratePlayerModel()
        {
            if (targetPlayerObject == null || spaceManPrefab == null)
            {
                EditorUtility.DisplayDialog("エラー", "プレイヤーオブジェクトとモデルを指定してください。", "OK");
                return;
            }

            Undo.RecordObject(targetPlayerObject, "Integrate Player Model");

            // 既存のビジュアルを削除（Playerタグ以外の子オブジェクト）
            var childrenToRemove = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in targetPlayerObject.transform)
            {
                if (!child.CompareTag("Player") && child.GetComponent<MonoBehaviour>() == null)
                {
                    childrenToRemove.Add(child);
                }
            }

            foreach (var child in childrenToRemove)
            {
                DestroyImmediate(child.gameObject);
            }

            // SpaceManモデルをインスタンス化して追加
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(spaceManPrefab, targetPlayerObject.transform);
            modelInstance.name = "SpaceManModel";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            ApplyAutomaticScale(modelInstance, TargetCellWidth, TargetCellDepth, TargetCellHeight);
            AlignModelBottomToParent(modelInstance);
            AutoApplyMaterials(modelInstance, PlayerMaterialFolders, "SpaceMan");

            // Animatorコンポーネントを確認
            var animator = modelInstance.GetComponent<Animator>();
            if (animator != null)
            {
                Debug.Log("[PlayerModelIntegration] Animatorが検出されました。アニメーション対応が可能です。");
            }

            EditorUtility.SetDirty(targetPlayerObject);
            Debug.Log($"[PlayerModelIntegration] SpaceManモデルを {targetPlayerObject.name} に統合しました。");
            EditorUtility.DisplayDialog("成功", "プレイヤーモデルの統合が完了しました。", "OK");
        }

        private void CreateWeaponData()
        {
            if (electricitySwordPrefab == null)
            {
                EditorUtility.DisplayDialog("エラー", "武器プレハブを指定してください。", "OK");
                return;
            }

            // WeaponDataフォルダが存在しなければ作成
            var weaponDataFolder = "Assets/Game/Weapon/Data";
            if (!AssetDatabase.IsValidFolder(weaponDataFolder))
            {
                var parentFolder = "Assets/Game/Weapon";
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    AssetDatabase.CreateFolder("Assets/Game", "Weapon");
                }
                AssetDatabase.CreateFolder(parentFolder, "Data");
            }

            // JSONファイルを作成（HitBox Editorで編集可能）
            var jsonPath = $"{weaponDataFolder}/ElectricitySword2.json";
            var jsonData = new WeaponJsonDataForIntegration();
            jsonData.weaponName = "Electricity Sword";
            jsonData.damage = 5;
            jsonData.attackDuration = 0.3f;
            jsonData.cooldownTime = 1.0f;
            jsonData.colliderSize = 1.0f;
            jsonData.isTrigger = true;
            jsonData.colliders = new System.Collections.Generic.List<ColliderCellData>
            {
                // デフォルトのヒットボックス（プレイヤー前方3マス）
                new ColliderCellData { name = "Collider_00", x = 0f, y = 1f },
                new ColliderCellData { name = "Collider_01", x = 0f, y = 2f },
                new ColliderCellData { name = "Collider_02", x = 0f, y = 3f }
            };

            var json = JsonUtility.ToJson(jsonData, true);
            System.IO.File.WriteAllText(jsonPath, json);
            AssetDatabase.Refresh();

            Debug.Log($"[PlayerModelIntegration] 武器JSONを作成しました: {jsonPath}");
            Debug.Log("[PlayerModelIntegration] HitBox Editorで攻撃範囲を調整できます。");

            // WeaponPickupプレハブを作成
            CreateWeaponPickupPrefab();

            EditorUtility.DisplayDialog("成功",
                $"武器データを作成しました。\n" +
                $"JSONパス: {jsonPath}\n\n" +
                $"次のステップ:\n" +
                $"1. JSONを右クリック → 'Create WeaponData from JSON'\n" +
                $"2. DOD > Tools > HitBox Editor で攻撃範囲を調整",
                "OK");
        }

        private void CreateWeaponPickupPrefab()
        {
            // WeaponPickupプレハブフォルダを作成
            var pickupFolder = "Assets/Game/InteractObj/Prefabs";
            if (!AssetDatabase.IsValidFolder(pickupFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Game/InteractObj"))
                {
                    AssetDatabase.CreateFolder("Assets/Game", "InteractObj");
                }
                AssetDatabase.CreateFolder("Assets/Game/InteractObj", "Prefabs");
            }

            // プレハブを作成
            var pickupObj = new GameObject("ElectricitySword2_Pickup");

            // 武器モデルを子として追加
            var weaponModel = (GameObject)PrefabUtility.InstantiatePrefab(electricitySwordPrefab, pickupObj.transform);
            weaponModel.transform.localPosition = Vector3.zero;
            weaponModel.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // 剣を立てる
            weaponModel.transform.localScale = Vector3.one;

            ApplyAutomaticScale(weaponModel, TargetCellWidth, TargetCellDepth, TargetCellHeight);
            AlignModelBottomToParent(weaponModel);
            AutoApplyMaterials(weaponModel, WeaponMaterialFolders, "ElectricitySword2");

            // Colliderを追加
            var collider = pickupObj.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1f, 2f, 1f);
            collider.center = Vector3.up;

            // WeaponPickupコンポーネントを追加
            var weaponPickupType = ResolveType("Game.InteractObj.WeaponPickup");
            if (weaponPickupType != null)
            {
                pickupObj.AddComponent(weaponPickupType);
            }
            else
            {
                Debug.LogWarning("[PlayerModelIntegration] Game.InteractObj.WeaponPickup が見つからないため、コンポーネント追加をスキップしました。");
            }

            // 回転アニメーションを追加（オプション）
            pickupObj.AddComponent<RotateObject>();

            // プレハブとして保存
            var prefabPath = $"{pickupFolder}/ElectricitySword2_Pickup.prefab";
            PrefabUtility.SaveAsPrefabAsset(pickupObj, prefabPath);
            DestroyImmediate(pickupObj);

            Debug.Log($"[PlayerModelIntegration] WeaponPickupプレハブを作成しました: {prefabPath}");
        }

        private void IntegrateAll()
        {
            if (!EditorUtility.DisplayDialog("確認",
                "プレイヤーモデルと武器を一括統合します。\n" +
                "この操作は元に戻せません。\n\n" +
                "続行しますか？",
                "はい", "キャンセル"))
            {
                return;
            }

            try
            {
                // 1. プレイヤーモデル統合
                IntegratePlayerModel();

                // 2. 武器データ作成
                CreateWeaponData();

                EditorUtility.DisplayDialog("完了",
                    "統合作業が完了しました！\n\n" +
                    "次のステップ:\n" +
                    "1. ElectricitySword2.jsonを右クリック\n" +
                    "2. 'Create WeaponData from JSON' を選択\n" +
                    "3. 作成されたWeaponDataアセットをPlayerEquipmentに設定\n" +
                    "4. HitBox Editorで攻撃範囲を調整",
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("エラー", $"統合中にエラーが発生しました:\n{e.Message}", "OK");
                Debug.LogError($"[PlayerModelIntegration] エラー: {e}");
            }
        }

        private static System.Type ResolveType(string fullTypeName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var foundType = assembly.GetType(fullTypeName);
                if (foundType != null)
                {
                    return foundType;
                }
            }

            return null;
        }

        private static void ApplyAutomaticScale(GameObject modelRoot, float targetWidth, float targetDepth, float targetHeight)
        {
            if (modelRoot == null)
            {
                return;
            }

            modelRoot.transform.localScale = Vector3.one;
            if (!TryGetRendererBounds(modelRoot, out var bounds))
            {
                return;
            }

            const float minSize = 0.0001f;
            var scaleByWidth = targetWidth / Mathf.Max(bounds.size.x, minSize);
            var scaleByDepth = targetDepth / Mathf.Max(bounds.size.z, minSize);
            var scaleByHeight = targetHeight / Mathf.Max(bounds.size.y, minSize);

            // 形状を崩さないように等倍スケールで最小係数を採用する。
            var uniformScale = Mathf.Min(scaleByWidth, scaleByDepth, scaleByHeight);
            uniformScale = Mathf.Clamp(uniformScale, 0.01f, 100f);
            modelRoot.transform.localScale = Vector3.one * uniformScale;
        }

        private static void AlignModelBottomToParent(GameObject modelRoot)
        {
            if (modelRoot == null || !TryGetRendererBounds(modelRoot, out var bounds))
            {
                return;
            }

            var parentY = modelRoot.transform.parent != null ? modelRoot.transform.parent.position.y : 0f;
            var deltaY = parentY - bounds.min.y;
            modelRoot.transform.position += Vector3.up * deltaY;
        }

        private static bool TryGetRendererBounds(GameObject modelRoot, out Bounds bounds)
        {
            var renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static void AutoApplyMaterials(GameObject modelRoot, string[] preferredFolders, string modelKeyword)
        {
            if (modelRoot == null)
            {
                return;
            }

            var candidateMaterials = LoadMaterials(preferredFolders);
            if (candidateMaterials.Length == 0)
            {
                candidateMaterials = LoadMaterials(CommonMaterialFolders);
            }

            if (candidateMaterials.Length == 0)
            {
                Debug.LogWarning("[PlayerModelIntegration] 候補マテリアルが見つからないため自動適用をスキップしました。");
                return;
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
                    if (currentMaterial != null && IsMaterialInFolders(currentMaterial, preferredFolders))
                    {
                        continue;
                    }

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

            Debug.Log($"[PlayerModelIntegration] マテリアル自動適用: {appliedCount} スロット");
        }

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

            if (!string.IsNullOrEmpty(lowerHint))
            {
                if (lowerCandidate == lowerHint)
                {
                    score += 1000;
                }

                if (lowerCandidate.Contains(lowerHint) || lowerHint.Contains(lowerCandidate))
                {
                    score += 120;
                }

                var tokens = lowerHint.Split(new[] { '_', '-', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokens)
                {
                    if (token.Length >= 3 && lowerCandidate.Contains(token))
                    {
                        score += 30;
                    }
                }
            }

            if (!string.IsNullOrEmpty(lowerKeyword) && lowerCandidate.Contains(lowerKeyword))
            {
                score += 80;
            }

            if (lowerCandidate.Contains("urp") || lowerCandidate.Contains("lit"))
            {
                score += 10;
            }

            return score;
        }

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

        [System.Serializable]
        private class WeaponJsonDataForIntegration
        {
            public string weaponName;
            public int damage;
            public float attackDuration;
            public float cooldownTime;
            public float colliderSize;
            public bool isTrigger;
            public System.Collections.Generic.List<ColliderCellData> colliders;
        }

        [System.Serializable]
        private class ColliderCellData
        {
            public string name;
            public float x;
            public float y;
        }
    }

    /// <summary>
    /// オブジェクトを回転させるシンプルなコンポーネント（武器ピックアップ用）
    /// </summary>
    public class RotateObject : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        private void Update()
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }
}



