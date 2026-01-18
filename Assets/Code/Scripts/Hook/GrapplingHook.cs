using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

using tagName = Globals.TagName;

public class GrapplingHook : MonoBehaviour
{
    [Header("Global Volume 오브젝트")]
    public Volume globalVolume;
    [Header("Line 오브젝트 LineRenderer")]
    public LineRenderer line;
    [Header("Hook 오브젝트 Transform")]
    public Transform hook;
    [Header("훅 활성화 상태 여부")]
    public bool isHookActive;
    [Header("그래플링 훅 길이가 최대인지")]
	public bool isLineMax;
    [Header("그래플링 훅 사용 중인지 여부")]
    public bool isAttach;
    [Header("그래플링 훅으로 무언가를 잡았는지 여부")]
    public bool isAttachElement;
    [Header("슬로우모션 여부")]
    public bool isSlowing;
    [Header("슬로우 비율")]
    public float slowFactor;
    [Header("원래 속도로 복귀하는 데 걸리는 시간")]
    public float slowLength;
    [Header("오프셋 (잡힌 오브젝트 이동 보정값)")]
    public Vector3 followOffset = new Vector3(1f, 0f, 0f);  // 기본값: (1, 0, 0)
    [Header("회전 에너지 게이지 UI")]
    public Slider swingGauge;
    [Header("저장가능한 최대 회전 수(에너지 제한)")]
    public int maxTurns = 1;
    [Header("회전 중이지 않을 때 게이지 감소 속도")]
    public float decreaseSpeed = 400f;
    [Header("회전할 때 회전량 증가 배율")]
    public float increaseMultiplier = 1.0f;
    [Header("회전으로 인정할 최소 각도 변화")]
    public float turnMinDelta = 0.3f;
    [Header("속도 증가 배율")]
    public float boostMultiplier = 1.5f;
    [Header("Boost 지속 시간")]
    public float boostDuration = 0.5f;

