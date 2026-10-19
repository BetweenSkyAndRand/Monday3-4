using UnityEngine;

namespace Game.Enemy
{
    public readonly struct EnemyVirusSpawnCommand
    {
        public EnemyVirusSpawnCommand(string uid, Vector3 spawnPosition, uint hpBonus = 0, uint attackPowerBonus = 0, float dropRateMultiplier = 1f)
        {
            DropRateMultiplier = dropRateMultiplier;
            Uid = uid;
            SpawnPosition = spawnPosition;
            HpBonus = hpBonus;
            AttackPowerBonus = attackPowerBonus;
        }

        public string Uid { get; }
        public Vector3 SpawnPosition { get; }
        public uint HpBonus { get; }
        public uint AttackPowerBonus { get; }
        public float DropRateMultiplier { get; }
    }
}

