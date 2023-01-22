using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum RoundState { ROUND_START, GAMEPLAY, ROUND_END, GAME_END};
public class FightStats : MonoBehaviour
{
    private Game_Manager GM;
    //Created by the game MGR upon loading into the next scene
    public GameObject playerProto;
    private GameObject P1, P2;
    private Fighter_Parent P1F, P2F;
    public Transform Cam;
    public TextMeshProUGUI Healthp1, Healthp2, P1RoundDisp, P2RoundDisp, MainText;

    private float MapEdge, CamRange; //How far from cam center a player can go & how far from stage center the camera can go
    private float ContactLimit = 1.5f;

    // Round management?
    private int P1Wins, P2Wins, RoundNum;
    private Timer GeneralTimer;
    private RoundState RState;

    // Start is called before the first frame update
    void Start()
    {
        GM = Game_Manager.ThisGMG;
        Cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        P1 = Instantiate(playerProto); P1F = P1.GetComponent<Fighter_Parent>();
        P2 = Instantiate(playerProto); P2F = P2.GetComponent<Fighter_Parent>();
        P1F.INIT(1, P2F, Healthp1); P2F.INIT(2,P1F, Healthp2);
        
        MapEdge = 8.5f; CamRange = 4.5f;

        P1Wins = 0; P2Wins = 0; RoundNum = 1;
        P1RoundDisp.text = "Rounds: 0"; P2RoundDisp.text = "Rounds: 0";

        //Display Round Start, then set timer
        GeneralTimer = new Timer(3);
        RState = RoundState.ROUND_START;
        MainText.text = "ROUND " + RoundNum;
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlayers();

        //Update Camera
        float desiredX = (P1.transform.position.x + P2.transform.position.x) / 2.0f;
        Cam.position = new Vector3(Mathf.Clamp(desiredX, -CamRange, CamRange), 0, -10);

        UpdateState();
        
    }

