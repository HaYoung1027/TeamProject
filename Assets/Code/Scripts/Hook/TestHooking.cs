using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static Globals;

public class TestHooking : MonoBehaviour
{
	/* 훅 */
    [HideInInspector] public Vector2 destiny;
	[HideInInspector] public float speed;			// 훅 발사 속도
	[HideInInspector] public bool isHit = false;	// 땅 충돌 여부 (발사 시 갈고리가 땅에 부딪혔는지)

	/* 훅 물리 */
	[Header("훅 물리")]
	public int constraintRuns = 500;                // 실행 횟수
	public Vector2 gravityForce = new Vector2(0f, -80f);	// 로프 중력값
	public float dampingFactor = 0.9f;            // 제동 계수 (과도한 흔들림 제어용)

	/* 길이 제어 */
	[Header("훅 길이 제어")]
	public float lengthChangeSpeed = 8f;     // 길이 변화 부드러움
	public float reelSpeed = 15f;            // 감기 속도
	float currentLength; // 현재 길이
	float targetLength;  // 목표 길이

	private GameObject player;		// 플레이어 오브젝트
    private LineRenderer line;		// 훅 줄
    [HideInInspector] public int segmentCnt;		// 점 갯수
    [HideInInspector] public float lineLen;			// 줄 길이

	private List<HookSegment> hookSegments = new List<HookSegment>();
	private bool isPlayedDraftSound = false;        // 사운드 재생 여부

