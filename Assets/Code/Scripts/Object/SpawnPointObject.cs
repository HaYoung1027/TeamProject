using UnityEngine;
using tagName = Globals.TagName;

public class SpawnPointObject : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(tagName.player))
        {
            GameManager.Instance.SetSpawnPoint(transform.position);
        }
    }
}