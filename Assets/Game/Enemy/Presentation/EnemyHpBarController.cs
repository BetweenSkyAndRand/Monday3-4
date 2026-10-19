using Game.Enemy.View;
using UnityEngine;

namespace Game.Enemy.Presentation
{
    /// <summary>
    /// 敵HPバーの状態管理と View へのイベント中継（Presentation層）。
    /// EnemyVirusHealthComponent の状態変化を EnemyHpBarView に橋渡しする。
    /// </summary>
    public class EnemyHpBarController : MonoBehaviour
    {
        [SerializeField] private EnemyVirusHealthComponent healthComponent;
        [SerializeField] private EnemyHpBarView enemyHpBarView;
        [SerializeField] private bool hideOnDeath = true;
        [SerializeField] private bool hideAtStartUntilInitialized = false;

        private void Awake()
        {
            if (healthComponent == null)
            {
                healthComponent = GetComponentInParent<EnemyVirusHealthComponent>();
            }

            if (enemyHpBarView == null)
            {
                enemyHpBarView = GetComponentInChildren<EnemyHpBarView>(true);
            }

            if (hideAtStartUntilInitialized && enemyHpBarView != null)
            {
                enemyHpBarView.SetVisible(false);
            }
        }

        private void OnEnable()
        {
            if (healthComponent == null || enemyHpBarView == null)
            {
                return;
            }

            healthComponent.OnHpChanged += OnHpChanged;
            healthComponent.OnDeath += OnDeath;

            enemyHpBarView.SetVisible(true);
            enemyHpBarView.SetHp(healthComponent.CurrentHp, healthComponent.MaxHp);
        }
        private void Start()
        {
            if (healthComponent == null || enemyHpBarView == null)
            {
                return;
            }

            enemyHpBarView.SetVisible(true);
            enemyHpBarView.SetHp(healthComponent.CurrentHp, healthComponent.MaxHp);
        }
        private void OnDisable()
        {
            if (healthComponent == null)
            {
                return;
            }

            healthComponent.OnHpChanged -= OnHpChanged;
            healthComponent.OnDeath -= OnDeath;
        }

        private void OnHpChanged(float currentHp, float maxHp)
        {
            if (enemyHpBarView == null)
            {
                return;
            }

            enemyHpBarView.SetVisible(true);
            enemyHpBarView.SetHp(currentHp, maxHp);
        }

        private void OnDeath()
        {
            if (enemyHpBarView == null)
            {
                return;
            }

            if (hideOnDeath)
            {
                enemyHpBarView.SetVisible(false);
            }
            else
            {
                enemyHpBarView.SetHp(0f, Mathf.Max(0.01f, healthComponent.MaxHp));
            }
        }
    }
}

