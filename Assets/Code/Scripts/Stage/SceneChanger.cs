using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// TODO: 임시 클래스 (씬 이동 관련)
public class SceneChanger : MonoBehaviour
{
	private void Update()
	{
		if (Keyboard.current.f5Key.wasPressedThisFrame)     // F5키 입력 시 씬 리로드
			GameManager.Instance.sceneReloader.Reload();
		else if (Keyboard.current.f7Key.wasPressedThisFrame)    // F7키 입력 시 이전 씬으로 넘어감
			LoadPrevScene();
		else if (Keyboard.current.f8Key.wasPressedThisFrame)    // F8키 입력 시 다음 씬으로 넘어감
			LoadNextScene();
	}

	public void LoadNextScene()    // 다음씬 로드
	{
		if (SceneManager.GetActiveScene().buildIndex < 6)
		{
			int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
			SceneManager.LoadScene(nextIndex);
		}
	}

	public void LoadPrevScene()    // 이전씬 로드
	{
		if (SceneManager.GetActiveScene().buildIndex >= 0)
		{
			int prevIndex = SceneManager.GetActiveScene().buildIndex - 1;
			SceneManager.LoadScene(prevIndex);
		}
	}
}
