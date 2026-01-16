/// <summary>
/// 글로벌 변수를 관리하는 파일
/// **반드시 불변하는 값(읽기전용)만 지정할 것!!!**
/// </summary>

public class Globals
{
	// 태그 이름(string) 관련 클래스
	public static class TagName
	{
		// 적
		public static readonly string enemy = "Enemy";
		public static readonly string throwingEnemy = "ThrowingEnemy";
		// 오브젝트
		public static readonly string obj = "Object";
		public static readonly string throwingObj = "ThrowingObject";
		// 플레이어 관련
		public static readonly string player = "Player";
		public static readonly string hook = "Hook";
		// 배경 요소
		public static readonly string ground = "Ground";
		public static readonly string groundCheck = "GroundCheck";
		// NPC
		public static readonly string npc = "NPC";
	}
}