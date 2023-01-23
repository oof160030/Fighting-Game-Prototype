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

    private float mapEdge, camRange; // Defines the maximum distance the player and camera can move from camera and stage center respectively
    private float contactLimit = 1.5f; // Defines how close fighters can get before being seperated. May have to redefine based on player size.

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
        
        mapEdge = 8.5f; camRange = 4.5f;

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
        //Resolve fighter positions and damage
        UpdatePlayers();

        //Update Camera position - may move to seperate camera script eventually?
        float desiredX = (P1.transform.position.x + P2.transform.position.x) / 2.0f;
        Cam.position = new Vector3(Mathf.Clamp(desiredX, -camRange, camRange), 0, -10);

        //Update the current state the round is in
        UpdateState();
        
    }

    /// <summary>
    /// Manages the fight's state machine. Handles round transitions, keeps track of rounds won, and allows the player's to restart the round.
    /// </summary>
    public void UpdateState()
    {
        //Update Round State
        GeneralTimer.UpdateTimer();
        switch (RState)
        {
            case RoundState.ROUND_START:
                //During round start - switch to the fight state once time elapses.
                if (GeneralTimer.TimeJustExpired)
                {
                    GeneralTimer.SetTimer(1); //Duration of FIGHT text
                    MainText.text = "FIGHT!!!";
                    P1F.SetControlsActive(true); P2F.SetControlsActive(true);
                    RState = RoundState.GAMEPLAY;
                }
                break;
            case RoundState.GAMEPLAY:
                //Hide the Start text after time expires
                if (GeneralTimer.TimeJustExpired)
                    MainText.text = "";
                //End the round once one fighter loses all their health [To Do: Also end the round once time elapses.]
                if (P1F.Health == 0 || P2F.Health == 0)
                {
                    //Deactivate both fighters and set the time controlling KO text duration
                    P1F.SetControlsActive(false); P2F.SetControlsActive(false);
                    GeneralTimer.SetTimer(3);

                    //If both players died, Resolve the Double KO
                    if (P1F.Health == 0 && P2F.Health == 0)
                    {
                        MainText.text = "DOUBLE KO";
                        if (P1Wins == 0) P1Wins++;
                        if (P2Wins == 0) P2Wins++;
                        P1RoundDisp.text = "Rounds: " + P1Wins; P2RoundDisp.text = "Rounds: " + P2Wins;
                    }
                    //If only one player died, resolve a standard KO
                    else
                    {
                        MainText.text = "KO";
                        if (P2F.Health == 0) P1Wins++;
                        else P2Wins++;
                        P1RoundDisp.text = "Rounds: " + P1Wins; P2RoundDisp.text = "Rounds: " + P2Wins;
                    }
                    //Switch to Round End State
                    RState = RoundState.ROUND_END;
                }
                break;
            case RoundState.ROUND_END:
                // After time elapses, check if game is over
                if (GeneralTimer.TimeJustExpired)
                {
                    //If either fighter has two wins, the game ends
                    if (P1Wins == 2 || P2Wins == 2)
                    {
                        MainText.text = "PLAYER " + (P1Wins == 2 ? 1 : 2) + " WINS!\nPress [SPACE] to reset.";
                        RState = RoundState.GAME_END;
                    }
                    //Otherwise, both fighters are reset to start the next round
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
                //Once the game has end, the gameloop can be reset by pressing space
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

    /// <summary>
    /// Updates fighter parameters that must be handled simultaneously, such as resolving damage or seperating the fighters.
    /// </summary>
    private void UpdatePlayers()
    {
        //Prompt each fighter to update their relative position variables, based on the other's position.
        P1F.UpdatePosition(P2.transform); P2F.UpdatePosition(P1.transform);

        //if the fighters stand too close, use the seperation script to move them apart. Do so only if both are on the ground.
        if (P1F.OnGround && P2F.OnGround && Mathf.Abs(P1.transform.position.x - P2.transform.position.x) < contactLimit)
        {
            if (P1F.IsOnRight)
                SeperatePlayers(P2.transform, P1.transform);
            else
                SeperatePlayers(P1.transform, P2.transform);
        }

        //Prompt each fighter to resolve any damage received.
        P1F.UpdateDamage(); P2F.UpdateDamage();

        //CAMERA CONTROLS - may move to seperate camera script eventually?
        //Ensure players stay within the camera bounds
        P1.transform.position = new Vector2(Mathf.Clamp(P1.transform.position.x, Cam.position.x - mapEdge, Cam.position.x + mapEdge), P1.transform.position.y);
        P2.transform.position = new Vector2(Mathf.Clamp(P2.transform.position.x, Cam.position.x - mapEdge, Cam.position.x + mapEdge), P2.transform.position.y);
    }

    /// <summary>
    /// Moves the two fighters apart if they are currently too close to each other.
    /// </summary>
    /// <param name="leftFighter">The fighter that is currently to the right of the other.</param>
    /// <param name="rightFighter">The fighter that is currently to the left of the other.</param>
    private void SeperatePlayers(Transform leftFighter, Transform rightFighter)
    {
        //If left fighter is close to wall, move only the right one
        if (leftFighter.position.x <= -(camRange + mapEdge) + 0.05f)
            rightFighter.position = new Vector2(leftFighter.position.x + contactLimit, rightFighter.position.y);

        //If right fighter is close to wall, move only the left one
        else if (rightFighter.position.x >= (camRange + mapEdge) - 0.05f)
            leftFighter.position = new Vector2(rightFighter.position.x - contactLimit, leftFighter.position.y);

        //Else, move both fighters
        else
        {
            float mid = (leftFighter.position.x + rightFighter.position.x) / 2.0f;
            leftFighter.position = new Vector2(mid - contactLimit/2.0f, leftFighter.position.y);
            rightFighter.position = new Vector2(mid + contactLimit/2.0f, rightFighter.position.y);
        }
    }

    /// <summary>
    /// Checks whether the fighter is within 0.05 Unity units of the camera boundary.
    /// </summary>
    /// <param name="fighterTransform">The transform of the fighter to check.</param>
    /// <returns>A bool, which is true if the transform is close to the map edge.</returns>
    public bool FighterNearEdge(Transform fighterTransform)
    {
        return (Mathf.Abs(Cam.position.x - fighterTransform.position.x) > mapEdge - 0.05f);
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
