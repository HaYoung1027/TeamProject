using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("AudioMixer")]
    public AudioMixer audioMixer;

    [Header("AudioSource")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Player SFX")]
    public AudioClip playerDamaged;
    public AudioClip playerJump;
    public AudioClip playerRun;

    [Header("Enemy SFX")]
    public AudioClip enemyAttack;
    public AudioClip enemyShoot;

    [Header("Enemy SFX")]
    public AudioClip objectExplosion;

    [Header("Hook SFX")]
    public AudioClip hookAttach;
    public AudioClip hookDraft;
    public AudioClip hookShoot;
    public AudioClip hookThrowEnemy;

    [Header("Dialog SFX")]
    public AudioClip textTyping;

    const string BGM_KEY = "BGM_VOLUME";
    const string SFX_KEY = "SFX_VOLUME";

    void Awake()
    {
        LoadVolume();
    }

    /* ---------- Volume ---------- */

    public void SetBGMVolume(float value)
    {
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(BGM_KEY, value);
    }

    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(SFX_KEY, value);
    }

    void LoadVolume()
    {
        float bgm = PlayerPrefs.GetFloat(BGM_KEY, 1f);
        float sfx = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        SetBGMVolume(bgm);
        SetSFXVolume(sfx);
    }


    // 배경음 재생
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    // 배경음 정지
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    // 효과음 재생
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip, volume);
    }
    public void StopSFX()
    {
        if (sfxSource == null) return;

        sfxSource.Stop();
    }
    // 편의 함수

    // 플레이어
    public void PlayDamagedSound(float volume = 1f) => PlaySFX(playerDamaged, volume);
    public void PlayJumpSound(float volume = 1f) => PlaySFX(playerJump, volume);
    public void PlayRunSound(float volume = 1f)
    {
        if (sfxSource == null || playerRun == null) return;

        // 이미 발소리 재생 중이면 무시
        if (sfxSource.isPlaying && sfxSource.clip == playerRun)
            return;

        sfxSource.clip = playerRun;
        sfxSource.loop = true;
        sfxSource.volume = volume;
        sfxSource.Play();
    }

    public void StopRunSound() // 전체 소리 멈춤 버그
    {
        if (sfxSource == null) return;

        if (sfxSource.isPlaying && sfxSource.clip == playerRun)
            sfxSource.Stop();
    }

    // 적
    public void EnemyAttackSound(float volume = 1f) => PlaySFX(enemyAttack, volume);
    public void EnemyShootSound(float volume = 1f) => PlaySFX(enemyShoot, volume);

    // 오브젝트
    public void ObjectExplosionSound(float volume = 1f) => PlaySFX(objectExplosion, volume);

    // 갈고리
    public void HookAttachSound(float volume = 1f) => PlaySFX(hookAttach, volume);
    public void HookDraftSound(float volume = 1f) => PlaySFX(hookDraft, volume);
    public void HookShootSound(float volume = 1f) => PlaySFX(hookShoot, volume);
    public void HookThrowEnemySound(float volume = 1f) => PlaySFX(hookThrowEnemy, volume);

    // 다이어로그
    public void TextTypingSound(float volume = 1f) => PlaySFX(textTyping, volume);
}