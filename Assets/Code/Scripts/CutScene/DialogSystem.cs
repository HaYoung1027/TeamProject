using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogSystem : MonoBehaviour
{
    [Header("대화 말풍선")]
    public GameObject talkPanel;
    [Header("대화 텍스트")]
    public TextMeshProUGUI talkText;

    [Header("텍스트 웨이브")]
    public bool waveText = false;
    [Header("웨이브 세기")]
    public float WaveAmount = 0.01f;
    [Header("웨이브 속도")]
    public float waveSpeed = 15f;

    [Header("미세 흔들림")]
    public bool shakeText = false;
    [Header("흔들림 세기")]
    public float shakeAmount = 0.07f;
    [Header("흔들림 속도")]
    public float shakeSpeed = 15f;

    [Header("폰트 크기 연출")]
    public float sizeUpMultiplier = 1.3f;
    List<bool> bigCharStates = new List<bool>();
    bool isBigMode = false;

    [Header("| 0.1초 숨 고름, ++ 커짐, -- 원래대로")]
    [TextArea]
    public List<string> dialogList = new List<string>();

    int currentDialogIndex = 0;

    [Header("타이핑 속도")]
    public float typingSpeed = 0.05f;
    bool isTyping;

    [HideInInspector]
    public bool isAction;

    Coroutine typingCoroutine;

	void Update()
    {
        if (!isAction) return;

        if (TimelineController.isTimelinePlaying) return;	// 컷씬 재생 중일 때는 스킵 불가
        // 스킵 버튼
        if (Keyboard.current != null &&
            Keyboard.current.qKey.wasPressedThisFrame)
        {
            HandleEnter();
        }
    }
    public void HandleEnter()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            ShowFullText(dialogList[currentDialogIndex]);
        }
        else
        {
            NextDialog();
        }
    }

    void ShowFullText(string text)
    {
        talkText.text = "";
        bigCharStates.Clear();
        isBigMode = false;

        for (int i = 0; i < text.Length; i++)
        {
            if (i + 1 < text.Length && text[i] == '+' && text[i + 1] == '+')
            {
                isBigMode = true;
                i++;
                continue;
            }

            if (i + 1 < text.Length && text[i] == '-' && text[i + 1] == '-')
            {
                isBigMode = false;
                i++;
                continue;
            }

            char c = text[i];
            if (c == '|') continue;

            talkText.text += c;
            bigCharStates.Add(isBigMode);
        }

        isTyping = false;
    }

    void NextDialog()
    {
        currentDialogIndex++;

        if (currentDialogIndex >= dialogList.Count)
        {
            // 대화 종료
            isAction = false;
            talkPanel.SetActive(false);
            StopAllCoroutines();
            return;
        }

        StartDialog();
    }

    public void Action()
    {
        isAction = !isAction;

        if (isAction)
        {
            talkPanel.SetActive(true);
            currentDialogIndex = 0;
            StartDialog();
        }
        else
        {
            StopAllCoroutines();
            talkPanel.SetActive(false);
        }
    }
    void StartDialog()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(dialogList[currentDialogIndex]));
    }


    IEnumerator TypeText(string text)
    {
        isTyping = true;
        talkText.text = "";

        bigCharStates.Clear();
        isBigMode = false;

        StartCoroutine(AnimateText());

        for (int i = 0; i < text.Length; i++)
        {
            // ++
            if (i + 1 < text.Length && text[i] == '+' && text[i + 1] == '+')
            {
                isBigMode = true;
                i++;
                continue;
            }

            // --
            if (i + 1 < text.Length && text[i] == '-' && text[i + 1] == '-')
            {
                isBigMode = false;
                i++;
                continue;
            }

            char c = text[i];

            if (c == '|')
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            talkText.text += c;
            bigCharStates.Add(isBigMode);

            GameManager.Instance.audioManager.TextTypingSound(1f);
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    IEnumerator AnimateText()
    {
        while (isAction)
        {
            talkText.ForceMeshUpdate();
            TMP_TextInfo textInfo = talkText.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int meshIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[meshIndex].vertices;

                Vector3 center =
                    (vertices[vertexIndex + 0] +
                     vertices[vertexIndex + 2]) * 0.5f;

                Vector3 offset = Vector3.zero;

                // 웨이브
                if (waveText)
                {
                    offset.y += Mathf.Sin(Time.time * waveSpeed + i) * WaveAmount;
                }

                // 쉐이크
                if (shakeText)
                {
                    offset.x += (Mathf.PerlinNoise(Time.time * shakeSpeed, i) - 0.5f) * shakeAmount;
                    offset.y += (Mathf.PerlinNoise(i, Time.time * shakeSpeed) - 0.5f) * shakeAmount;
                }

                // 사이즈
                float scale = 1f;
                if (i < bigCharStates.Count && bigCharStates[i])
                    scale = sizeUpMultiplier;

                for (int v = 0; v < 4; v++)
                {
                    Vector3 pos = vertices[vertexIndex + v];
                    pos = (pos - center) * scale + center;
                    pos += offset;
                    vertices[vertexIndex + v] = pos;
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices =
                    textInfo.meshInfo[i].vertices;
                talkText.UpdateGeometry(
                    textInfo.meshInfo[i].mesh, i);
            }

            yield return null;
        }
    }

}
