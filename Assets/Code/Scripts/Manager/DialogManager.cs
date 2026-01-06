using TMPro;
using UnityEngine;
using System.Collections;

public class DialogManager : MonoBehaviour
{
    [Header("대화 말풍선")]
    public GameObject talkPanel;
    [Header("대화 텍스트")]
    public TextMeshProUGUI talkText;

    [TextArea]
    public string dialogText;

    public float typingSpeed = 0.05f;
    bool isTyping;

    [HideInInspector]
    public bool isAction;

    Coroutine typingCoroutine;

    public void Action()
    {
        isAction = !isAction;

        if (isAction)
        {
            talkPanel.SetActive(true);

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText());
        }
        else
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            talkPanel.SetActive(false);
        }
    }

    IEnumerator TypeText()
    {
        isTyping = true;
        talkText.text = "";

        foreach (char c in dialogText)
        {
            talkText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }
}
