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

	[Header("적들")]
	public List<SpriteRenderer> enemySprites = new List<SpriteRenderer>();

	[Header("배경들")]
	public List<SpriteRenderer> backgroundSprites = new List<SpriteRenderer>();

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
	private Coroutine damagedCheckCoroutine;	// 데미지 시간 체크 코루틴

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
			float x = inputVec.x * speed * Time.deltaTime;
			transform.Translate(x, 0, 0);
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

		if (damagedCheckCoroutine != null)
			StopCoroutine(damagedCheckCoroutine);

		damageCanvasCoroutine = StartCoroutine(ShowDamagedCanvas());
		damagedColorCoroutine = StartCoroutine(PlayerDamagedColor());
		damagedCheckCoroutine = StartCoroutine(CheckPlagerDamagedTime());
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
			Destroy(glitchGlobalVolume.gameObject);
			Destroy(tvGlobalVolume.gameObject);
			GameManager.Instance.sceneReloader.Reload();    // 씬 리로드
		}
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

	IEnumerator CheckPlagerDamagedTime()		// 플레이어 데미지 시간 체크
	{
		StartSlow();
		solihoutte.Active = true;
		yield return new WaitForSeconds(slowTime);
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
			if (!isSlow && slowGauge > 0f)
			{
				StartSlow();
				solihoutte.Active = true;
			}
			else
			{
				StopSlow();
				solihoutte.Active = false;
			}
		}
	}

	void UpdateSlowGauge()      // 슬로우 게이지 업데이트
	{
        if (slowGaugeSlider == null) return;
		if (isSlow)
		{
			slowGauge -= slowDecreaseRate * Time.unscaledDeltaTime;

			if (slowGauge <= 0f)
			{
				slowGauge = 0f;
				StopSlow(); // 자동 해제
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
        if (isSlow) return;
        isSlow = true;
        Time.timeScale = slowFactor;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
		if (colorAdjustments != null)
            colorAdjustments.saturation.value = -50f;
		if (bloom != null)
            bloom.intensity.value = 3;
        mixer.SetFloat("MasterCutoff", 1000f);   // 먹먹
        ApplySlowColor();
    }

    void StopSlow()     // 슬로우 효과 종료
	{
		if (!isSlow) return;
		isSlow = false;
		Time.timeScale = 1f;            // 시간 원래대로
		Time.fixedDeltaTime = 0.02f;
        if (colorAdjustments != null)
            colorAdjustments.saturation.value = 0f;
        if (bloom != null)
            bloom.intensity.value = 0.8f;
        mixer.SetFloat("MasterCutoff", 22000f); // 원래 소리
        ApplyNormalColor();
	}

	void ApplySlowColor()   // 슬로우 ON
	{
		//ApplySlowColorToPlayer();
		//ApplySlowColorToEnemies();
		ApplySlowColorToBackgrounds();
	}

	void ApplyNormalColor() // 슬로우 OFF
	{
		if (sprite)
			sprite.color = Color.white;

		foreach (var enemy in enemySprites)
			if (enemy)
				enemy.color = Color.white;

		foreach (var bg in backgroundSprites)
			if (bg)
				bg.color = Color.white;
	}

	void ApplySlowColorToPlayer()
	{
		if (sprite)
			sprite.color = BoostSaturation(sprite.color);
	}

	void ApplySlowColorToEnemies()
	{
		foreach (var enemy in enemySprites)
			if (enemy)
				enemy.color = BoostSaturation(enemy.color);
	}

	void ApplySlowColorToBackgrounds()
	{
		foreach (var bg in backgroundSprites)
			if (bg)
				bg.color = ReduceSaturation(bg.color);
	}

	Color BoostSaturation(Color original)
	{
		return original;
	}

	Color ReduceSaturation(Color original)  // 채도 감소
	{
		float h, s, v;
		Color.RGBToHSV(original, out h, out s, out v);

		s = 0f; // 채도 완전 제거 -> 회색 계열
		v *= 0.6f;  // 약간 어둡게

		Color c = Color.HSVToRGB(h, s, v);

		// ★ 톤을 조금 더 죽여서 배경이 확실히 흐려짐
		c.r *= 0.9f;
		c.g *= 0.9f;
		c.b *= 0.9f;

		return c;
	}
}