    public void UpdateState()
    {
        //Update Round State
        GeneralTimer.UpdateTimer();
        switch (RState)
        {
            case RoundState.ROUND_START:
                if (GeneralTimer.TimeJustExpired)
                {
                    GeneralTimer.SetTimer(1); //Duration of FIGHT text
                    MainText.text = "FIGHT!!!";
                    P1F.SetControlsActive(true); P2F.SetControlsActive(true);
                    RState = RoundState.GAMEPLAY;
                }
                break;
            case RoundState.GAMEPLAY:
                if (GeneralTimer.TimeJustExpired)
                    MainText.text = "";
                if (P1F.Health == 0 || P2F.Health == 0)
                {
                    P1F.SetControlsActive(false); P2F.SetControlsActive(false);
                    GeneralTimer.SetTimer(3); //Duration of KO text
                    //If both players died, double KO
                    if (P1F.Health == 0 && P2F.Health == 0)
                    {
                        MainText.text = "DOUBLE KO";
                        if (P1Wins == 0) P1Wins++;
                        if (P2Wins == 0) P2Wins++;
                        P1RoundDisp.text = "Rounds: " + P1Wins; P2RoundDisp.text = "Rounds: " + P2Wins;
                    }
                    //If one player died, display which and update rounds
                    else
                    {
                        MainText.text = "KO";
                        if (P2F.Health == 0) P1Wins++;
                        else P2Wins++;

                        P1RoundDisp.text = "Rounds: " + P1Wins; P2RoundDisp.text = "Rounds: " + P2Wins;
                    }
                    RState = RoundState.ROUND_END;
                }
                break;
            case RoundState.ROUND_END:
                if (GeneralTimer.TimeJustExpired) //At time elapse, check if game is over
                {
                    if (P1Wins == 2 || P2Wins == 2)
                    {
                        MainText.text = "PLAYER " + (P1Wins == 2 ? 1 : 2) + " WINS!\nPress [SPACE] to reset.";
                        RState = RoundState.GAME_END;
                    }
                    else
                    {
                        //Reset Both players
                        P1F.ResetFighter(); P2F.ResetFighter();
                        //Advance round
                        RoundNum++;
                        RState = RoundState.ROUND_START;
                        GeneralTimer = new Timer(3);
                        MainText.text = "ROUND " + RoundNum;
                    }
                }
                break;
            case RoundState.GAME_END:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    //Reset Both players
                    P1F.ResetFighter(); P2F.ResetFighter();
                    //Reset Round counts
                    RoundNum = 1; P1Wins = 0; P2Wins = 0;
                    P1RoundDisp.text = "Rounds: " + P1Wins; P2RoundDisp.text = "Rounds: " + P2Wins;
                    
                    RState = RoundState.ROUND_START;
                    GeneralTimer = new Timer(3);
                    MainText.text = "ROUND " + RoundNum;
                }
                break;
        }
    }

    private void UpdatePlayers()
    {
        //Check the fighter's relative positions
        P1F.UpdatePosition(P2.transform); P2F.UpdatePosition(P1.transform);

        //if the fighters are close, seperate them
        if (P1F.OnGround && P2F.OnGround && Mathf.Abs(P1.transform.position.x - P2.transform.position.x) < ContactLimit)
        {
            if (P1F.IsOnRight)
                SeperatePlayers(P2.transform, P1.transform);
            else
                SeperatePlayers(P1.transform, P2.transform);
        }

        //Tell each fighter to check if they got hit
        P1F.UpdateDamage(); P2F.UpdateDamage();

        //CAMERA CONTROLS
        //Lock players within camera bounds
        P1.transform.position = new Vector2(Mathf.Clamp(P1.transform.position.x, Cam.position.x - MapEdge, Cam.position.x + MapEdge), P1.transform.position.y);
        P2.transform.position = new Vector2(Mathf.Clamp(P2.transform.position.x, Cam.position.x - MapEdge, Cam.position.x + MapEdge), P2.transform.position.y);
    }

    //Called if the fighters are too close - moves them to their minimum allowed distance based on relative position
    private void SeperatePlayers(Transform left, Transform right)
    {
        //If left fighter is close to wall, move only the right one
        if (left.position.x <= -(CamRange + MapEdge) + 0.05f)
            right.position = new Vector2(left.position.x + ContactLimit, right.position.y);

        //If right fighter is close to wall, move only the left one
        else if (right.position.x >= (CamRange + MapEdge) - 0.05f)
            left.position = new Vector2(right.position.x - ContactLimit, left.position.y);

        //Else, move both fighters
        else
        {
            float mid = (left.position.x + right.position.x) / 2.0f;
            left.position = new Vector2(mid - ContactLimit/2.0f, left.position.y);
            right.position = new Vector2(mid + ContactLimit/2.0f, right.position.y);
        }
    }

    public bool FighterNearEdge(Transform T)
    {
        return (Mathf.Abs(Cam.position.x - T.position.x) > MapEdge - 0.05f);
    }
}

/// <summary>
/// A general use timer. Once set, it runs for that amount of seconds and indicates that it expired with a bool.
/// </summary>
public class Timer
{
    private float currentTime; public float CurrentTime { get { return currentTime; } }
    private bool timeJustExpired; public bool TimeJustExpired { get { return timeJustExpired; } }

    public Timer()
    {
        currentTime = 0;
        timeJustExpired = false;
    }

    public Timer(float X)
    {
        currentTime = X;
        timeJustExpired = false;
    }

    /// <summary>
    /// Updates the timer's current value. Also sets if the timer just expired.
    /// </summary>
    public void UpdateTimer()
    {
        if (currentTime == 0 && timeJustExpired)
            timeJustExpired = false;

        else if(currentTime > 0)
        {
            currentTime = Mathf.Clamp(currentTime - Time.deltaTime, 0, 200);
            if (currentTime == 0)
                timeJustExpired = true;
        }
    }

    /// <summary>
    /// Sets the timer to run for a certain amount of time.
    /// </summary>
    /// <param name="time">The time, in seconds, the timer should run for.</param>
    public void SetTimer(float time)
    {
        currentTime = time;
        timeJustExpired = false;
    }

}
