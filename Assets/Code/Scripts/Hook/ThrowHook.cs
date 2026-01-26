using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

using tagName = Globals.TagName;

public class ThrowHook : MonoBehaviour
{
	[Header("그래플링 훅 갈고리 프리펩")]
	public GameObject hook;
	public bool isHookActive;   // 훅 활성화 여부

	Camera mainCam;         // 메인 카메라 정보
	GameObject curHook;     // 현재 훅
	float distance;         // 발사 훅 길이

	private void Start()
	{
		mainCam = Camera.main;
		distance = GameManager.Instance.playerStats.hookDistance;
	}

	private void Update()
	{
		// 마우스 좌클릭 시
		if (Mouse.current.leftButton.wasPressedThisFrame)
		{
			if (!isHookActive)  // 훅이 활성화되지 않았을 경우
			{
				ActiveHook();   // 훅 발사

				isHookActive = true;
			}
			else
			{
				DestroyHook();  // 훅 삭제

				isHookActive = false;
			}
		}
	}

	void ActiveHook()
	{
		Vector3 mouseScreen = Mouse.current.position.ReadValue();                       // 스크린 좌표 구하기
		mouseScreen.z = Mathf.Abs(mainCam.transform.position.z);                        // z값 보정
		Vector2 worldPos = mainCam.ScreenToWorldPoint(mouseScreen);                     // 월드 좌표
		Vector2 dir = (worldPos - (Vector2)transform.position).normalized;              // 광선 방향
		LayerMask mask = LayerMask.GetMask(tagName.ground);                            // 레이케스트 플레이어 충돌 무시
		RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, distance, mask);  // 자기 위치에서 dir 방향으로 광선 발사

		if (hit)
		{
			Vector2 destiny = hit.point;  // Raycast로 쐈을 때 충돌된 위치
			curHook = Instantiate(hook, transform.position, Quaternion.identity);   // 플레이어 위치에 훅 생성
			curHook.GetComponent<TestHooking>().destiny = destiny;
		}
	}

	void DestroyHook()
	{
		Destroy(curHook);
	}
}
