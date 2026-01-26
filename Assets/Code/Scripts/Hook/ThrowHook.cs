using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

using tagName = Globals.TagName;

public class ThrowHook : MonoBehaviour
{
	[Header("그래플링 훅 갈고리 프리펩")]
	public GameObject hook;

	GameObject curHook;     // 현재 훅
	float distance;         // 발사 훅 길이

	private void Start()
	{
		distance = GameManager.Instance.playerStats.hookDistance;
	}

	private void Update()
	{
		if (Mouse.current.leftButton.wasPressedThisFrame)
		{
			Vector3 mouseScreen = Mouse.current.position.ReadValue();                       // 스크린 좌표 구하기
			mouseScreen.z = Mathf.Abs(Camera.main.transform.position.z);                        // z값 보정
			Vector2 worldPos = Camera.main.ScreenToWorldPoint(mouseScreen);                     // 월드 좌표
			Vector2 dir = (worldPos - (Vector2)transform.position).normalized;              // 광선 방향
			LayerMask mask = LayerMask.GetMask(tagName.ground);                            // 레이케스트 플레이어 충돌 무시
			
			RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, distance, mask);  // 자기 위치에서 dir 방향으로 광선 발사

			Vector2 destiny = hit.point;  // Raycast로 쐈을 때 충돌된 위치

			curHook = Instantiate(hook, transform.position, Quaternion.identity);   // 플레이어 위치에 훅 생성

			curHook.GetComponent<TestHooking>().destiny = destiny;
		}
	}
}
