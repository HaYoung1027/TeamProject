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
    Rigidbody2D rigid;
    IDamageable damageable;
    public bool isGrounded;
    public bool hasCollided = false;

    void Awake()
	{
        rigid = GetComponent<Rigidbody2D>();
        enemy = GetComponent<Enemy>();
		damageable = GetComponent<Enemy>();
	}

    void Start()
    {
        isGrounded = true;
    }

    void Update()
	{
        if (isGrounded && rigid.linearVelocity == Vector2.zero)
            gameObject.tag = "Enemy";
    }
    public void CheckGround(Collision2D collision)
    {
        // 바닥 체크
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.7f &&
                contact.point.y < transform.position.y)
            {
                isGrounded = true;
                break;
            }
        }

        // 충돌 체크
        hasCollided = true;

        // y값 보정 (바닥 뚫림 방지)
        if (isGrounded && rigid.linearVelocityY < 0f)
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0f);
    }


    void OnCollisionEnter2D(Collision2D other)
	{
        CheckGround(other);     // 바닥 체크

        if (gameObject.CompareTag(tagName.enemy))
		{
			// 적과 닿았을 경우
			if (other.gameObject.CompareTag(tagName.throwingEnemy))
			{
				if (other.gameObject.TryGetComponent<Enemy>(out var target))
				{
					target.TakeDamage(1);       // 닿은 적에게 데미지 주기
					damageable.TakeDamage(1);   // 자기 자신도 데미지 받기
				}
			}
			// 오브젝트와 닿았을 경우
			else if (other.gameObject.CompareTag(tagName.throwingObj))
			{
				if (other.gameObject.TryGetComponent<Enemy>(out var target))
				{
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