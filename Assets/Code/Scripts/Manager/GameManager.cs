using UnityEngine;

public class GameManager : MonoSingleton<GameManager> // 싱글톤 사용
{
    [Header("Manager 관련 코드")]
    public AudioManager audioManager;
    public PoolManager poolManager;

    [Header("Player 관련 코드")]
    public PlayerController playerController;
    public PlayerStats playerStats;

    [Header("게임 실행 중 플레이어 스텟값 수정")]
    public PlayerStatsRuntime playerStatsRuntime;

    protected new void Awake()
    {
        QualitySettings.vSyncCount = 0; // VSync 비활성화 (모니터 주사율 영향 제거)

        Application.targetFrameRate = 120; // 프레임 120 제한
        if (Instance != null && Instance != this) // 중복 GameManager 방지
        {
            Destroy(gameObject);
            return;
        }

        base.Awake(); // MonoSingleton의 Awake 호출

        playerStatsRuntime = new PlayerStatsRuntime();
        playerStatsRuntime.CopyFrom(playerStats); // 스텟 값 복제
    }
}
