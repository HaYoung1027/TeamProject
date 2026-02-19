using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using tagName = Globals.TagName;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class TestGrapplingHook : MonoBehaviour
{
	[Header("그래플링 훅 갈고리 프리펩")]
	public GameObject hook;
	[Header("임시 표시선 visualizerLine 프리펩")]
	public GameObject visualizerLine;
	[Header("오프셋 (잡힌 오브젝트 이동 보정값)")]
	public Vector3 followOffset = new Vector3(1f, 0f, 0f);  // 기본값: (1, 0, 0)

	/* 카메라 */
	private Camera mainCam;     // 메인 카메라

	/* 훅 */
	[HideInInspector] public bool isAttach;			// 훅 사용 여부
	[HideInInspector] public bool isGrab;			// 훅 잡음 여부
	[HideInInspector] public GameObject curHook;	// 현재 훅
	private float distance;							// 훅 길이
	private List<Transform> hookingList = new List<Transform>();    // 그래플링 훅으로 잡은 요소 리스트

	/* 플레이어 */
	private PlayerController player;

	/* 임시 표시선 */
	private LineRendererAtoB lineAtoB;  // 임시 표시선 관련 데이터

	/* 사운드 */
	private bool hasPlayedAttachSound = false;

	/* 부스트 */
	private Coroutine currentBoost;         // 현재 부스트 코루틴
	public float boostMultiplier = 1.5f;    // 속도 증가 배율
	public float boostDuration = 0.5f;      // Boost 지속 시간

	/* 슬로우모션 */
	[Header("슬로우 비율")]
	public float slowFactor;            // 슬로우 비율
	[Header("슬로우 복귀 시간")]
	public float slowLength;            // 슬로우 복귀 시간
	private ColorAdjustments colorAdjustments;
	private SpriteRenderer sprite;

	private void Awake()
	{
		sprite = GetComponent<SpriteRenderer>();
	}

	private void Start()
	{
		/* 훅 */
		isAttach = false;
		isGrab = false;

		/* 플레이어 */
		player = GameManager.Instance.playerController;

		/* 임시 표시선 */
		lineAtoB = Instantiate(visualizerLine).GetComponent<LineRendererAtoB>();    // 인스턴스화 시킨 오브젝트의 스크립트 컴포넌트 저장하기
		distance = GameManager.Instance.playerStatsRuntime.hookDistance;            // 표시선 길이 불러오기

		/* 카메라 */
		mainCam = Camera.main;      // 메인 카메라 정보 가져오기
	}

	private void Update()
	{
		CursorPathMarking();    // 임시 표시선 그리기
		ActiveHook();           // 훅 사용
	}

	private void LateUpdate()
	{
		MoveElementPos();       // 잡힌 요소의 좌표 이동
	}

	// 훅 사용 (후킹, 해제, 던지기)
	private void ActiveHook()
	{
		// 훅이 활성화되지 않고 좌클릭 했을 때
		if (!isAttach && Mouse.current.leftButton.wasPressedThisFrame)
		{
			Vector2 worldPos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());  // 월드 좌표
			Vector2 dir = (worldPos - (Vector2)transform.position).normalized;                  // 광선 방향
			LayerMask mask = ~LayerMask.GetMask(tagName.player);                                // 레이케스트 땅만 맞출 수 있도록 마스크 생성
			RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, distance, mask);      // 자기 위치에서 dir 방향으로 광선 발사

			if (hit)
			{
				// 효과음 재생
				if (!hasPlayedAttachSound)      // 갈고리 or 적에 처음 붙었을 때
				{
					GameManager.Instance.audioManager.HookShootSound(0.7f); // 갈고리 발사 효과음
					hasPlayedAttachSound = true;
				}

				// 부딪힌 요소가 적 or 던져지는 적 or 오브젝트 or 던져지는 오브젝트일 경우
				if (!isGrab && (hit.collider.CompareTag(tagName.enemy) || hit.collider.CompareTag(tagName.throwingEnemy)
					|| hit.collider.CompareTag(tagName.obj) || hit.collider.CompareTag(tagName.throwingObj)))
				{
					AttachElement(hit.transform);   // 요소 잡기
					isGrab = true;
				}
				// 땅과 부딪혔을 때
				else if (hit.collider.CompareTag(tagName.ground))
				{
					TestHooking hooking;
					Vector2 destiny = hit.point;    // Raycast로 쐈을 때 충돌된 위치
					curHook = Instantiate(hook, transform.position, Quaternion.identity);   // 플레이어 위치에 훅 생성

					hooking = curHook.GetComponent<TestHooking>();
					hooking.destiny = destiny;
					hooking.isHit = true;

					// 점 사이 거리를 고려하여 거리만큼의 점 갯수 구하기
					float len = Vector2.Distance(transform.position, destiny);
					hooking.lineLen = len;

					isAttach = true;    // 훅 활성 여부 변경
				}
			}
		}
		// 좌클릭 해제시
		else if (Mouse.current.leftButton.wasReleasedThisFrame)
		{
			// 훅 할당 해제
			if (curHook != null)
				Destroy(curHook);

			if (isAttach)
			{
				isAttach = false;
				hasPlayedAttachSound = false;

				// 줄 놓았을 때 가속도 조정
				Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
				Vector2 slow = new Vector2(0.25f, 0.7f);
				playerRb.linearVelocity *= slow;
			}

		}
		// 요소를 잡고 있고, 마우스를 우클릭 했을 경우
		else if (isGrab && Mouse.current.rightButton.wasPressedThisFrame)
		{
			Vector2 worldPos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());	// 월드 좌표
			Vector2 dir = (worldPos - (Vector2)transform.position).normalized;      // 광선 방향
			ThrowElement(hookingList[0], dir);  // 적 던지기
			isGrab = false;
		}
	}

	// 임시 표시선 그리기
	public void CursorPathMarking()
	{
		if (Mouse.current == null) return;
		if (GameManager.Instance.dialogSystem && GameManager.Instance.dialogSystem.isAction) return;    // 상호작용 중일 경우 표시선 그리지 않음

		Vector3 mouseScreen = Mouse.current.position.ReadValue();                       // 스크린 좌표 구하기
		mouseScreen.z = Mathf.Abs(mainCam.transform.position.z);                        // z값 보정
		Vector2 worldPos = mainCam.ScreenToWorldPoint(mouseScreen);                     // 월드 좌표
		Vector2 dir = (worldPos - (Vector2)transform.position).normalized;              // 광선 방향
		LayerMask mask = ~LayerMask.GetMask(tagName.player);                            // 레이케스트 플레이어 충돌 무시
		RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, distance, mask);  // 자기 위치에서 dir 방향으로 광선 발사

		if (isAttach)   // 훅 사용 중일 경우 선 비활성화
		{
			lineAtoB.Stop();
			return;
		}

		if (hit)        // 광선에 부딪히는 오브젝트가 있으면 선 활성화
		{
			if (hit.collider.CompareTag(tagName.npc))   // 부딪힌 요소가 NPC일 경우 선 비활성화
			{
				lineAtoB.Stop();
				return;
			}

			// 부딪힌 요소에 따라 선 색상 변경
			// 뭔가를 들고 있을 때 오브젝트나 몬스터가 부딪혔을 경우
			if (isGrab && (hit.collider.CompareTag(tagName.enemy) || hit.collider.CompareTag(tagName.obj)))
				lineAtoB.SetLineColor(new Color(1f, 0.2f, 0.2f));
			else if (hit.collider.CompareTag(tagName.obj))
				lineAtoB.SetLineColor(new Color(0.49f, 0.85f, 0.45f));
			else
				lineAtoB.SetLineColor(new Color(0.18f, 0.76f, 1f));

			lineAtoB.Play(transform.position, hit.point);
		}
		else
			lineAtoB.Stop();
	}

	// 잡기
	public void AttachElement(Transform element)
	{
		if (hookingList.Contains(element) || isGrab) return;

		LongRangeEnemy enemyAttack = element.GetComponent<LongRangeEnemy>();
		if (enemyAttack != null)
			enemyAttack.isGrabbed = true;

		DroneEnemy droneEnemy = element.GetComponent<DroneEnemy>();
		if (droneEnemy != null)
			droneEnemy.isGrabbed = true;

		EnemyController enemyController = element.GetComponent<EnemyController>();
		if (enemyController != null)
			enemyController.isGrounded = false;

		hookingList.Add(element);   // 리스트에 추가하기
		Collider2D elementCol = element.GetComponent<Collider2D>();
		Collider2D playerCol = GetComponent<Collider2D>();
		Rigidbody2D rb = element.GetComponent<Rigidbody2D>();

		if (elementCol != null && playerCol != null)            // 플레이어가 자기 자신을 잡았을 때 -> 충돌 무시
			Physics2D.IgnoreCollision(elementCol, playerCol, true);

		if (rb != null)                                     // Rigidbody가 있으면 Kinematic으로
			rb.bodyType = RigidbodyType2D.Kinematic;

		element.SetParent(transform);   // 플레이어 자식으로

		isGrab = true;
	}

	// 던지기
	public void ThrowElement(Transform element, Vector2 throwDir)
	{
		if (!hookingList.Contains(element)) return;
		LongRangeEnemy longRangeEnemy = element.GetComponent<LongRangeEnemy>();
		if (longRangeEnemy != null)
		{
			longRangeEnemy.isGrabbed = false;
			longRangeEnemy.ResetAttackState();
		}

		DroneEnemy droneEnemy = element.GetComponent<DroneEnemy>();
		if (droneEnemy != null)
		{
			droneEnemy.isGrabbed = false;
			droneEnemy.ResetAttackState();
		}
		GameManager.Instance.audioManager.HookThrowEnemySound(1f);  // 적 던지는 효과음
		hookingList.Remove(element);
		element.SetParent(null);    // 부모 해제

		if (element.gameObject.CompareTag(tagName.enemy))           // 태그가 적일 때
			element.gameObject.tag = tagName.throwingEnemy;         // 던져지는 적 태그로 변경
		else if (element.gameObject.CompareTag(tagName.obj))        // 태그가 오브젝트일 때
			element.gameObject.tag = tagName.throwingObj;           // 던져지는 오브젝트 태그로 변경

		Collider2D enemyCol = element.GetComponent<Collider2D>();
		Collider2D playerCol = GetComponent<Collider2D>();
		Rigidbody2D rb = element.GetComponent<Rigidbody2D>();

		if (rb != null)
		{
			rb.bodyType = RigidbodyType2D.Dynamic;
			rb.linearVelocity = Vector2.zero;
			rb.AddForce(throwDir.normalized * GameManager.Instance.playerStatsRuntime.hookThrowForce, ForceMode2D.Impulse);
		}

		if (hookingList.Count == 0)
		{
			isGrab = false;
			hasPlayedAttachSound = false;
		}
	}

	// 위치 이동하기(그래플링 훅으로 잡았을 경우만)
	public void MoveElementPos()
	{
		if (!isGrab) return;

		SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();   // 훅에 있는 플레이어 SpriteRenderer 가져오기

		for (int i = 0; i < hookingList.Count; i++)
		{
			if (hookingList[i] == null) continue;

			Vector3 offset = followOffset;
			offset.x = playerSprite.flipX ? -Mathf.Abs(followOffset.x) : Mathf.Abs(followOffset.x); // followOffset을 기준으로 x를 왼쪽/오른쪽 방향 맞춤
			hookingList[i].localPosition = offset; // 부모 transform 기준 localPosition
		}
	}
}
