using UnityEngine;

/// <summary>
/// 전역으로 열거형(Enum) 모아놓는 파일
/// </summary>
/// 
namespace EnumType
{
	// 메인 메뉴 설정 버튼 타입
	public enum BTNType
	{
		Start,
		Option,
		Sound,
		Back,
		Quit,
		Leave
	}

	// 적 상태
	public enum EnemyState
	{
		None = 0,	// 기본
		Thrown,		// 던져짐
	}

	// 오브젝트 상태
	public enum ObjState
	{
		None = 0,	// 기본
		Thrown,		// 던져짐
	}

	// 플레이어 상태
	public enum PlayerState
	{
		Idle = 0,		// 기본 (대기)
		Run,			// 달리기
		Jump,           // 점프
		Damaged,        // 데미지
		Grappling,		// 와이어 던지기
		Hanging,		// 와이어 매달리기
		SpeedUp,		// 가속도 얻기
		PickUp,			// 적 및 오브젝트 집기
		Throw,			// 적 및 오브젝트 던지기
		PickAndHook,	// 적 및 오브젝트 잡은 상태에서 와이어 던지기
	}
}