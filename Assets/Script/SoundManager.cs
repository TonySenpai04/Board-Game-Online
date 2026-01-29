using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip xoClickClip;
    [SerializeField] private AudioClip chessMoveClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [SerializeField] private AudioClip drawClip;

    [Header("UI Buttons")]
    [SerializeField] private List<Button> buttonsToHook = new List<Button>();

    [Header("Toggle Sound")]
    [SerializeField] private Image soundToggleIcon;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    private bool isSoundOn = true;
    private const string SoundPrefKey = "IsSoundOn";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Load sound setting
        isSoundOn = PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;
    }

    private void Start()
    {
        UpdateSoundIcon();
        foreach (var button in buttonsToHook)
        {
            if (button != null)
            {
                button.onClick.AddListener(PlayButtonClick);
            }
        }
    }

    public void PlayButtonClick()
    {
        PlaySound(buttonClickClip);
    }

    public void PlayXOClick()
    {
        PlaySound(xoClickClip);
    }

    public void PlayChessMove()
    {
        PlaySound(chessMoveClip);
    }

    public void PlayWin()
    {
        PlaySound(winClip);
    }

    public void PlayLose()
    {
        PlaySound(loseClip);
    }

    public void PlayDraw()
    {
        PlaySound(drawClip);
    }

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        PlayerPrefs.SetInt(SoundPrefKey, isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
        UpdateSoundIcon();
    }

    private void UpdateSoundIcon()
    {
        if (soundToggleIcon != null)
        {
            soundToggleIcon.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (!isSoundOn) return;

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
