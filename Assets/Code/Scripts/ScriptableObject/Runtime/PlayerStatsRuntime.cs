using UnityEngine;

[System.Serializable]
public class PlayerStatsRuntime
{
    public int level;
    public float maxHP;
    public float currentHP;
    public float speed;
    public float attack;
    public float attackSpeed;
    public int criticalChance;
    public int criticalValue;

    public void CopyFrom(PlayerStats baseStats)
    {
        level = baseStats.level;
        maxHP = baseStats.maxHP;
        currentHP = maxHP;
        speed = baseStats.speed;
        attack = baseStats.attack;
        attackSpeed = baseStats.attackSpeed;
        criticalChance = baseStats.criticalChance;
        criticalValue = baseStats.criticalValue;
    }
}
