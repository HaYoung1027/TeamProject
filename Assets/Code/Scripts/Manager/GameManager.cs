using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager> // 싱글톤 사용
{
	private static GameManager managerInstance = null;

    [Header("Manager 관련 코드")]
    public AudioManager audioManager;
    public PoolManager poolManager;

    [Header("CutScene 관련 코드")]
    public DialogSystem dialogSystem;

    [Header("카메라 관련 코드")]
    public CameraShake cameraShake;

    [Header("컨트롤러")]
    public PlayerController playerController;
    public PlayerInteraction playerInteraction;

    [Header("스탯")]
    public PlayerStats playerStats;
    public EnemyStats enemyStats;

    [Header("게임 실행 중 플레이어 스텟값 수정")]
    public PlayerStatsRuntime playerStatsRuntime;
    public EnemyStatsRuntime enemyStatsRuntime;

    [HideInInspector] public Vector2 spawnPoint;            // 스폰포인트 관련
    [HideInInspector] public ESCKey escKey;					// 메뉴 관련
	[HideInInspector] public SceneReloader sceneReloader;	// 씬 관련

    protected new void Awake()
	{
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(1920, 1080, false); // 원하는 창 크기

        sceneReloader = GetComponent<SceneReloader>();
		escKey = GetComponent<ESCKey>();
		if(!escKey || !sceneReloader)
		{
			Debug.LogWarning("escKey 또는 sceneReloader 데이터 없음");
			return;
		}

		if (managerInstance)
		{
			DestroyImmediate(this.gameObject);
			return;
		}

		managerInstance = this;
		DontDestroyOnLoad(this.gameObject);
        QualitySettings.vSyncCount = 1;   // 모니터 주사율에 동기
        Application.targetFrameRate = -1; // 제한 안 함

        if (Instance != null && Instance != this) // 중복 GameManager 방지
        {
            Destroy(gameObject);
            return;
        }

        base.Awake(); // MonoSingleton의 Awake 호출

        if (playerStatsRuntime != null)     // 플레이어 초기화
            playerStatsRuntime = new PlayerStatsRuntime(playerStats);   // 스탯 값 복제
    }

    private void Start()
    {
        spawnPoint = playerController.transform.position;
    }

    public void SetSpawnPoint(Vector2 pos)
    {
        spawnPoint = pos;
    }
}