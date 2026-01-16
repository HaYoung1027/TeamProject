using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.LowLevelPhysics2D.PhysicsShape;

public class PlayerController : MonoBehaviour, IDamageable
{
	public bool isGrounded;
	public bool hasCollided = false;
	bool wasAttach;

	public Vector2 inputVec;
	Rigidbody2D rigid;
	SpriteRenderer sprite;
	GrapplingHook grappling;

	PlayerInteraction interaction;  // 상호작용

	void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		sprite = GetComponent<SpriteRenderer>();
		grappling = GetComponent<GrapplingHook>();
		interaction = GetComponent<PlayerInteraction>();
	}
	void Start()
	{
		isGrounded = true;
	}
	void FixedUpdate()
	{
        if (interaction && interaction.GetIsAction()) return;

		float speed = GameManager.Instance.playerStatsRuntime.speed;

        // 그래플 시작 순간
        if (!wasAttach && grappling.isAttach)
        {
            // 입력 방향으로 쌓인 속도만 제거
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y); // 수평 가속도 제거, 수직 가속도 유지
		}

		if (grappling.isAttach)
		{
			float hookSwingForce = GameManager.Instance.playerStatsRuntime.hookSwingForce;
			rigid.AddForce(new Vector2(inputVec.x * hookSwingForce, 0f));

			if (rigid.linearVelocity.magnitude > GameManager.Instance.playerStatsRuntime.maxSwingSpeed)
			{
				rigid.linearVelocity = rigid.linearVelocity.normalized * GameManager.Instance.playerStatsRuntime.maxSwingSpeed;
			}
		}
		else
		{
			float x = inputVec.x * speed * Time.deltaTime;
			transform.Translate(x, 0, 0);
		}

		// 방향 플립
		if (inputVec.x > 0)
		{
			sprite.flipX = false;
		}
		else if (inputVec.x < 0)
		{
			sprite.flipX = true;
		}

		// 상태 저장 (맨 마지막!)
		wasAttach = grappling.isAttach;
	}


	void OnJump()
	{
		// 플레이어가 바닥이 아닐 경우
		if (!isGrounded) return;

		GameManager.Instance.audioManager.PlayJumpSound(1f);
		rigid.AddForce(Vector2.up * GameManager.Instance.playerStatsRuntime.jumpForce, ForceMode2D.Impulse);

		isGrounded = false;
	}

	void OnCollisionEnter2D(Collision2D collision)
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
		{
			rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0f);
		}

	}


	void OnMove(InputValue value)
	{
		inputVec = value.Get<Vector2>();
	}

	// 플레이어 데미지
	public void TakeDamage(int attack)
	{
		// 플레이어 체력 줄어들기
		GameManager.Instance.playerStatsRuntime.currentHP -= attack;
	}

}
