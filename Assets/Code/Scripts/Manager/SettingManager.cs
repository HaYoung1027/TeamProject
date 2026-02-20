using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingManager : MonoBehaviour
{
    [Header("Global Light 오브젝트")]
    public Light2D globalLight;
    public Slider brightSlider;
    private float bright;
    [Header("사운드 조절")]
    public AudioMixer audioMixer;
    public Slider bgmSlider;
    public Slider sfxSlider;

    private void Start()
    {
        // 슬라이더 값 변경 시 이벤트 등록
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    private void Update()
    {
        if (globalLight == null || brightSlider == null) return;
        globalLight.intensity = brightSlider.value;
    }

    void SetBGMVolume(float value)
    {
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(value) * 20);
    }

    void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
    }
}