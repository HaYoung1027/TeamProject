using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
	EnemyStatsRuntime currStat;			// 현재 스탯
	public EnumType.EnemyState state;	// 현재 상태

	void Start()
	{
		state = EnumType.EnemyState.None;   // 상태 초기화
		currStat = new EnemyStatsRuntime(GameManager.Instance.enemyStats);  // 각 스탯 초기화
	}

	// 데미지 입히기
	public void TakeDamage(int attack)
	{
		// 공격력 만큼 체력 깎기
		currStat.currentHP -= attack;

		// TODO: 체력이 0 이하일 경우 할당 해제
		if (currStat.currentHP <= 0)
		{
			gameObject.SetActive(false);
		}
	}
}
