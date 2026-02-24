using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.LowLevelPhysics2D.PhysicsShape;
using playerState = EnumType.PlayerState;
using tagName = Globals.TagName;

public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("Audio Mixer")]
    public AudioMixer mixer;
    [Header("Global Volume 오브젝트")]
    public Volume globalVolume;
    [Header("Glitch Global Volume 오브젝트")]
	public GameObject glitchGlobalVolume;
	[Header("TV Global Volume 오브젝트")]
	public GameObject tvGlobalVolume;
	[Header("데미지 UI")]
	public GameObject damagedCanvas;
	[Header("검은 화면 UI")]
	public Image blackCanvas;
	[Header("땅에서 움직이지 않을 때 일정 시간 이후 Run에서 Idle")]
	public float maxTime;
	private float curTime;
	[Header("땅 체크")]
	public bool isGrounded;
	public Transform pos;
	public LayerMask isLayer;
	public float checkRadious = 0.1f;
	[Header("충돌 체크")]
	public bool hasCollided = false;
	[Header("걷기 사운드")]
	public float walkSoundInterval = 0.7f;
	private float walkSoundTimer = 0f;
	public bool isRunning = false;

	[Header("슬로우 게이지 UI")]
	public Slider slowGaugeSlider;
	[Header("슬로우 비율")]
	public float slowFactor = 0.3f;
	[Header("슬로우 게이지 최대치")]
	public float slowMaxGauge = 3f;
	[Header("슬로우 게이지 현재치")]
	public float slowGauge = 3f;
	[Header("슬로우 게이지 감소 속도")]
	public float slowDecreaseRate = 1f;
	[Header("슬로우 게이지 회복 속도")]
	public float slowRecoverRate = 0.5f;
    [Header("슬로우 상태")]
    public bool isSlow = false;
    [Header("Shift 슬로우 상태")]
    private bool isPlayerSlow = false;
    [Header("데미지 슬로우 상태")]
    private bool isDamageSlow = false;

	private TestGrapplingHook grappling;
	public Vector2 inputVec;
	private Rigidbody2D rigid;
	private SpriteRenderer sprite;
	private PlayerInteraction interaction;  // 상호작용
	private Animator animator;              // 애니메이션
	private bool isPlayedRunSound = false;  // 효과음 재생 여부
	private bool isJump = false;
    private bool wasGrounded;		// 이전 프레임 바닥 상태 저장
    private bool justLanded;
	private Silhouette solihoutte;  // 잔상효과
    private ColorAdjustments colorAdjustments;
	private Bloom bloom;
	private float slowTime = 0.5f;	// 슬로우 지속 시간

	/* 코루틴 */
    private Coroutine playerDieCoroutine;
	private Coroutine damageCanvasCoroutine;
	private Coroutine damagedColorCoroutine;
	private Coroutine slowCoroutine;			// 데미지 시간 체크 코루틴

	void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		sprite = GetComponent<SpriteRenderer>();
		animator = GetComponent<Animator>();
		interaction = GameManager.Instance.playerInteraction;
		grappling = GetComponent<TestGrapplingHook>();
		solihoutte = GetComponent<Silhouette>();
	}

	void Start()
	{
        SetPlayerState(playerState.Idle);
        if (globalVolume == null)
        {
            Debug.LogError("Global Volume이 할당되지 않았음");
            return;
        }

        if (!globalVolume.profile.TryGet(out colorAdjustments))
            Debug.LogError("Volume Profile에 없음");
        if (!globalVolume.profile.TryGet(out bloom))
            Debug.LogError("Volume Profile에 없음");
    }

    void FixedUpdate()
    {
        if (interaction && interaction.GetIsAction()) return;
        HandleMove();   // 플레이어 이동
        HandleFlip();   // 방향 플립

        // 착지 감지
        justLanded = !wasGrounded && isGrounded;
        wasGrounded = isGrounded;
    }

	void Update()
	{
		if (TimelineController.isTimelinePlaying)
		{
			inputVec = Vector2.zero;
			return;   // 컷씬 재생 중일 때는 플레이어 컨트롤 불가
		}

		UpdateAnimation();          // 애니메이션
		HandleWalkSound();          // 걷기 사운드

        HandleSlowMode();           // 슬로우 모드
        UpdateSlowGauge();	        // 슬로우 게이지 업데이트
    }

	void OnMove(InputValue value)
	{
		inputVec = value.Get<Vector2>();
	}

	void OnJump()
	{
		if (grappling.isAttach) return;  // 플레이어가 훅을 사용 중일 경우 리턴
		if (!isGrounded) return;    // 플레이어가 바닥이 아닐 경우

		GameManager.Instance.audioManager.PlayJumpSound(1f);
		rigid.AddForce(Vector2.up * GameManager.Instance.playerStatsRuntime.jumpForce, ForceMode2D.Impulse);
		isGrounded = false;
		isJump = true;
	}

	public void HandleMove()
	{
		float speed = GameManager.Instance.playerStatsRuntime.speed;

		if (float.IsNaN(inputVec.x) || float.IsNaN(speed))
			return;

		if (grappling.isAttach && !isGrounded)
		{
			// 스윙 가속도 주기
			Vector2 hookPoint = grappling.curHook.transform.position;
			Vector2 centerToPlayer = (Vector2)transform.position - hookPoint;

			// 접선 방향 2개 생성
			Vector2 tangent1 = new Vector2(-centerToPlayer.y, centerToPlayer.x).normalized;
			Vector2 tangent2 = -tangent1;

			float input = inputVec.x;

			// 입력 방향과 같은 쪽 접선 선택
			Vector2 chosenTangent = Vector2.Dot(tangent1, Vector2.right * input) > 0 ? tangent1 : tangent2;

			rigid.AddForce(chosenTangent * Mathf.Abs(input) * GameManager.Instance.playerStatsRuntime.hookSwingForce);
		}
		else
		{
            //float x = inputVec.x * speed * Time.deltaTime;
            //transform.Translate(x, 0, 0);
            rigid.linearVelocity = new Vector2(inputVec.x * speed, rigid.linearVelocityY);
        }
	}

	void HandleWalkSound()
	{
		if (!isRunning)
		{
			walkSoundTimer = 0f; // 멈추면 타이머 리셋
			//GameManager.Instance.audioManager.StopRunSound();    // 효과음 재생 중지
			isPlayedRunSound = false;
			return;
		}

		walkSoundTimer += Time.deltaTime;

		if (walkSoundTimer >= walkSoundInterval)
		{	
			// 효과음 재생
			if(!isPlayedRunSound)
			{
				//GameManager.Instance.audioManager.PlayRunSound(0.5f);
				isPlayedRunSound = true;
			}
			walkSoundTimer = 0f;
		}
	}

	public void HandleFlip()    // 방향 플립
	{
		if (inputVec.x > 0)
			sprite.flipX = false;
		else if (inputVec.x < 0)
			sprite.flipX = true;
	}

	public void TakeDamage(int attack)      // 플레이어 데미지
	{
		GameManager.Instance.audioManager.PlayDamagedSound(1f);         // 데미지 사운드 재생
		GameManager.Instance.playerStatsRuntime.currentHP -= attack;    // 체력 감소

		if (GameManager.Instance.playerStatsRuntime.currentHP <= 0)     // 체력이 0 이하일 때
		{
			if (playerDieCoroutine == null)
				playerDieCoroutine = StartCoroutine(PlayerDie());
			return;
		}

		// 이미 실행 중이면 중단 (연속 피격 대응)
		if (damageCanvasCoroutine != null)
			StopCoroutine(damageCanvasCoroutine);

		if (damagedColorCoroutine != null)
			StopCoroutine(damagedColorCoroutine);

		if (slowCoroutine != null)
			StopCoroutine(slowCoroutine);

		damageCanvasCoroutine = StartCoroutine(ShowDamagedCanvas());
		damagedColorCoroutine = StartCoroutine(PlayerDamagedColor());
		slowCoroutine = StartCoroutine(CheckPlayerSlowTime());
    }

	IEnumerator PlayerDie()             // 데미지 UI 코루틴
	{
		if (glitchGlobalVolume != null && tvGlobalVolume)
		{
			glitchGlobalVolume.SetActive(true);
			tvGlobalVolume.SetActive(true);
			yield return new WaitForSeconds(0.5f);
			blackCanvas.gameObject.SetActive(true);
			GameManager.Instance.sceneReloader.SetAlpha(1f);
			yield return new WaitForSeconds(0.5f);
            RespawnPlayer();
            playerDieCoroutine = null;
        }
	}
    void RespawnPlayer()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;

        transform.position = GameManager.Instance.spawnPoint;

        GameManager.Instance.playerStatsRuntime.currentHP = GameManager.Instance.playerStatsRuntime.maxHP;

        blackCanvas.gameObject.SetActive(false);

        glitchGlobalVolume.SetActive(false);
        tvGlobalVolume.SetActive(false);
    }
    IEnumerator ShowDamagedCanvas()             // 데미지 UI 코루틴
	{
		damagedCanvas.SetActive(true);
		yield return new WaitForSeconds(1f);
		damagedCanvas.SetActive(false);
	}

	IEnumerator PlayerDamagedColor()            // 데미지 플레이어 색 변경
	{
		sprite.color = Color.red;
		yield return new WaitForSeconds(0.2f);
		sprite.color = Color.white;
	}

	IEnumerator CheckPlayerSlowTime()		// 플레이어 데미지 시간 체크
	{
        isPlayerSlow = false;   // Shift 슬로우 모드 해제
        isDamageSlow = true;
        StartSlow();
		solihoutte.Active = true;
		yield return new WaitForSeconds(slowTime);
        isDamageSlow = false;
        solihoutte.Active = false;
		StopSlow();
	}

	public void CheckGround(Collision2D collision)      // 바닥 체크
	{
		isGrounded = Physics2D.OverlapCircle(pos.position, checkRadious, isLayer);
		isJump = false;
	}

	void SetPlayerState(playerState state)      // 플레이어 상태 변경
	{
		animator.SetInteger(Globals.AnimationVarName.playerState, (int)state);
	}

	void UpdateAnimation()      // 애니메이션 업데이트
	{
        if (!isGrounded && isJump)
        {
            SetPlayerState(playerState.Jump);
            isRunning = false;
            return;
        }

        if (justLanded && isGrounded)
        {
            SetPlayerState(playerState.Land);
            return;
        }

        if (isGrounded)
		{
            bool hasMoveInput = Mathf.Abs(inputVec.x) > 0.01f;      // 플레이어가 가만히 있을 때
            if (hasMoveInput)
			{
				SetPlayerState(playerState.Run);
				curTime = 0f;
				isRunning = true;
			}
			else
			{
				curTime += Time.deltaTime;

				if (curTime >= maxTime)
					SetPlayerState(playerState.Idle);
				isRunning = false;
			}
		}
		else
		{	
			isRunning = false;
		}
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

	public void HandleSlowMode()        // 슬로우 모드
	{
		if (Keyboard.current.leftShiftKey.wasPressedThisFrame)
		{
			if(!isPlayerSlow)
			{
                // 슬로우 코루틴 시작
                isPlayerSlow = true;
				StartSlow();
                Debug.Log("start slow");

				solihoutte.Active = true;
			}
			else
			{
                isPlayerSlow = false;
                solihoutte.Active = false;
                StopSlow();
            }
        }
		// 그래플링 훅 발사 시 슬로우모션 종료
		if(grappling.isAttach)
		{
			if(isPlayerSlow)
			{
				StopSlow();

                if (slowCoroutine != null)
                    StopCoroutine(slowCoroutine);

                solihoutte.Active = false;
            }
		}
        // 게이지 다 닳으면 플레이어 슬로우 종료
        if (isPlayerSlow && slowGauge <= 0f)
        {
            isPlayerSlow = false;
            solihoutte.Active = false;
            StopSlow();
        }
    }

	void UpdateSlowGauge()      // 슬로우 게이지 업데이트
	{
        if (slowGaugeSlider == null) return;
		if (isPlayerSlow)
		{
			slowGauge -= slowDecreaseRate * Time.unscaledDeltaTime;

			if (slowGauge <= 0f)
			{
				slowGauge = 0f;
				StopSlow();
			}
		}
		else
		{
			slowGauge += slowRecoverRate * Time.unscaledDeltaTime;
			if (slowGauge > slowMaxGauge)
				slowGauge = slowMaxGauge;
		}
		slowGaugeSlider.value = slowGauge / slowMaxGauge;
    }
    void StartSlow()    // 슬로우 효과 시작
    {
        isSlow = true;
        Time.timeScale = slowFactor;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
		if (colorAdjustments != null)
            colorAdjustments.saturation.value = -100f;
		if (bloom != null)
            bloom.intensity.value = 3;
        mixer.SetFloat("MasterCutoff", 1000f);   // 먹먹
    }

    void StopSlow()     // 슬로우 효과 종료
	{
        if (isPlayerSlow || isDamageSlow)
            return;
        isSlow = false;
        Time.timeScale = 1f;            // 시간 원래대로
		Time.fixedDeltaTime = 0.02f;
        if (colorAdjustments != null)
            colorAdjustments.saturation.value = 0f;
        if (bloom != null)
            bloom.intensity.value = 0.8f;
        mixer.SetFloat("MasterCutoff", 22000f); // 원래 소리
	}

}