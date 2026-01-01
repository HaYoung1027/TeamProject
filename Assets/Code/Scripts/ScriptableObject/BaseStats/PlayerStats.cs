using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Custom/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("플레이어 기본 스텟")]
    [Header("이동속도")]
    public float speed;
    [Header("점프 높이")]
    public float jumpForce;
    [Header("공격력")]
    public float attack;
    [Header("체력")]
    public float maxHP;

    [Header("플레이어 갈고리 스텟")]
    [Header("갈고리 속도")]
    public float hookSpeed;
    [Header("갈고리 최대 거리")]
    public float hookDistance;
    [Header("갈고리 중 좌우 속도")]
    public float hookSwingForce;
    [Header("갈고리 던지는 힘")]
    public float hookThrowForce;

    [Header("갈고리 중 최대 속도")]
    public float maxSwingSpeed;

}

