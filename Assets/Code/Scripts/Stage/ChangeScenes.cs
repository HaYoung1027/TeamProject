using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScenes : MonoBehaviour
{
    [Header("Fade Canvas (CanvasGroup)")]
    public CanvasGroup fadeCanvas;

    public float fadeDuration = 0.8f;

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player detected!");
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene("1_MainMenu");
    }

}
