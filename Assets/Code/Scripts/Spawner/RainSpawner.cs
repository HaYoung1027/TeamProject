using UnityEngine;
using System.Collections;

public class Rain : MonoBehaviour
{
    public Transform spawnerTransform;
    public GameObject RainDrop;

    public float rainSpawnInterval;
    
    //private static float duration = 0.5f; // 따라다니는 시간 (이펙트 지속시간과 동일)
    //private static float elapsed = 0f;

    // Update is called once per frame
    //void Update()
    //{
    //    elapsed += Time.deltaTime;

    //    if (elapsed > duration)
    //    {
    //        Vector3 pos = new Vector3(Random.Range(-7f, 7f), 0f, 0f);
    //        GameObject RainObj = GameManager.Instance.poolManager.SpawnFromPool("RainDrop", spawnerTransform.position + pos, Quaternion.identity);

    //        elapsed = 0f;
    //    }

    //}

    private void Start()
    {
        StartCoroutine(Rainy());
    }

    private IEnumerator Rainy()
    {
        while(true)
        {
            float sizeX = spawnerTransform.transform.localScale.x * 0.5f;
            Vector3 pos = new Vector3(Random.Range(-sizeX, sizeX), 0f, 0f);
            GameObject RainObj = GameManager.Instance.poolManager.SpawnFromPool("RainDrop", spawnerTransform.position + pos, Quaternion.identity);
            GameManager.Instance.playerStatsRuntime.speed += 0.1f;

            yield return new WaitForSeconds(rainSpawnInterval);
        }    
  
    }
}
