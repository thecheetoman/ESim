using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // audio system
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

    // scores
    private int scoreBlue = 0;
    private int scoreRed = 0;
    private int autoScoreBlue = 0;
    private int autoScoreRed = 0;

    private bool isAutoPhase = false;

    // hub active state tracking
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

    private void Start()
    {
        // Start match with active hub arrow indicators hidden
        SetHubStates(false, false);

        if (blueShift != null) blueShift.SetActive(false);
        if (redShift != null) redShift.SetActive(false);

        UpdateScoreUI();

        float totalMatchLength = autoDuration + teleopTransitionDuration + (shiftDuration * 4) + endgameDuration;
        Debug.Log("Total Match Length: " + totalMatchLength);
        UpdateTimerUI(totalMatchLength);
    }

    void Update()
    {
        // Toggle match start using Right Shift
        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            if (!gameStarted && !isMatchOver)
            {
                startGame();
            }
        }
    }

    public void startGame()
    {
        if (gameStarted) return;

        gameStarted = true;
        isMatchOver = false;

        // start the full match sequence coroutine
        matchRoutine = StartCoroutine(MatchSequence());
    }

    private IEnumerator MatchSequence()
    {
        // COUNTDOWN 
        for (int i = 3; i > 0; i--)
        {
            Debug.Log(i.ToString());
            yield return new WaitForSeconds(1f);
        }

        // PHASE 1: AUTONOMOUS 
        Debug.Log(" STARTING AUTONOMOUS (20s) ");
        isAutoPhase = true;
        PlaySound(0); // match start sfx

        SetHubStates(true, true);

        yield return RunTimerSegment(autoDuration, 140f);

        // PHASE 2: AUTO END 
        Debug.Log(" END OF AUTO ");
        isAutoPhase = false;
        SetHubStates(false, false);
        PlaySound(1); // auto end sfx

        yield return new WaitForSeconds(2.0f);

        // DETERMINE AUTO WINNER 
        bool blueWonAuto = autoScoreBlue > autoScoreRed;

        // If tied, default winner behavior 
        if (autoScoreBlue == autoScoreRed)
        {
            blueWonAuto = isBlueAlliance;
        }

        // PHASE 3: TELEOP TRANSITION SHIFT (10s) 
        SetHubStates(true, true);
        Debug.Log(" STARTING TELEOP: TRANSITION SHIFT 1/6 (10s) ");
        PlaySound(2); // Teleop start sfx

        yield return RunTimerSegment(teleopTransitionDuration, 130f);

        // SHIFT 1 / ShiftName 2/6 (25s) 
        Debug.Log(" SHIFT 1 (2/6) ");
        PlaySound(3); // shift sfx
        SetHubStates(!blueWonAuto, blueWonAuto); // Auto winner is INACTIVE
        yield return RunTimerSegment(shiftDuration, 105f);

        // SHIFT 2 / ShiftName 3/6 (25s) 
        Debug.Log(" SHIFT 2 (3/6) ");
        PlaySound(3); // shift sfx
        SetHubStates(blueWonAuto, !blueWonAuto); // Auto winner is ACTIVE
        yield return RunTimerSegment(shiftDuration, 80f);

        // SHIFT 3 / ShiftName 4/6 (25s) 
        Debug.Log(" SHIFT 3 (4/6) ");
        PlaySound(3); // shift sfx
        SetHubStates(!blueWonAuto, blueWonAuto); // Auto winner is INACTIVE
        yield return RunTimerSegment(shiftDuration, 55f);

        // SHIFT 4 / ShiftName 5/6 (25s) 
        Debug.Log(" SHIFT 4 (5/6) ");
        PlaySound(3); // shift sfx
        SetHubStates(blueWonAuto, !blueWonAuto); // Auto winner is ACTIVE
        yield return RunTimerSegment(shiftDuration, 30f);

        // ENDGAME / ShiftName 6/6 (30s) 
        Debug.Log(" ENDGAME (6/6) ");
        PlaySound(4); // shift sfx
        SetHubStates(true, true); // both hubs active in endgame
        yield return RunTimerSegment(endgameDuration, 0f);

        // PHASE 4: MATCH END 
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

        // Direct, state-driven material updates
        if (BlueHub != null) BlueHub.SetActive(isBlueHubActive);
        if (RedHub != null) RedHub.SetActive(isRedHubActive);

        // Update UI indicator objects
        if (blueShift != null) blueShift.SetActive(isBlueHubActive);
        if (redShift != null) redShift.SetActive(isRedHubActive);
    }

    private void EndMatch()
    {
        Debug.Log(" MATCH FINISHED ");
        SetHubStates(false, false);
        isMatchOver = true;
        gameStarted = false;

        PlaySound(5); // match end sfx

        UpdateTimerUI(0f);
    }

    // Handle scoring with legal shot validation passed from HubFuelCounter
    public void ScorePoint(bool isBlueHub, bool wasShotFromLegalZone)
    {
        if (!gameStarted || isMatchOver) return;

        if (isBlueHub)
        {
            if (!isBlueHubActive) return; // Ignore score if Blue Hub is inactive

            // Check penalty ONLY for Blue Hub shots
            bool isPenalty = !wasShotFromLegalZone;

            if (isPenalty)
            {
                // Penalty: Award 10 points to Red, 1 to Blue
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
                // Normal Blue score
                scoreBlue += 1;
                if (isAutoPhase) autoScoreBlue += 1;
            }
        }
        else
        {
            if (!isRedHubActive) return; // Ignore score if Red Hub is inactive

            // Red Hub scores normally without checking legal zone
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