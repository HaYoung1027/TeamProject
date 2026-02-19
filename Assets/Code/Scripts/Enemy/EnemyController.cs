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

    Rigidbody2D rigid;
    IDamageable damageable;
    public bool isGrounded;

    void Awake()
	{
        rigid = GetComponent<Rigidbody2D>();
		damageable = GetComponent<Enemy>();
        isGrounded = true;
    }

    void Update()
	{
        if (isGrounded && rigid.linearVelocity == Vector2.zero)
            gameObject.tag = tagName.enemy;
    }
    public void CheckGround(Collision2D collision)
    {
        foreach (var contact in collision.contacts)     // 바닥 체크
        {
            if (contact.normal.y > 0.7f &&
                contact.point.y < transform.position.y)
            {
                isGrounded = true;
                break;
            }
        }

        if (isGrounded && rigid.linearVelocityY < 0f)       // y값 보정 (바닥 뚫림 방지)
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0f);
    }

    void OnCollisionEnter2D(Collision2D collision)
	{
        CheckGround(collision);     // 바닥 체크

        if (gameObject.CompareTag(tagName.enemy))
		{
			if (collision.gameObject.CompareTag(tagName.throwingEnemy))     // 적과 닿았을 경우
            {
				if (collision.gameObject.TryGetComponent<Enemy>(out var target))
				{
                    Vector2 hitDir = (target.transform.position - transform.position).normalized;
                    target.SetHitDirection(hitDir);

                    target.TakeDamage(1);       // 닿은 적에게 데미지 주기
					damageable.TakeDamage(1);   // 자기 자신도 데미지 받기
				}
			}
			// 오브젝트와 닿았을 경우
			else if (collision.gameObject.CompareTag(tagName.throwingObj))
			{
				if (collision.gameObject.TryGetComponent<Enemy>(out var target))
                {
                    Vector2 hitDir = (target.transform.position - transform.position).normalized;
                    target.SetHitDirection(hitDir);

                    target.TakeDamage(1);       // 닿은 적에게 데미지 주기
                }
            }
		}
	}

    void OnCollisionStay2D(Collision2D collision)
    {
        CheckGround(collision);     // 바닥 체크
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(tagName.ground))
            isGrounded = false;
    }
}