    private Vector2 mousedir;
    private bool hasShakedOnAttach = false;
	private bool hasPlayedAttachSound = false;
	private bool isPlayedDraftSound = false;
	private bool hasPlayedShootSound = false;
	private bool hasAppliedHookForce = false;
    private float accumulatedAngle = 0f;    // 누적 회전량(게이지 수치)
    private float maxAngle;                 // maxTurns 회전 시 최대 각도(= 360 * maxTurns)
    private float previousAngle;            // 이전 프레임의 각도
    private bool angleInitialized = false;  // 첫 프레임 각도 초기화 여부
    private int storedDirection = 0;        // 저장된 회전 방향(1=시계, -1=반시계, 0=없음)
    private Coroutine currentBoost;
    private Coroutine slowCoroutine;    // 슬로우 효과 코루틴
	private Rigidbody2D rigid;
	private SpriteRenderer sprite;
	private DistanceJoint2D hookJoint;
	ColorAdjustments colorAdjustments;
	List<Transform> hookingList = new List<Transform>();    // 그래플링 훅으로 잡은 요소 리스트


	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		sprite = GetComponent<SpriteRenderer>();
		maxAngle = maxTurns * 360f;
	}

	void Start()
	{
		// 라인을 그리는 포지션을 두개로 설정하고 (PositionCount)
		// 한 점은 Player의 포지션, 한 점은 Hook의 포지션으로 설정 (SetPosition)
		line.positionCount = 2;
		line.endWidth = line.startWidth = 0.05f;
		line.SetPosition(0, transform.position);
		line.SetPosition(1, hook.position);
		line.useWorldSpace = true;
		isAttach = false;
		isSlowing = false;
		hook.gameObject.SetActive(false);

		hookJoint = hook.GetComponent<DistanceJoint2D>();

		if (globalVolume == null)
		{
			Debug.LogError("❌ Global Volume이 할당되지 않았음");
			return;
		}

		if (!globalVolume.profile.TryGet(out colorAdjustments))
		{
			Debug.LogError("❌ ColorAdjustments가 Volume Profile에 없음");
		}
	}
	void Update()
	{
		if (TimelineController.isTimelinePlaying) return;   // 컷씬 재생 중일 때는 갈고리 불가

        UpdateLine();
        HandleHookShoot();
		HandleHookMove();
        HandleAttachState();
        HandleSwingGauge();
        HandleThrow();
	}

	void LateUpdate()
	{
		MoveElementPos();
	}

    void UpdateLine() // 라인 업데이트
    {
        line.SetPosition(0, transform.position);
        line.SetPosition(1, hook.position);
    }
	void HandleHookShoot() // 훅 발사 입력처리
	{
        if (Mouse.current.leftButton.wasPressedThisFrame && !isHookActive && !isAttach)
        {
            GameManager.Instance.cameraShake.ShakeForSeconds(0.1f); // 카메라 흔들기
            GameManager.Instance.audioManager.HookShootSound(0.7f); // 갈고리 발사 효과음
            hook.position = transform.position;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorldPos.z = 0f;

            mousedir = mouseWorldPos - transform.position;
            isHookActive = true;
            hook.gameObject.SetActive(true);
            line.enabled = true;
        }
    }

    void HandleHookMove() // 훅 이동 / 복귀 처리
    {
        if (!isHookActive || isAttach) return;
        // 훅이 발사된 상태이고, 아직 최대 사거리에 도달하지 않았을 때
        if (!isLineMax)
        {
			// 마우스 방향으로 훅을 전진시킴
            hook.Translate(mousedir.normalized * Time.deltaTime * GameManager.Instance.playerStatsRuntime.hookSpeed);
            // 플레이어와 훅 사이의 거리가 최대 사거리보다 커지면
            if (Vector2.Distance(transform.position, hook.position) > GameManager.Instance.playerStatsRuntime.hookDistance)
            {
                // 최대 사거리 도달 상태로 전환
                isLineMax = true;
            }
        }
        // 훅이 최대 사거리에 도달한 이후
        else
        {
            // 훅을 플레이어 위치로 부드럽게 되돌림
            hook.position = Vector2.MoveTowards(hook.position, transform.position, Time.deltaTime * GameManager.Instance.playerStatsRuntime.hookSpeed);
            // 훅이 거의 플레이어 위치까지 돌아왔을 경우
            if (Vector2.Distance(transform.position, hook.position) < 0.1f)
            {
                // 훅 상태 초기화
                isHookActive = false;
                isLineMax = false;
                hook.gameObject.SetActive(false);
            }
        }
    }

    void HandleAttachState() // 그래플링 붙어 있을 때 처리
    {
        if (!isAttach) return;
        // 갈고리 or 적에 처음 붙었을 때
        if (!hasPlayedAttachSound)
        {
            GameManager.Instance.audioManager.HookAttachSound(1f);
            hasPlayedAttachSound = true;
        }

        if (!hasShakedOnAttach)
        {
            GameManager.Instance.cameraShake.ShakeForSeconds(0.1f);
            hasShakedOnAttach = true;
        }

        HandleDetachInput();
        HandleRopeDraft();
    }

    void HandleDetachInput() // 붙은 상태에서 해제 처리
    {
        // 마우스를 땠을 때만 해제
        if (!Mouse.current.leftButton.wasReleasedThisFrame) return;

        isAttach = false;
        isHookActive = false;
        isLineMax = false;
        hasShakedOnAttach = false;
        hasPlayedAttachSound = false;

        hook.GetComponent<Hooking>().joint2D.enabled = false;
        hook.gameObject.SetActive(false);

        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }

        slowCoroutine = StartCoroutine(SlowRoutine());

        Boost(GetGaugePercent());
    }

    void HandleRopeDraft() // 줄 당기기 처리
    {
        // 스페이스 줄 당기기
        if (Keyboard.current.spaceKey.isPressed)
        {
            if (hookJoint != null && hookJoint.enabled)
            {
                hookJoint.distance = Mathf.Max(0.5f, hookJoint.distance - 0.1f);

                if (!isPlayedDraftSound)
                {
                    GameManager.Instance.audioManager.HookDraftSound(1f);
                    isPlayedDraftSound = true;
                }
            }
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            GameManager.Instance.audioManager.StopSFX();
            isPlayedDraftSound = false;
        }
    }

    void HandleSwingGauge() // 회전 게이지 처리 분리
    {
        if (!isAttach)
        {
            ResetSwingGauge();
            return;
        }

        swingGauge.gameObject.SetActive(true);  // 게이지 UI 활성화 

        bool noInput = GameManager.Instance.playerController.inputVec == Vector2.zero;

        Vector2 hookPos = hook.position;        // 갈고리(회전 중심) 좌표
        Vector2 playerPos = transform.position; // 플레이어 좌표

        Vector2 dir = (playerPos - hookPos).normalized; // 갈고리 -> 플레이어 방향 벡터
        float angleNow = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // 현재 각도(0~360°)

        // 첫 프레임에서는 이전 각도가 없으므로 초기화
        if (!angleInitialized)
        {
            previousAngle = angleNow;
            angleInitialized = true;
        }

        // 프레임 간 각도 변화 계산 (360° 넘어가도 정확하게 처리)
        float delta = Mathf.DeltaAngle(previousAngle, angleNow);
        previousAngle = angleNow; // 현재 각도를 다음 프레임을 위해 저장

        ProcessSwingDelta(delta, noInput);

        accumulatedAngle = Mathf.Clamp(accumulatedAngle, 0, maxAngle);
        swingGauge.value = accumulatedAngle / maxAngle;
    }

    void ProcessSwingDelta(float delta, bool noInput)
    {
        // delta가 충분히 크면 "회전 중"으로 처리
        if (Mathf.Abs(delta) <= turnMinDelta)
        {
            accumulatedAngle -= decreaseSpeed * Time.deltaTime;
            if (accumulatedAngle <= 0)
            {
                accumulatedAngle = 0;
                storedDirection = 0;
            }
            return;
        }

        int deltaDir = delta > 0 ? 1 : -1; // 회전 방향 판단

        // 회전 방향이 처음 결정되는 순간
        if (storedDirection == 0)
            storedDirection = deltaDir;

        if (deltaDir == storedDirection)
        {
            if (noInput)    // 입력 없을 때
            {
                accumulatedAngle += decreaseSpeed * Time.deltaTime * 0.05f;
            }
            else
            {
                // 같은 방향으로 돌면 -> 게이지 증가
                accumulatedAngle += Mathf.Abs(delta) * increaseMultiplier;
            }
        }
        else
        {
            // 반대 방향으로 돌면 -> 게이지 감소
            accumulatedAngle -= Mathf.Abs(delta) * increaseMultiplier * 1.5f;

            // 감소하다가 0 이하가 되면 방향 초기화
            if (accumulatedAngle <= 0f)
            {
                accumulatedAngle = 0;
                storedDirection = 0;
            }
        }
    }

    void ResetSwingGauge()
    {
        // 갈고리에서 떨어지면 모든 값 초기화
        swingGauge.gameObject.SetActive(false); // 게이지 숨기기
        accumulatedAngle = 0f;                  // 회전량 초기화
        swingGauge.value = 0f;                  // UI 리셋
        angleInitialized = false;               // 다음 회전 때 새 초기화 필요
        storedDirection = 0;                    // 방향 초기화
    }

    void HandleThrow() // 던지기 처리 분리
    {
        // 적 또는 오브젝트 던지기
        if (!isAttachElement) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            Vector2 dir = mouseWorld - (Vector2)transform.position;

            ThrowElement(hookingList[0], dir);

            line.enabled = true;

            // 훅 상태 초기화
            resetHook();
        }
    }


    // 적 및 오브젝트 위치 이동하기
    void MoveElementPos()
	{
		if (!isAttachElement) return;

		MovePos();
	}

	// 잡기
	public void AttachElement(Transform element)
	{
		if (hookingList.Contains(element) || isAttachElement) return;

		hookingList.Add(element);   // 리스트에 추가하기

		Collider2D elementCol = element.GetComponent<Collider2D>();
		Collider2D playerCol = GetComponent<Collider2D>();

		// 플레이어가 자기 자신을 잡았을 때 -> 충돌 무시
		if (elementCol != null && playerCol != null)
        {
            Physics2D.IgnoreCollision(elementCol, playerCol, true);
        }

		// Rigidbody가 있으면 Kinematic으로
		Rigidbody2D rb = element.GetComponent<Rigidbody2D>();
		if (rb != null)
			rb.bodyType = RigidbodyType2D.Kinematic;

		element.SetParent(transform);   // 플레이어 자식으로

		disableHook();  // 훅 & 줄 숨기기
		isAttachElement = true;    // 잡힘
	}

	// 던지기
	public void ThrowElement(Transform element, Vector2 throwDir)
	{
		if (!hookingList.Contains(element)) return;

		GameManager.Instance.audioManager.HookThrowEnemySound(1f); // 적 던지는 효과음
		hookingList.Remove(element);

		// 부모 해제
		element.SetParent(null);

		// 태그 변경
		if (element.gameObject.CompareTag(tagName.enemy))   // 태그가 적일 때
		{
			element.gameObject.tag = tagName.throwingEnemy; // 던져지는 적 태그로 변경
		}
		else if (element.gameObject.CompareTag(tagName.obj))    // 태그가 오브젝트일 때
		{
			element.gameObject.tag = tagName.throwingObj;   // 던져지는 오브젝트 태그로 변경
		}

		Collider2D enemyCol = element.GetComponent<Collider2D>();
		Collider2D playerCol = GetComponent<Collider2D>();

		// Rigidbody 처리
		Rigidbody2D rb = element.GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.bodyType = RigidbodyType2D.Dynamic;
			rb.linearVelocity = Vector2.zero;
			rb.AddForce(throwDir.normalized * GameManager.Instance.playerStatsRuntime.hookThrowForce, ForceMode2D.Impulse);
		}

		if (hookingList.Count == 0)
		{
			isAttachElement = false;
			hasPlayedAttachSound = false;
		}
	}

	// 위치 이동하기(그래플링 훅으로 잡았을 경우만)
	public void MovePos()
	{
		if (!isAttachElement) return;

		// 훅에 있는 플레이어 SpriteRenderer 가져오기
		SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
		for (int i = 0; i < hookingList.Count; i++)
		{
			if (hookingList[i] == null) continue;

			// followOffset을 기준으로 x를 왼쪽/오른쪽 방향 맞춤
			Vector3 offset = followOffset;
			offset.x = playerSprite.flipX ? -Mathf.Abs(followOffset.x) : Mathf.Abs(followOffset.x);

			hookingList[i].localPosition = offset; // 부모 transform 기준 localPosition
		}
	}

	// 훅 & 줄 숨기기
	public void disableHook()
	{
		hook.gameObject.SetActive(false);
		line.enabled = false;

		isAttach = false;
		isHookActive = false;
		isLineMax = false;
	}

	// 훅 상태 초기화
	public void resetHook()
	{
		isHookActive = false;
		isLineMax = false;
		hook.GetComponent<Hooking>().joint2D.enabled = false;
		hook.gameObject.SetActive(false);
	}

	// 슬로우 효과 코루틴
	IEnumerator SlowRoutine()
	{
		sprite.color = Color.red;

		if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = -50f;
        }

		Time.timeScale = slowFactor;
		Time.fixedDeltaTime = 0.02f * Time.timeScale;

		float elapsed = 0f;

		while (elapsed < slowLength)
		{
			if (GameManager.Instance.playerController.isGrounded || isAttach) break;

            elapsed += Time.unscaledDeltaTime;
			yield return null;
		}

		Time.timeScale = 1f;
		Time.fixedDeltaTime = 0.02f;
		sprite.color = Color.white;

		if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = 0f;
        }
	}

	// 힘 주기
	public void ApplyHookImpulse(Vector2 hookPos)
	{
		Vector2 dir = (hookPos - (Vector2)transform.position).normalized;

		float horizontal = dir.x > 0 ? 1f : -1f;
		float power = 3f; // 힘 세기

		rigid.AddForce(new Vector2(horizontal * power, 1.2f), ForceMode2D.Impulse);
	}

    public float GetGaugePercent()
    {
        return accumulatedAngle / maxAngle; // 0~1
    }

    public void Boost(float gaugePercent)
    {
        if (currentBoost != null)
        {
            StopCoroutine(currentBoost);
        }
        currentBoost = StartCoroutine(BoostRoutine(gaugePercent));
    }

    private IEnumerator BoostRoutine(float gaugePercent)
    {
        var stats = GameManager.Instance.playerStatsRuntime;
        float originalSpeed = stats.speed;

        float boostFactor = 1 + (boostMultiplier - 1) * gaugePercent;
        stats.speed = originalSpeed * boostFactor;

        float time = 0f;
        while (time < boostDuration)
        {
            if (GameManager.Instance.playerController.hasCollided) break;

            time += Time.deltaTime;
            yield return null;
        }
        stats.speed = originalSpeed;
        currentBoost = null;
    }
}
