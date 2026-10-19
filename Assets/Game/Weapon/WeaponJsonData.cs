using System;
using System.Collections.Generic;
using UnityEngine;
using DOD.HitBox;

namespace Game.Weapon
{
    /// <summary>
    /// 武器設定の JSON データモデル。
    /// 武器パラメーター（名前・ダメージ・攻撃時間・クールタイム）と
    /// HitBox コライダー情報（colliderSize・colliders）を 1 ファイルで管理する。
    /// </summary>
    /// <example>
    /// {
    ///   "weaponName": "Sword",
    ///   "damage": 3,
    ///   "attackDuration": 0.2,
    ///   "cooldownTime": 0.8,
    ///   "colliderSize": 1.0,
    ///   "colliders": [
    ///     { "name": "Collider_00", "x": 0.0, "y": 1.0 },
    ///     { "name": "Collider_01", "x": -1.0, "y": 1.0 }
    ///   ]
    /// }
    /// </example>
    [Serializable]
    public class WeaponJsonData
    {
        public string weaponName = "UnnamedWeapon";
        public int damage = 1;

        /// <summary>HitBox を有効化する時間（秒）。</summary>
        public float attackDuration = 0.2f;

        /// <summary>
        /// 次の攻撃が可能になるまでの待機時間（秒）。
        /// attackDuration より長い値を推奨。
        /// </summary>
        public float cooldownTime = 0.8f;

        /// <summary>HitBox コライダー 1 つ分のサイズ（ワールド単位）。</summary>
        public float colliderSize = 1f;

        /// <summary>
        /// true にすると BoxCollider が Trigger として生成される。
        /// OnTriggerEnter/Exit で当たり判定を受け取る場合は true にする。
        /// </summary>
        public bool isTrigger = true;

        /// <summary>
        /// 武器のビジュアルモデルのパス（Resources フォルダからの相対パス）。
        /// 例: "Weapons/SwordModel", "Weapons/Enemy/ClawModel"
        /// 空の場合はモデルなし（コライダーのみ）。
        /// </summary>
        public string weaponModelPath = "";

        /// <summary>
        /// 武器モデルのローカルスケール倍率。
        /// 1.0 でプレハブのデフォルトサイズ。0.3 などで小さく表示できる。
        /// 親（VisualModel 等）に lossyScale が掛かっていても基本サイズを保証するため、
        /// WeaponHolder 側で親スケールを打ち消したうえでこの値を掛ける。
        /// </summary>
        public float weaponModelScale = 1f;

        /// <summary>
        /// 武器モデルの localPosition オフセット（プレイヤー基準）。
        /// 武器の柄が手元に来るよう、刃のピボットからの逆方向オフセットを設定する。
        /// </summary>
        public Vector3 weaponModelOffset = Vector3.zero;

        /// <summary>
        /// 武器モデルの localEulerAngles（度）。
        /// プレハブの向きが正面でない場合や、刃の向きを補正するために使う。
        /// </summary>
        public Vector3 weaponModelRotation = Vector3.zero;

        /// <summary>
        /// インベントリ UI 用アイコン Sprite の Resources パス。
        /// 例: "Weapons/Icons/BeginnerSword"
        /// 空の場合は WeaponData.inventoryIcon（Inspector アサイン）にフォールバックする。
        /// </summary>
        public string inventoryIconPath = "";

        /// <summary>攻撃時の VFX プレハブ Resources パス。空ならフォールバック使用。</summary>
        public string attackVfxPath = "";

        /// <summary>敵ヒット時の VFX プレハブ Resources パス。空ならフォールバック使用。</summary>
        public string hitVfxPath = "";

        /// <summary>攻撃時の SE クリップ Resources パス。空ならフォールバック使用。</summary>
        public string attackSePath = "";

        /// <summary>ヒット時の SE クリップ Resources パス。空ならフォールバック使用。</summary>
        public string hitSePath = "";

        /// <summary>
        /// 攻撃時 SE の ID（GamePresentationSettings.sePresentations）。
        /// 設定されていれば AudioManager.PlayConfiguredSe(seId) 経由で再生する。
        /// 空の場合は attackSePath / Inspector アサインの AudioClip にフォールバックする。
        /// </summary>
        public string attackSeId = "";

        /// <summary>
        /// ヒット時 SE の ID（GamePresentationSettings.sePresentations）。
        /// 設定されていれば AudioManager.PlayConfiguredSe3D(seId, pos) 経由で再生する。
        /// 空の場合は hitSePath / Inspector アサインの AudioClip にフォールバックする。
        /// </summary>
        public string hitSeId = "";

        /// <summary>VFX の表示位置オフセット。プレイヤー基準の相対座標。</summary>
        public Vector3 vfxOffset = Vector3.zero;

        /// <summary>
        /// 武器を振るときに Animator.SetTrigger() に渡すトリガー名。
        /// 空の場合はアニメーションを再生しない。
        /// 例: "Attack", "SwordSwing"
        /// </summary>
        public string attackAnimationTrigger = "";

        /// <summary>HitBox Editor で定義したコライダー座標リスト。</summary>
        public List<ColliderData> colliders = new List<ColliderData>();
    }
}

