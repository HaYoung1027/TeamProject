using UnityEngine;

public class LayerSetting : MonoBehaviour
{
    SpriteRenderer sr;
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            sr.color = new Color(1, 1, 1, 0.5f);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            sr.color = new Color(1, 1, 1, 1f);
    }
}
