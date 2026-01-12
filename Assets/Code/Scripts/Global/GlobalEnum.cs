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
}