	private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void Start()
    {
		lineLen = Mathf.Max(HookValue.minSegmentLen, lineLen);
		segmentCnt = Mathf.Max(HookValue.minSegmentLen, (int)(lineLen / HookValue.segmentLen)); // 세그먼트 개수 계산
		line.positionCount = segmentCnt;
		speed = GameManager.Instance.playerStatsRuntime.hookSpeed;
        player = GameObject.FindGameObjectWithTag(TagName.player);		// 플레이어 태그로 정보 불러오기

		currentLength = lineLen;
		targetLength = lineLen;

		// 세그먼트 생성
		for (int i = 0; i < segmentCnt; i++)
			hookSegments.Add(new HookSegment(destiny));

		// 훅 방향 회전
		Vector2 dir = (Vector2)transform.position - destiny;
		transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90);	// +90 : 각도 보정
	}

    private void FixedUpdate()
	{
		Simulate();             // 줄 위치 업데이트

		for (int i = 0; i < constraintRuns; i++)
			ApplyContraints();

		HandleRopeLengthInput(); // 입력 -> 목표 길이 변경

		// 목표 길이를 부드럽게 따라감
		currentLength = Mathf.Lerp(currentLength, targetLength, Time.fixedDeltaTime * lengthChangeSpeed);
		lineLen = currentLength;
		if (segmentCnt > Mathf.Max(HookValue.minSegmentLen, (int)(lineLen / HookValue.segmentLen)) + 5)
		{
			segmentCnt -= 5;
			line.positionCount = segmentCnt;
			hookSegments.RemoveRange(hookSegments.Count - 5 - 1, 5);
		}

		ClampPlayerDistance();  // 플레이어 거리 제한

		HookShootAction();      // 훅 발사 액션
		RenderLine();           // 라인 그리기
	}

	// 플레이어가 로프 길이 밖으로 못 나가게 제한
	void ClampPlayerDistance()
	{
		Vector2 dir = (Vector2)player.transform.position - destiny;

		if (dir.sqrMagnitude > currentLength)
		{
			Vector2 pos = destiny + dir.normalized * currentLength;
			player.transform.position = pos;

			Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
			rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, dir.normalized);
		}
	}

	// 선 그리기
	void RenderLine()
    {
        Vector3[] ropePos = new Vector3[segmentCnt];
        for (int i = 0; i < hookSegments.Count; i++)
            ropePos[i] = hookSegments[i].CurrPos;

        line.SetPositions(ropePos);
		line.gameObject.layer = 15;
    }

    // 줄 구체화 (Verlet 적산법 사용)
    private void Simulate()
    {
		for (int i = 1; i < hookSegments.Count; i++)
		{
			HookSegment segment = hookSegments[i];
			Vector2 velocity = (segment.CurrPos - segment.OldPos) * dampingFactor;
			segment.OldPos = segment.CurrPos;
			segment.CurrPos += velocity + gravityForce * Time.fixedDeltaTime * Time.fixedDeltaTime;
			hookSegments[i] = segment;  // 현재 세그먼트 리스트에 적용하기
		}
	}

    // 세그먼트 위치 조정
    private void ApplyContraints()
    {
		// 첫 번째 세그먼트 (플레이어 위치)
        HookSegment firstSegment = hookSegments[0];	// 첫 번째 세그먼트
        firstSegment.CurrPos = destiny;             // 첫 번째 세그먼트는 라인으로 충돌된 위치
		hookSegments[0] = firstSegment;				// 현재 세그먼트 리스트에도 반영

		// 마지막 세그먼트 (훅 위치)
        HookSegment lastSegment = hookSegments[hookSegments.Count - 1]; // 마지막 세그먼트
        lastSegment.CurrPos = player.transform.position;                // 마지막 세그먼트는 플레이어 위치
		hookSegments[hookSegments.Count - 1] = lastSegment;

		float segLen = currentLength / (segmentCnt - 1); // 동적 세그먼트 길이

		for (int i = 0; i < segmentCnt - 1; i++)
        {
            HookSegment currSeg = hookSegments[i];
            HookSegment nextSeg = hookSegments[i + 1];

            float dist = Vector2.Distance(currSeg.CurrPos, nextSeg.CurrPos);	// 두 세그먼트 사이 거리 계산
            float difference = dist - segLen;							// 세그먼트 길이 차이 계산

            Vector2 changeDir = (currSeg.CurrPos - nextSeg.CurrPos).normalized;		// 변경할 세그먼트 방향 정규화
            Vector2 changeVector = changeDir * difference;							// 변경할 세그먼트 위치 벡터값 계산

            if (i == 0)		// 첫 번째 세그먼트일 경우 전체 보정값을 다음 세그먼트에 적용
                nextSeg.CurrPos += changeVector;
            else if (i == segmentCnt - 2)	// 마지막 세그먼트일 경우
                currSeg.CurrPos -= changeVector;
			else			// 첫 번째 세그먼트가 아닐 경우 해당 세그먼트와 다음 세그먼트에 수정값을 분배
			{
                currSeg.CurrPos -= (changeVector * 0.5f);
                nextSeg.CurrPos += (changeVector * 0.5f);
            }
            hookSegments[i] = currSeg;  // 현재 세그먼트 리스트에 반영
            hookSegments[i + 1] = nextSeg;
        }
    }

	// 훅 발사 액션
	public void HookShootAction()
	{
		// 훅 이동
		transform.position = Vector2.MoveTowards(transform.position, destiny, speed * Time.fixedDeltaTime);

		// TODO: 줄 이동
		
	}

	// 훅 발사/회수 액션
	//public void HookShootReelAction()
	//{
	//	// 훅 이동
	//	Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
	//	Vector2 dir = (mouseWorld - (Vector2)transform.position).normalized;
	//	Vector2 destinyPos = (Vector2)transform.position + dir * HookValue.maxSegmentLen;
	//	transform.position = Vector2.MoveTowards(transform.position, destiny, speed * Time.deltaTime);

	//	// 줄 이동

	//	// 훅 회수

	//	// 줄 회수
	//}

	// 줄 길이 변경
	void HandleRopeLengthInput()
	{
		// 훅 부딪힌 위치 아래 거리의 90% 길이로 길이 보정
		Vector2 destinyPos = destiny + new Vector2(0f, -1f);    // 땅과 부딪힌 지점의 1만큼 아래로 위치 설정 (버그 방지)
		LayerMask mask = LayerMask.GetMask(TagName.ground);		// 레이캐스트 땅만 맞출 수 있도록 마스크 생성
        RaycastHit2D hit = Physics2D.Raycast(destinyPos, Vector2.down, HookValue.maxSegmentLen, mask);        // 자기 위치에서 dir 방향으로 광선 발사

		if(hit && Vector2.Distance(destiny, hit.point) * 0.85f < currentLength)
            DecreaseRopeLength();

        // 스페이스 키 입력 시 줄 줄어들기
        if (Keyboard.current.spaceKey.isPressed)
		{
			DecreaseRopeLength();

			if (!isPlayedDraftSound)
			{
				GameManager.Instance.audioManager.HookDraftSound(1f);
				isPlayedDraftSound = true;
			}
		}
		if (Keyboard.current.spaceKey.wasReleasedThisFrame)
		{
			GameManager.Instance.audioManager.StopSFX();
			isPlayedDraftSound = false;
		}

		targetLength = Mathf.Clamp(targetLength, HookValue.minSegmentLen, lineLen);
	}

	// 줄 길어지게
	private void IncreaseRopeLength()
	{
		if(targetLength < HookValue.maxSegmentLen)
			targetLength += reelSpeed * Time.fixedDeltaTime;
	}

	// 줄 짧아지게
	private void DecreaseRopeLength()
	{
		if (targetLength > HookValue.minSegmentLen)
			targetLength -= reelSpeed * Time.fixedDeltaTime;
	}

	// 세그먼트 구조체
	public struct HookSegment
	{
		public Vector2 CurrPos;     // 현재 세그먼트 위치
		public Vector2 OldPos;      // 이전 세그먼트 위치

		public HookSegment(Vector2 pos)
		{
			CurrPos = pos;
			OldPos = pos;
		}
	}
}
