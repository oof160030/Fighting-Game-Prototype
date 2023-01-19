using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightStats : MonoBehaviour
{
    //Created by the game MGR upon loading into the next scene
    public GameObject playerProto;
    private GameObject P1, P2;
    private Fighter_Parent P1F, P2F;
    public Transform Cam;

    private float MapEdge, CamRange; //How far from cam center a player can go & how far from stage center the camera can go
    private float ContactLimit = 1.5f;

    // Start is called before the first frame update
    void Start()
    {
        Cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        P1 = Instantiate(playerProto); P1F = P1.GetComponent<Fighter_Parent>();
        P2 = Instantiate(playerProto); P2F = P2.GetComponent<Fighter_Parent>();
        P1F.INIT(1, P2F); P2F.INIT(2,P1F);
        MapEdge = 8.5f; CamRange = 4.5f;
    }

    // Update is called once per frame
    void Update()
    {
        //Check the fighter's relative positions
        P1F.UpdatePosition(P2.transform); P2F.UpdatePosition(P1.transform);

        //if the fighters are close, seperate them
        if (P1F.IsOnGround() && P2F.IsOnGround() && Mathf.Abs(P1.transform.position.x - P2.transform.position.x) < ContactLimit)
        {
            if(P1F.IsOnRight())
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

        //Reposition Camera as needed to follow fighters
        float desiredX = (P1.transform.position.x + P2.transform.position.x) / 2.0f;
        Cam.position = new Vector3(Mathf.Clamp(desiredX, -CamRange, CamRange), 0, -10);
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
