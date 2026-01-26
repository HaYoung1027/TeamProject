/// <summary>
/// 글로벌 변수를 관리하는 파일
/// **반드시 불변하는 값(읽기전용)만 지정할 것!!!**
/// </summary>

public class Globals
{
	// 씬 이름(string) 관련 클래스
	public static class SceneName
	{
		public static readonly string mainMenu = "1_MainMenu";	// 1: 메인메뉴
		public static readonly string stage01 = "2_Stage0_1";	// 2: 0-1 스테이지
	}

	// 애니메이션 이름(string) 관련 클래스
	public static class AnimationVarName
	{
		public static readonly string playerState = "playerState";	// 플레이어 상태
	}

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

	// 갈고리 관련 수치값(int, float) 클래스
	public static class HookValue
	{
		public static readonly int minDistanceLimit = 2;  // 갈고리 최소 길이 (이하이면 보정)
		public static readonly int minClampDistance = 4;	// 갈고리 보정 길이 (이하일 경우 보정되는 거리)
		//public static readonly float 
	}
}