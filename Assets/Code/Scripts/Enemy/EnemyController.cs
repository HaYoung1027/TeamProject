using UnityEngine;
using UnityEngine.InputSystem.XR.Haptics;
using UnityEngine.Windows.Speech;
using tagName = Globals.TagName;    // 태그

/// <summary>
/// 몬스터 컨트롤러
/// 몬스터의 행동 담당
/// </summary>
public class EnemyController : MonoBehaviour
{
	Enemy enemy;
	IDamageable damageable;

	void Awake()
	{
		enemy = GetComponent<Enemy>();
		damageable = GetComponent<Enemy>();
	}

	private void Update()
	{
		
	}

	void OnCollisionEnter2D(Collision2D other)
	{
		// 적과 닿았을 경우
		if (other.gameObject.CompareTag(tagName.enemy) || other.gameObject.CompareTag(tagName.throwingEnemy))
		{
			if (other.gameObject.TryGetComponent<Enemy>(out var target))
			{
				target.TakeDamage(1);       // 닿은 적에게 데미지 주기
				damageable.TakeDamage(1);   // 자기 자신도 데미지 받기
			}
		}
		// 오브젝트와 닿았을 경우
		else if(other.gameObject.CompareTag(tagName.obj))
		{

		}
	}
}