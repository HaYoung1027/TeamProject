using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Custom/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("플레이어 스텟")]
    public int level;
    public float maxHP;
    public float speed;
    public float attack;
    public float attackSpeed;
    public int criticalChance;
    public int criticalValue;
}