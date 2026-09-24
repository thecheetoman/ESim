using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Global event broadcast whenever the robot state toggles
    public static event Action<bool> OnRobotStateChanged;

    // Current robot state property
    public static bool IsRobotEnabled { get; private set; } = false;

    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Audio system
    [Header("Audio System")]
    public AudioSource fieldSFX;
    [Tooltip("0: Match Start | 1: Auto End | 2: Teleop Start | 3: Shift Change / Alert | 4: Endgame / Shift | 5: Match End")]
    [SerializeField] private List<AudioClip> soundEffects = new();

    [Header("Season Match Timing")]
    public bool isBlueAlliance = true;
    [SerializeField] private float autoDuration = 20f;
    [SerializeField] private float teleopTransitionDuration = 10f;
    [SerializeField] private float shiftDuration = 25f; // 4 shifts @ 25s = 100s total
    [SerializeField] private float endgameDuration = 30f;

    private bool gameStarted = false;
    private bool isMatchOver = false;
    private bool isCountingDown = false;

    // Scores
    private int scoreBlue = 0;
    private int scoreRed = 0;
    private int autoScoreBlue = 0;
    private int autoScoreRed = 0;

    private bool isAutoPhase = false;

    // Hub active state tracking
    private bool isBlueHubActive = true;
    private bool isRedHubActive = true;

    [Header("UI Elements")]
    public TextMeshProUGUI blueTMPObject;
    public TextMeshProUGUI redTMPObject;
    public GameObject blueShift;
    public GameObject redShift;
    public TextMeshProUGUI timer;

    [Header("Hub Material Switchers")]
    public HubSystem RedHub;
    public HubSystem BlueHub;

    private Coroutine matchRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Start match with robot disabled and active hub indicators hidden
        SetRobotEnabled(false);
        SetHubStates(false, false);

        if (blueShift != null) blueShift.SetActive(false);
        if (redShift != null) redShift.SetActive(false);

        UpdateScoreUI();

        float totalMatchLength = autoDuration + teleopTransitionDuration + (shiftDuration * 4) + endgameDuration;
        UpdateTimerUI(totalMatchLength);
    }

    private void Update()
    {
        // Toggle match start or manual enable/disable using Right Shift
        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            if (!gameStarted && !isMatchOver)
            {
                startGame();
            }
            else if (gameStarted && !isCountingDown)
            {
                // Manual override: toggle enable state during match (blocked during countdown)
                SetRobotEnabled(!IsRobotEnabled);
            }
            else if (isMatchOver)
            {
                // Manual override: toggle enable state after match
                SetRobotEnabled(!IsRobotEnabled);
            }
        }
        if(Input.GetKeyDown(KeyCode.R))
        {
            // restart to main menu
            SceneManager.LoadScene("Field");
        }
        //DE#BUGGING R#EMOV WHEN BUOLD
        if(Input.GetKeyDown(KeyCode.T))
        {
            Debug.developerConsoleVisible = !Debug.developerConsoleVisible;
        }
    }

    public void SetRobotEnabled(bool enabled)
    {
        // Prevent enabling if match countdown is currently running
        if (isCountingDown && enabled)
        {
            enabled = false;
        }

        IsRobotEnabled = enabled;
        OnRobotStateChanged?.Invoke(IsRobotEnabled);
        Debug.Log(IsRobotEnabled ? "enabled" : "disabled");
    }

    public void startGame()
    {
        if (gameStarted) return;

        gameStarted = true;
        isMatchOver = false;

        matchRoutine = StartCoroutine(MatchSequence());
    }

    private IEnumerator MatchSequence()
    {
        // COUNTDOWN
        isCountingDown = true;
        SetRobotEnabled(false);
        for (int i = 3; i > 0; i--)
        {
            Debug.Log(i.ToString());
            yield return new WaitForSeconds(1f);
        }
        isCountingDown = false;

        // PHASE 1: AUTONOMOUS (20s)
        Debug.Log("auto");
        isAutoPhase = true;
        PlaySound(0); // Match start sfx

        SetHubStates(true, true);
        SetRobotEnabled(true); // Enable robot for Auto

        yield return RunTimerSegment(autoDuration, 140f);

        // PHASE 2: AUTO END (DISABLE)
        Debug.Log("field reset");
        isAutoPhase = false;
        SetRobotEnabled(false); // Disable robot during transition
        SetHubStates(false, false);
        PlaySound(1); // Auto end sfx

        yield return new WaitForSeconds(2.0f);

        // DETERMINE AUTO WINNER
        bool blueWonAuto = autoScoreBlue > autoScoreRed;
        if (autoScoreBlue == autoScoreRed)
        {
            blueWonAuto = isBlueAlliance;
        }

        // PHASE 3: TELEOP TRANSITION SHIFT 1/6 (10s)
        SetHubStates(true, true);
        Debug.Log("teleop ");
        PlaySound(2); // Teleop start sfx
        SetRobotEnabled(true); // Enable robot for Teleop

        yield return RunTimerSegment(teleopTransitionDuration, 130f);

        // SHIFT 1 / 2/6 (25s)
        Debug.Log(" shift 1 ");
        PlaySound(3);
        SetHubStates(!blueWonAuto, blueWonAuto);
        yield return RunTimerSegment(shiftDuration, 105f);

        // SHIFT 2 / 3/6 (25s)
        Debug.Log(" shift 2 ");
        PlaySound(3);
        SetHubStates(blueWonAuto, !blueWonAuto);
        yield return RunTimerSegment(shiftDuration, 80f);

        // SHIFT 3 / 4/6 (25s)
        Debug.Log(" shift 3 ");
        PlaySound(3);
        SetHubStates(!blueWonAuto, blueWonAuto);
        yield return RunTimerSegment(shiftDuration, 55f);

        // SHIFT 4 / 5/6 (25s)
        Debug.Log(" shift 4 ");
        PlaySound(3);
        SetHubStates(blueWonAuto, !blueWonAuto);
        yield return RunTimerSegment(shiftDuration, 30f);

        // ENDGAME / 6/6 (30s)
        Debug.Log(" engdame ");
        PlaySound(4);
        SetHubStates(true, true);
        yield return RunTimerSegment(endgameDuration, 0f);

        // MATCH END
        EndMatch();
    }

    private IEnumerator RunTimerSegment(float segmentDuration, float baseRemainingTime)
    {
        float timer = segmentDuration;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            UpdateTimerUI(timer + baseRemainingTime);
            yield return null;
        }
    }

    private void SetHubStates(bool blueActive, bool redActive)
    {
        isBlueHubActive = blueActive;
        isRedHubActive = redActive;

        if (BlueHub != null) BlueHub.SetActive(isBlueHubActive);
        if (RedHub != null) RedHub.SetActive(isRedHubActive);

        if (blueShift != null) blueShift.SetActive(isBlueHubActive);
        if (redShift != null) redShift.SetActive(isRedHubActive);
    }

    private void EndMatch()
    {
        Debug.Log("wraps");

        // Disable robot at match end
        SetRobotEnabled(false);
        SetHubStates(false, false);

        isMatchOver = true;
        gameStarted = false;

        PlaySound(5); // Match end sfx
        UpdateTimerUI(0f);

        // Wait 3 seconds, then re-enable for post-match
        StartCoroutine(PostMatchEnableRoutine());
    }

    private IEnumerator PostMatchEnableRoutine()
    {
        yield return new WaitForSeconds(3.0f);

        SetRobotEnabled(true);
        Debug.Log("reneabled");
    }

    public void ScorePoint(bool isBlueHub, bool wasShotFromLegalZone)
    {
        if (!gameStarted || isMatchOver) return;

        if (isBlueHub)
        {
            if (!isBlueHubActive) return;

            bool isPenalty = !wasShotFromLegalZone;

            if (isPenalty)
            {
                scoreRed += 10;
                scoreBlue += 1;
                if (isAutoPhase)
                {
                    autoScoreRed += 10;
                    autoScoreBlue += 1;
                }
            }
            else
            {
                scoreBlue += 1;
                if (isAutoPhase) autoScoreBlue += 1;
            }
        }
        else
        {
            if (!isRedHubActive) return;

            scoreRed += 6;
            if (isAutoPhase) autoScoreRed += 6;
        }

        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (blueTMPObject != null) blueTMPObject.text = scoreBlue.ToString();
        if (redTMPObject != null) redTMPObject.text = scoreRed.ToString();
    }

    private void UpdateTimerUI(float timeDisplay)
    {
        if (timer == null) return;

        int minutes = Mathf.FloorToInt(timeDisplay / 60F);
        int seconds = Mathf.FloorToInt(timeDisplay % 60F);
        timer.text = string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    private void PlaySound(int index)
    {
        if (fieldSFX != null && soundEffects != null && index < soundEffects.Count && soundEffects[index] != null)
        {
            fieldSFX.PlayOneShot(soundEffects[index]);
        }
    }
}