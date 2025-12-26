using UnityEngine;
using UnityEngine.InputSystem;

public class CursorPathMarking : MonoBehaviour
{
	[Header("메인 카메라")]
	public Camera mainCam;
	[Header("라인렌더러")]
	public LineRendererAtoB visualizerLine;

	GrapplingHook hook;	// 그래플링 훅 정보
	float distance = 0f;    // 표시선 길이

	private void Awake()
	{
		hook = GetComponent<GrapplingHook>();
		Debug.Log(hook);
	}

	private void Start()
	{
		distance = GameManager.Instance.playerStats.hookDistance;
	}

	void Update()
	{
		// 스크린 좌표 구하기
		if (Mouse.current == null) return;
		Vector3 mouseScreen = Mouse.current.position.ReadValue();
		mouseScreen.z = Mathf.Abs(mainCam.transform.position.z);	// z값 보정

		// 월드 좌표
		Vector2 worldPos = mainCam.ScreenToWorldPoint(mouseScreen);

		// 광선 방향
		Vector2 dir = (worldPos - (Vector2)transform.position).normalized;

		// 레이케스트 플레이어 충돌 무시
		LayerMask mask = ~LayerMask.GetMask("Player");

		// 자기 위치에서 dir 방향으로 광선 발사
		RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, distance, mask);

		// 훅 사용 중이거나 몬스터를 잡고 있을 경우 선 비활성화
		if(hook.isAttach || hook.isEnemyAttach)
		{
			visualizerLine.Stop();
			return;
		}

		// 광선에 부딪히는 오브젝트가 있으면 선 활성화
		if (hit)
		{
			// 부딪힌 요소가 NPC일 경우 선 비활성화
			if(hit.collider.tag == "NPC")
			{
				visualizerLine.Stop();
				return;
			}

			// 부딪힌 요소에 따라 선 색상 변경
			if (hit.collider.tag == "Object")
			{
				visualizerLine.SetLineColor(new Color(0.49f, 0.85f, 0.45f));
			}
			else
			{
				visualizerLine.SetLineColor(new Color(0.18f, 0.76f, 1f));
			}

			visualizerLine.Play(transform.position, hit.point);
		}
		else
		{
			visualizerLine.Stop();
		}
	}
}

