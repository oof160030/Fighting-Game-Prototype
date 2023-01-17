using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game_Manager : MonoBehaviour
{
    static Game_Manager GMG;
    bool inGameplay = false;
    FightStats FightMGR;

    public GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        if (Game_Manager.GMG == null)
        {
            Game_Manager.GMG = this;
            Object.DontDestroyOnLoad(this);
        }
        else
            Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space) && !inGameplay)
        {
            SceneManager.LoadScene("Battle Scene");
            inGameplay = true;
        }
    }

    static public Game_Manager GetMGR() { return GMG; }
}
