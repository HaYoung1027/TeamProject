using DG.Tweening;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using tagName = Globals.TagName;

public class GrapplingHook : MonoBehaviour
{
	public Volume globalVolume;
	public LineRenderer line;
	public Transform hook;
	private Vector2 mousedir;

	public bool isHookActive;
	public bool isLineMax;			// 그래플링 훅 길이가 최대인지
	public bool isAttach;			// 그래플링 훅 사용 중인지 여부
	public bool isAttachElement;	// 그래플링 훅으로 무언가를 잡았는지 여부
	public bool isSlowing;			// 슬로우모션 여부
	private bool hasShakedOnAttach = false;
	private bool hasPlayedAttachSound = false;
	private bool isPlayedDraftSound = false;
	private bool hasPlayedShootSound = false;
	private bool hasAppliedHookForce = false;

    // 슬로우 효과 변수
    public float slowFactor;    // 슬로우 비율
	public float slowLength;    // 원래 속도로 복귀하는 데 걸리는 시간
	private Coroutine slowCoroutine;    // 슬로우 효과 코루틴

	private Rigidbody2D rigid;
	private SpriteRenderer sprite;
	private DistanceJoint2D hookJoint;
	private bool isStopped = false;

	PlayerController player;    // 플레이어

	public SwingBoostController swingBoostController;
	
	ColorAdjustments colorAdjustments;

	List<Transform> hookingList = new List<Transform>();    // 그래플링 훅으로 잡은 요소 리스트
	Vector3 followOffset = Vector3.zero;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		sprite = GetComponent<SpriteRenderer>();
		player = GetComponent<PlayerController>();
		swingBoostController = GetComponent<SwingBoostController>();
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
		line.SetPosition(0, transform.position);
		line.SetPosition(1, hook.position);

		// 갈고리 or 적에 처음 붙었을 때
		if ((isAttach || isAttachElement) && !hasPlayedAttachSound)
		{
			GameManager.Instance.audioManager.HookAttachSound(1f);
			hasPlayedAttachSound = true;
		}

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

		// 훅이 발사된 상태이고, 아직 최대 사거리에 도달하지 않았을 때
		if (isHookActive && !isLineMax && !isAttach)
		{
			// 마우스 방향으로 훅을 전진시킴
			hook.Translate(mousedir.normalized * Time.deltaTime * GameManager.Instance.playerStatsRuntime.hookSpeed);
			// 플레이어와 훅 사이의 거리가 최대 사거리보다 커지면
			if (Vector2.Distance(transform.position, hook.position) > GameManager.Instance.playerStatsRuntime.hookDistance)
			{
				// 최대 사거리 도달 상태로 전환
				isLineMax = true;
				hasPlayedShootSound = false;
			}
		}

		// 훅이 최대 사거리에 도달한 이후
		else if (isHookActive && isLineMax && !isAttach)
		{
			// 훅을 플레이어 위치로 부드럽게 되돌림
			hook.position = Vector2.MoveTowards(hook.position, transform.position, Time.deltaTime * GameManager.Instance.playerStatsRuntime.hookSpeed);

			// 훅이 거의 플레이어 위치까지 돌아왔을 경우
			if (Vector2.Distance(transform.position, hook.position) < 0.1f)
			{
				// 훅 상태 초기화
				isHookActive = false;
				isLineMax = false;
				hasPlayedShootSound = false;
				// 훅 오브젝트 비활성화
				hook.gameObject.SetActive(false);
			}
		}

        else if (isAttach)
		{
			if (!hasShakedOnAttach)
			{
				GameManager.Instance.cameraShake.ShakeForSeconds(0.1f);
				hasShakedOnAttach = true;
			}

			//if (!hasAppliedHookForce)
			//{
			//    Vector2 dir = hook.position - transform.position;
			//    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

			//    if (angle < 90)
			//    {
			//        rb.AddForce(Vector2.right * 3f);
			//    }
			//    else if (angle > 90)
			//    {
			//        rb.AddForce(Vector2.left * 3f);
			//    }

			//    hasAppliedHookForce = true;
			//}

			// 마우스를 뗐을 때만 해제
			if (Mouse.current.leftButton.wasReleasedThisFrame)
			{
				isAttach = false;
				isHookActive = false;
				isLineMax = false;
				hasShakedOnAttach = false;
				hasPlayedAttachSound = false;
				hasAppliedHookForce = true;

				hook.GetComponent<Hooking>().joint2D.enabled = false;
				hook.gameObject.SetActive(false);

				if (slowCoroutine != null)
					StopCoroutine(slowCoroutine);

				slowCoroutine = StartCoroutine(SlowRoutine());

				if (swingBoostController != null)
				{
					swingBoostController.Boost();
				}
			}

			// 쉬프트 줄 당기기
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

		// 적 또는 오브젝트 던지기
		if (isAttachElement)
		{
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
	}

	void LateUpdate()
	{
		MoveElementPos();
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

		hookingList.Add(element);	// 리스트에 추가하기

		Collider2D elementCol = element.GetComponent<Collider2D>();
		Collider2D playerCol = GetComponent<Collider2D>();

		// 플레이어가 자기 자신을 잡았을 때 -> 충돌 무시
		if(elementCol != null && playerCol != null)
			Physics2D.IgnoreCollision(elementCol, playerCol, true);

		// Rigidbody가 있으면 Kinematic으로
		Rigidbody2D rb = element.GetComponent<Rigidbody2D>();
		if (rb != null)
			rb.bodyType = RigidbodyType2D.Kinematic;

		element.SetParent(transform);

		//if (transformCol != null)
		//	transformCol.enabled = false;

		// 훅에 있는 플레이어 SpriteRenderer 가져오기
		SpriteRenderer playerSprite = hook.GetComponent<SpriteRenderer>();

		// followOffset을 기준으로 x를 왼쪽/오른쪽 방향 맞춤
		Vector3 offset = followOffset;
		offset.x = playerSprite.flipX ? -Mathf.Abs(followOffset.x) : Mathf.Abs(followOffset.x);

		element.localPosition = offset;

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

		SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
		for (int i = 0; i < hookingList.Count; i++)
		{
			if (hookingList[i] == null) continue;

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
			colorAdjustments.saturation.value = -50f;

		Time.timeScale = slowFactor;
		Time.fixedDeltaTime = 0.02f * Time.timeScale;

		float elapsed = 0f;

		while (elapsed < slowLength)
		{
			if (player.isGrounded || isAttach)
				break;

			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}

		Time.timeScale = 1f;
		Time.fixedDeltaTime = 0.02f;
		sprite.color = Color.white;

		if (colorAdjustments != null)
			colorAdjustments.saturation.value = 0f;
	}

	// 힘 주기
	public void ApplyHookImpulse(Vector2 hookPos)
	{
		Vector2 dir = (hookPos - (Vector2)transform.position).normalized;

		float horizontal = dir.x > 0 ? 1f : -1f;
		float power = 3f; // 힘 세기

		rigid.AddForce(new Vector2(horizontal * power, 1.2f), ForceMode2D.Impulse);
	}
}