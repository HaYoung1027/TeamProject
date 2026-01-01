using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// 몬스터 상태 열거형
enum EnemyState
{
	None,       // 일반 상태
	Thrown,     // 던져짐
}

public class EnemyController : MonoBehaviour, IDamageable
{
	EnemyStatsRuntime currStat;     // 현재 스탯
	Rigidbody2D rigid;
	PlayerController player;        // 플레이어 정보
	IDamageable damageable;
	string throwingEnemyStr = "ThrowingEnemy";
	string enemyStr = "Enemy";
	//EnemyState state;       // 몬스터 상태

	private void Awake()
	{
		damageable = GetComponent<IDamageable>();
		rigid = GetComponent<Rigidbody2D>();
		player = GetComponent<PlayerController>();
	}

	private void Start()
	{
		currStat = new EnemyStatsRuntime(GameManager.Instance.enemyStats);  // 스탯 초기화
		transform.gameObject.tag = enemyStr;
	}

	private void Update()
	{
		if (currStat.currentHP <= 0)
		{
			transform.gameObject.SetActive(false);
		}
	}


	// 던져지는 몬스터 또는 오브젝트와 닿았을 경우
	void OnCollisionEnter2D(Collision2D other)
	{
		// 몬스터의 태그가 Enemy일 때
		// 플레이어에게 잡혔을 경우
		//if (other.gameObject.CompareTag("Player"))
		//{
		//	state = EnemyState.Grabbed;
		//	Debug.Log(transform.gameObject.ToString() + " 잡힘");
		//}
		//// 땅과 충돌할 경우
		//else if (other.gameObject.CompareTag("Ground"))
		//{
		//	state = EnemyState.None;
		//	Debug.Log(transform.gameObject.ToString() + " None");
		//}
		//else
		//{
		//	state = EnemyState.Thrown;
		//	Debug.Log(transform.gameObject.ToString() + " 던져짐");
		//}

		if (other.gameObject.CompareTag("Player"))
		{
			transform.gameObject.tag = enemyStr;
		}
		else if(other.gameObject.CompareTag("Ceiling"))
		{
			//state = EnemyState.None;
			transform.gameObject.tag = enemyStr;
		}
		else
		{
			//state = EnemyState.Thrown;
			transform.gameObject.tag = throwingEnemyStr;
		}

		// 몬스터 태그가 Enemy일 때 던져지는 몬스터나 오브젝트에게 닿았을 경우
		if (transform.gameObject.CompareTag(enemyStr) && (other.gameObject.CompareTag(throwingEnemyStr) || other.gameObject.CompareTag("Object")))
		{
			// 몬스터 데미지 입히기
			damageable.TakeDamage(1);
		}

		// 몬스터의 태그가 ThrowingEnemy일 때 몬스터에게 닿았을 경우
		if (transform.gameObject.CompareTag(throwingEnemyStr) && other.gameObject.CompareTag(enemyStr))
		{
			// 던져지는 몬스터 데미지 입히기
			damageable.TakeDamage(1);
		}

	}

	// 적 데미지 입히기
	void IDamageable.TakeDamage(int attack)
	{
		currStat.currentHP -= attack;
	}
}
