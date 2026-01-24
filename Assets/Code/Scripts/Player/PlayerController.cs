using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using static UnityEngine.LowLevelPhysics2D.PhysicsShape;

using playerState = EnumType.PlayerState;
using tagName = Globals.TagName;

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
	Animator animator;              // 애니메이션
    private Coroutine damageCanvasCoroutine;

    public float maxTime;           // 땅에서 움직이지 않을 때 일정 시간 이후 Run에서 Idle
	private float curTime;
	public Canvas damagedCanvas;

	void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		sprite = GetComponent<SpriteRenderer>();
		animator = GetComponent<Animator>();
        interaction = GameManager.Instance.playerInteraction;
        grappling = GameManager.Instance.grapplingHook;
	}
	void Start()
	{
		isGrounded = true;
        damagedCanvas.enabled = false;
        SetPlayerState(playerState.Idle);
	}

    void Update()
    {
        if (TimelineController.isTimelinePlaying)
        {
            inputVec = Vector2.zero;
            return;   // 컷씬 재생 중일 때는 플레이어 컨트롤 불가
        }
        UpdateAnimation(); // 애니메이션
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
		if (TimelineController.isTimelinePlaying)
		{
			inputVec = Vector2.zero;
			return;   // 컷씬 재생 중일 때는 플레이어 컨트롤 불가
		}
											   // 플레이어가 바닥이 아닐 경우
			if (!isGrounded) return;

		GameManager.Instance.audioManager.PlayJumpSound(1f);
		rigid.AddForce(Vector2.up * GameManager.Instance.playerStatsRuntime.jumpForce, ForceMode2D.Impulse);

		isGrounded = false;
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
		CheckGround(collision);     // 바닥 체크
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


	void OnMove(InputValue value)
	{
        if (TimelineController.isTimelinePlaying)
        {
            inputVec = Vector2.zero;
            return;   // 컷씬 재생 중일 때는 플레이어 컨트롤 불가
        }
        inputVec = value.Get<Vector2>();
	}

    // 플레이어 데미지
    public void TakeDamage(int attack)
    {
        // 체력 감소
        GameManager.Instance.playerStatsRuntime.currentHP -= attack;

        // 이미 실행 중이면 중단 (연속 피격 대응)
        if (damageCanvasCoroutine != null)
            StopCoroutine(damageCanvasCoroutine);

        damageCanvasCoroutine = StartCoroutine(ShowDamagedCanvas());
    }

    IEnumerator ShowDamagedCanvas()
    {
        damagedCanvas.enabled = true;
        yield return new WaitForSeconds(1f);
        damagedCanvas.enabled = false;
    }

    // 바닥 체크
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

	// 플레이어 상태 변경
	void SetPlayerState(playerState state)
	{
		animator.SetInteger(Globals.AnimationVarName.playerState, (int)state);
	}

	// 애니메이션 업데이트
	void UpdateAnimation()
	{
        if (isGrounded)
		{
            // 플레이어가 가만히 있을 때
            bool hasMoveInput = Mathf.Abs(inputVec.x) > 0.01f;

            if (!hasMoveInput)
            {
                curTime += Time.deltaTime;

                if (curTime >= maxTime)
                    SetPlayerState(playerState.Idle);
            }
            else
            {
                SetPlayerState(playerState.Run);
                curTime = 0f;
            }
        }
		else
		{
			SetPlayerState(playerState.Idle);
		}
	}
}
