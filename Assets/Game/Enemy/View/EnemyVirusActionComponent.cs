using System;
using UnityEngine;

namespace Game.Enemy
{
    public sealed class EnemyVirusActionComponent : MonoBehaviour
    {
        // attackCoolTimeSeconds は WeaponMasterData から SetAttackCoolTime() 経由で設定される。
        // Inspector での手動設定は不要。
        private float attackCoolTimeSeconds = 1f;

        private EnemyVirusHealthComponent enemyVirusHealthComponent;
        private EnemyAnimationHandler animationHandler;
        private float stateTransitionTimer;
        private float attackTimer;
        private int actionPatternIndex;

        public EnemyVirusActionPatternEntity ActionPatternEntity { get; private set; }
        public EnemyVirusStateTransitionType CurrentState { get; private set; }
        public bool IsInitialized { get; private set; }

        public event Action<EnemyVirusStateTransitionType, EnemyVirusStateTransitionType> OnStateChanged;

        private void Awake()
        {
            // Awake 時点では healthComponent が未追加の場合がある（プレハブに事前アタッチされていない場合など）。
            // Start() でも再取得するため、ここで null でも問題ない。
            enemyVirusHealthComponent = GetComponent<EnemyVirusHealthComponent>();
            animationHandler = GetComponentInChildren<EnemyAnimationHandler>();
        }

        private void Start()
        {
            // Awake 時に取得できなかった場合（プレハブ事前アタッチ + InitializeLogic で動的追加のケース）
            // Start() はすべての AddComponent が完了した後に呼ばれるため、ここで確実に取得できる。
            if (enemyVirusHealthComponent == null)
            {
                enemyVirusHealthComponent = GetComponent<EnemyVirusHealthComponent>();
            }

            if (enemyVirusHealthComponent != null)
            {
                enemyVirusHealthComponent.OnDeath += HandleDeath;
            }
            else
            {
                UnityEngine.Debug.LogWarning("[EnemyVirusActionComponent] EnemyVirusHealthComponent が見つかりません。", this);
            }
        }

        private void OnDestroy()
        {
            if (enemyVirusHealthComponent == null)
            {
                return;
            }

            enemyVirusHealthComponent.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (!IsInitialized || CurrentState == EnemyVirusStateTransitionType.Dead)
            {
                return;
            }

            stateTransitionTimer += Time.deltaTime;
            attackTimer += Time.deltaTime;
        }

        public void Initialize(EnemyVirusActionPatternEntity actionPatternEntity)
        {
            ActionPatternEntity = actionPatternEntity;
            CurrentState = actionPatternEntity.InitialState;
            stateTransitionTimer = 0f;
            attackTimer = 0f;
            actionPatternIndex = 0;
            IsInitialized = true;

            UnityEngine.Debug.Log($"[EnemyVirusActionComponent] 初期化完了 - 初期状態: {CurrentState}, パターン数: {actionPatternEntity.ActionPatterns?.Count ?? 0}", this);
        }

        public bool TryTransitNextByPattern()
        {
            if (!IsInitialized || CurrentState == EnemyVirusStateTransitionType.Dead)
            {
                return false;
            }

            if (stateTransitionTimer < Mathf.Max(0f, ActionPatternEntity.StateTransitionCoolTime))
            {
                return false;
            }

            var actionPatterns = ActionPatternEntity.ActionPatterns;
            if (actionPatterns == null || actionPatterns.Count == 0)
            {
                return false;
            }

            if (actionPatternIndex >= actionPatterns.Count)
            {
                actionPatternIndex = 0;
            }

            var nextState = actionPatterns[actionPatternIndex];
            actionPatternIndex++;

            ChangeState(nextState);
            stateTransitionTimer = 0f;
            return true;
        }

        public bool CanAttackNow()
        {
            if (CurrentState != EnemyVirusStateTransitionType.Attacking)
            {
                return false;
            }

            return attackTimer >= Mathf.Max(0f, attackCoolTimeSeconds);
        }

        public void NotifyAttackExecuted()
        {
            attackTimer = 0f;

            // 攻撃アニメーションを再生
            if (animationHandler != null)
            {
                animationHandler.PlayAttackAnimation();
            }
        }

        /// <summary>
        /// WeaponMasterData の攻撃クールタイムを適用するために、スポーン時に呼び出す。
        /// EnemyVirusSpawner.InitializeLogic から WeaponEntity.AttackCoolTime を渡す。
        /// </summary>
        public void SetAttackCoolTime(float coolTimeSeconds)
        {
            attackCoolTimeSeconds = Mathf.Max(0f, coolTimeSeconds);
        }

        /// <summary>
        /// AI の判断によって状態を直接遷移させる。
        /// パターン順送りではなく、プレイヤー検知・離脱などのイベントドリブンな遷移に使う。
        /// </summary>
        public void ForceTransitState(EnemyVirusStateTransitionType newState)
        {
            if (!IsInitialized || CurrentState == EnemyVirusStateTransitionType.Dead)
            {
                return;
            }

            UnityEngine.Debug.Log($"[EnemyVirusActionComponent] ForceTransitState - {CurrentState} → {newState}", this);
            ChangeState(newState);
            stateTransitionTimer = 0f;
        }

        private void HandleDeath()
        {
            if (!IsInitialized)
            {
                return;
            }

            ChangeState(EnemyVirusStateTransitionType.Dead);
        }

        private void ChangeState(EnemyVirusStateTransitionType nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            var previousState = CurrentState;
            CurrentState = nextState;
            UnityEngine.Debug.Log($"[EnemyVirusActionComponent] ChangeState - {previousState} → {nextState}", this);
            OnStateChanged?.Invoke(previousState, nextState);
        }
    }
}

