using UnityEngine;
using UnityEngine.SceneManagement;
using static Globals;

public class ChangeScenes : MonoBehaviour
{
    [Header("Fade Canvas (CanvasGroup)")]
    public CanvasGroup fadeCanvas;

    public float fadeDuration = 0.8f;

	// private bool isTransitioning = false;

	private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(TagName.player))
        {
            Debug.Log("Player detected!");
            new SceneChanger().LoadNextScene();
        }
    }
}
