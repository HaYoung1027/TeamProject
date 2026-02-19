using UnityEngine;

namespace EnumType
{
    // 버튼 타입
    public enum BTNType
    {
        // MainMenu 버튼
        MainStart,
        MainSetting,
        MainQuit,
        MainQuitNo,
        // Game 버튼
        Check,
        Setting,
        GameLeave,
        GameQuit,
        LeaveYes,
        LeaveNo,
        QuitYes,
        QuitNo
    }

    // 몬스터 상태
    public enum EnemyState
    {
        Idle = 0,    // 기본
        Thrown,        // 던져짐
    }

    // 오브젝트 상태
    public enum ObjState
    {
        Idle = 0,    // 기본
        Thrown,        // 던져짐
    }

    // 플레이어 상태
    public enum PlayerState
    {
        Idle = 0,        // 기본
        Run,            // 달리기
        Jump,           // 점프
        Land,           // 착지
        Damaged,        // 데미지 받은 상태
        Grappling,        // 훅 걸고 있는 상태
        Hanging,        // 훅 매달린 상태
        SpeedUp,        // 가속도 받은 상태
        PickUp,            // 훅으로 요소 집은 상태
        Throw,            // 요소 던지기
        PickAndHook,    // 잡고 던지기
    }
}