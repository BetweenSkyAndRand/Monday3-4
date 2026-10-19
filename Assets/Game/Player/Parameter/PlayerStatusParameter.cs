using UnityEngine;

namespace Game.Player.Parameter
{
    [CreateAssetMenu(
        fileName = "PlayerStatusParameter",
        menuName = "Game/Player/Player Status Parameter")]
    public class PlayerStatusParameter : ScriptableObject
    {
        [Header("HP初期値")]
        [SerializeField] private float maxHp = 100f;

        public float MaxHp => maxHp;
    }
}

