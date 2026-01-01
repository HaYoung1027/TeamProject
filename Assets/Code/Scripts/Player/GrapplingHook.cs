using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrapplingHook : MonoBehaviour
{
	public Volume globalVolume;
	public LineRenderer line;
	public Transform hook;
	private Vector2 mousedir;

	public bool isHookActive;
	public bool isLineMax;		// 그래플링 훅 길이가 최대인지
	public bool isAttach;		// 그래플링 훅 사용 중인지 여부
	public bool isEnemyAttach;  // 적 잡고 있는 중인지 여부
	public bool isObjAttach;	// 오브젝트 잡고 있는 중인지 여부
	public bool isSlowing;		// 슬로우모션 여부
	private bool hasShakedOnAttach = false;
	private bool hasPlayedAttachSound = false;
	private bool isPlayedDraftSound = false;
	private bool hasPlayedShootSound = false;
	private bool hasAppliedHookForce = false;

	// 슬로우 효과 변수
	public float slowFactor;    // 슬로우 비율
	public float slowLength;    // 원래 속도로 복귀하는 데 걸리는 시간
	private Coroutine slowCoroutine;    // 슬로우 효과 코루틴

	// 적
	public Vector3 enemyFollowOffset = Vector3.zero;
	private List<Transform> enemies = new List<Transform>();

	// 오브젝트
	public Vector3 objFollowOffset = Vector3.zero;
	private List<Transform> objs = new List<Transform>();

	private Rigidbody2D rigid;
	private SpriteRenderer sprite;
	private DistanceJoint2D hookJoint;
	private bool isStopped = false;

	PlayerController player;    // 플레이어

	public SwingBoostController swingBoostController;

	ColorAdjustments colorAdjustments;
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
		if ((isAttach || isEnemyAttach || isObjAttach) && !hasPlayedAttachSound)
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

		// 적 던지기
		if (isEnemyAttach)
		{
			if (Mouse.current.rightButton.wasPressedThisFrame && enemies.Count > 0)
			{
				Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

				Vector2 dir = mouseWorld - (Vector2)transform.position;

				ThrowEnemy(enemies[0], dir, GameManager.Instance.playerStatsRuntime.hookThrowForce);
			}
		}
		// 오브젝트 던지기
		else if (isObjAttach)
		{
			if (Mouse.current.rightButton.wasPressedThisFrame && objs.Count > 0 && objs.Count > 0)
			{
				Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

				Vector2 dir = mouseWorld - (Vector2)transform.position;

				ThrowObject(objs[0], dir, GameManager.Instance.playerStatsRuntime.hookThrowForce);
			}
		}
	}

	void LateUpdate()
	{
		MoveEnemyPos();
		MoveObjPos();
	}

	// 적 위치 이동하기 (그래플링 훅으로 잡았을 경우만)
	void MoveEnemyPos()
	{
		if (!isEnemyAttach) return;

		SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
		for (int i = 0; i < enemies.Count; i++)
		{
			if (enemies[i] == null) continue;

			Vector3 offset = enemyFollowOffset;
			offset.x = playerSprite.flipX ? -Mathf.Abs(enemyFollowOffset.x) : Mathf.Abs(enemyFollowOffset.x);

			enemies[i].localPosition = offset; // 부모 transform 기준 localPosition
		}
	}

	// 오브젝트 위치 이동하기 (그래플링 훅으로 잡았을 경우만)
	void MoveObjPos()
	{
		if (!isObjAttach) return;

		SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
		for (int i = 0; i < objs.Count; i++)
		{
			if (objs[i] == null) continue;

			Vector3 offset = objFollowOffset;
			offset.x = playerSprite.flipX ? -Mathf.Abs(objFollowOffset.x) : Mathf.Abs(objFollowOffset.x);

			objs[i].localPosition = offset; // 부모 transform 기준 localPosition
		}
	}

	// 적 잡기
	public void AttachEnemy(Transform enemy)
	{
		if (enemies.Contains(enemy) || isEnemyAttach || isObjAttach) return;

		enemies.Add(enemy);

		Collider2D enemyCol = enemy.GetComponent<Collider2D>();
		Collider2D playerCol = GetComponent<Collider2D>();

		if (enemyCol != null && playerCol != null)
			Physics2D.IgnoreCollision(enemyCol, playerCol, true);

		// Rigidbody가 있으면 Kinematic으로
		Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
		if (rb != null)
			rb.bodyType = RigidbodyType2D.Kinematic;

		// 플레이어 자식으로
		enemy.SetParent(transform);

		if (enemyCol != null)
			enemyCol.enabled = false;

		// 플레이어 SpriteRenderer 가져오기
		SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();

		// enemyFollowOffset 기준으로 x를 왼쪽/오른쪽 맞춤
		Vector3 offset = enemyFollowOffset;
		offset.x = playerSprite.flipX ? -Mathf.Abs(enemyFollowOffset.x) : Mathf.Abs(enemyFollowOffset.x);

		enemy.localPosition = offset;

		// 훅 & 줄 숨기기
		hook.gameObject.SetActive(false);
		line.enabled = false;

		isEnemyAttach = true;
		isAttach = false;
		isHookActive = false;
		isLineMax = false;
	}

	// 적 던지기
	public void ThrowEnemy(Transform enemy, Vector2 throwDir, float throwForce)
	{
		Collider2D enemyCol = enemy.GetComponent<Collider2D>();
		Collider2D playerCol = GetComponent<Collider2D>();

		if (!enemies.Contains(enemy)) return;

		GameManager.Instance.audioManager.HookThrowEnemySound(1f); // 적 던지는 효과음

		if (enemyCol != null)
			enemyCol.enabled = true;

		enemies.Remove(enemy);

		// 부모 해제
		enemy.SetParent(null);

		// Rigidbody 처리
		Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.bodyType = RigidbodyType2D.Dynamic;
			rb.linearVelocity = Vector2.zero;
			rb.AddForce(throwDir.normalized * throwForce, ForceMode2D.Impulse);
		}

		if (enemies.Count == 0)
		{
			isEnemyAttach = false;
			hasPlayedAttachSound = false;
		}

		line.enabled = true;

		// 훅 상태 초기화
		isHookActive = false;
		isLineMax = false;
		hook.GetComponent<Hooking>().joint2D.enabled = false;
		hook.gameObject.SetActive(false);
	}

	// 오브젝트 잡기
	public void AttachObject(Transform obj)
	{
		if (objs.Contains(obj) || isObjAttach || isEnemyAttach) return;

		objs.Add(obj);

		Collider2D objCol = obj.GetComponent<Collider2D>();
		Collider2D playerCol = GetComponent<Collider2D>();

		if (objCol != null && objCol != null)
			Physics2D.IgnoreCollision(objCol, playerCol, true);

		// Rigidbody가 있으면 Kinematic으로
		Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
		if (rb != null)
			rb.bodyType = RigidbodyType2D.Kinematic;

		// 플레이어 자식으로
		obj.SetParent(transform);

		if (objCol != null)
			objCol.enabled = false;

		// 플레이어 SpriteRenderer 가져오기
		SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();

		// objFollowOffset 기준으로 x를 왼쪽/오른쪽 맞춤
		Vector3 offset = objFollowOffset;
		offset.x = playerSprite.flipX ? -Mathf.Abs(objFollowOffset.x) : Mathf.Abs(objFollowOffset.x);

		obj.localPosition = offset;

		// 훅 & 줄 숨기기
		hook.gameObject.SetActive(false);
		line.enabled = false;

		isObjAttach = true;
		isAttach = false;
		isHookActive = false;
		isLineMax = false;
	}

	// 오브젝트 던지기
	public void ThrowObject(Transform obj, Vector2 throwDir, float throwForce)
	{
		Collider2D objCol = obj.GetComponent<Collider2D>();
		Collider2D playerCol = GetComponent<Collider2D>();

		if (!objs.Contains(obj)) return;

		GameManager.Instance.audioManager.HookThrowEnemySound(1f); // 적 던지는 효과음

		if (objCol != null)
			objCol.enabled = true;

		objs.Remove(obj);

		// 부모 해제
		obj.SetParent(null);

		// Rigidbody 처리
		Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.bodyType = RigidbodyType2D.Dynamic;
			rb.linearVelocity = Vector2.zero;
			rb.AddForce(throwDir.normalized * throwForce, ForceMode2D.Impulse);
		}

		if (objs.Count == 0)
		{
			isObjAttach = false;
			hasPlayedAttachSound = false;
		}

		line.enabled = true;

		// 훅 상태 초기화
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