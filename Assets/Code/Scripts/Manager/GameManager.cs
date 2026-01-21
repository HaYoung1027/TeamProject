using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager> // 싱글톤 사용
{
	private static GameManager managerInstance = null;

    [Header("Manager 관련 코드")]
    public AudioManager audioManager;
    public PoolManager poolManager;
	public DialogSystem dialogSystem;

    [Header("카메라 관련 코드")]
    public CameraShake cameraShake;

    [Header("컨트롤러")]
    public PlayerController playerController;
    public PlayerInteraction playerInteraction;
    public GrapplingHook grapplingHook;

    [Header("스탯")]
    public PlayerStats playerStats;
    public EnemyStats enemyStats;

    [Header("게임 실행 중 플레이어 스텟값 수정")]
    public PlayerStatsRuntime playerStatsRuntime;
    public EnemyStatsRuntime enemyStatsRuntime;

    [Header("메뉴 관련")]
    public ESCKey escKey;

    protected new void Awake()
    {
		if(managerInstance)
		{
			DestroyImmediate(this.gameObject);
			return;
		}

		managerInstance = this;
		DontDestroyOnLoad(this.gameObject);
        QualitySettings.vSyncCount = 0; // VSync 비활성화 (모니터 주사율 영향 제거)
        Application.targetFrameRate = 120; // 프레임 120 제한

        if (Instance != null && Instance != this) // 중복 GameManager 방지
        {
            Destroy(gameObject);
            return;
        }

        base.Awake(); // MonoSingleton의 Awake 호출

        if (playerStatsRuntime != null)     // 플레이어 초기화
            playerStatsRuntime = new PlayerStatsRuntime(playerStats);   // 스탯 값 복제
    }
}