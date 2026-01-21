using System.Collections.Generic;
using UnityEngine;

// 하나의 풀에 대한 정보를 담는 클래스
// Inspector에서 설정하기 위해 Serializable로 선언
[System.Serializable]
public class PoolInfo
{
    public string prefabName;       // 풀을 구분하기 위한 이름 (Dictionary key로 사용)
    public GameObject prefab;       // 실제 생성할 프리팹
    public int initialSize = 10;    // 처음에 미리 만들어 둘 개수
}

public class PoolManager : MonoBehaviour
{
    public PoolInfo[] pools;        // Inspector에서 여러 풀 정보를 등록
    private Dictionary<string, Queue<GameObject>> poolDict;     // 풀 이름 → 해당 오브젝트 Queue

    void Awake()
    {
        
        poolDict = new Dictionary<string, Queue<GameObject>>(); // 풀들을 저장할 Dictionary 초기화
        
        foreach (var pool in pools)     // 풀 정보 하나씩 처리
        {
            var queue = new Queue<GameObject>();    // 해당 프리팹용 Queue 생성
            
            for (int i = 0; i < pool.initialSize; i++)  // 초기 개수만큼 미리 생성
            {
                GameObject obj = Instantiate(pool.prefab);

                // 반환 시 이름으로 풀을 구분하므로
                // 반드시 prefabName으로 이름 통일
                obj.name = pool.prefabName;
                obj.SetActive(false);                   // 일단 비활성화
                obj.transform.SetParent(transform);     // PoolManager 아래에 정리
                queue.Enqueue(obj);                     // 큐에 보관
            }
            poolDict.Add(pool.prefabName, queue);       // Dictionary에 등록
        }
    }

    // 풀에서 오브젝트를 하나 꺼내서 사용
    public GameObject SpawnFromPool(string prefabName, Vector3 pos, Quaternion rot)
    {
        // 등록되지 않은 풀 이름
        if (!poolDict.ContainsKey(prefabName))
        {
            Debug.LogWarning($"{prefabName}에 해당하는 풀 없음!");
            return null;
        }

        GameObject obj = null;
        
        if (poolDict[prefabName].Count > 0)     // 풀에 남은 오브젝트가 있으면 재사용
            obj = poolDict[prefabName].Dequeue();
        else
        {
            var poolInfo = System.Array.Find(pools, x => x.prefabName == prefabName);   // 풀에 없으면 새로 생성 (예외 처리)

            if (poolInfo != null)
            {
                obj = Instantiate(poolInfo.prefab);
                obj.name = prefabName;
            }
        }

        if (obj == null) return null;

        obj.transform.SetParent(null);      // 풀에서 꺼낸 오브젝트는 부모 해제
        obj.transform.position = pos;       // 위치 / 회전 적용
        obj.transform.rotation = rot;
        obj.SetActive(true);                // 활성화
        return obj;
    }

    // 사용이 끝난 오브젝트를 풀로 반환
    public void ReturnToPool(GameObject obj)
    {
        // 다시 비활성화
        obj.SetActive(false);

        // PoolManager 밑으로 정리
        obj.transform.SetParent(transform);

        // 이름으로 어떤 풀인지 판별
        if (poolDict.ContainsKey(obj.name))
        {
            // 정상적인 풀 오브젝트면 다시 큐에 저장
            poolDict[obj.name].Enqueue(obj);
        }
        else
        {
            // 풀에 없는 오브젝트면 관리 불가 → 제거
            Debug.LogWarning($"[{obj.name}]은(는) 풀에 등록되지 않은 오브젝트라 Destroy합니다.");
            Destroy(obj);
        }
    }
}

// GameObject bullet = GameManager.Instance.poolManager.SpawnFromPool("Bullet", firePos.position, Quaternion.identity);
// GameManager.Instance.poolManager.ReturnToPool(gameObject);

