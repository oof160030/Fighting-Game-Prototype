using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fighter_Parent : MonoBehaviour
{
    private bool facing_Right; //Indicates which direction the fighter is currently facing.
    private bool on_Right; //Indicates which side the fighter is currently on relative to the opponent.
    private Game_Manager MGR;
    public int PlayerPort; //1 or 2

    private Rigidbody2D RB2;
    private Player_Input PI;

    [SerializeField] private bool OnGround;

    //Movement Parameters
    [SerializeField] float WalkSpeed, JumpForce, Gravity;


    // Start is called before the first frame update
    void Start()
    {

    }

    public void INIT(int P)
    {
        PlayerPort = P;
        MGR = Game_Manager.GetMGR();
        RB2 = GetComponent<Rigidbody2D>();

        PI = new Player_Input(); PI.INIT();
        if (PlayerPort == 1)
        {
            transform.position = new Vector2(-5.25f, -2);
            PI.AssignInputs(KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Z, KeyCode.X, KeyCode.C);
            /*For debug*/ GetComponent<SpriteRenderer>().color = Color.cyan;
        }
        else
        {
            transform.position = new Vector2(5.25f, -2);
            PI.AssignInputs(KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.G, KeyCode.H, KeyCode.J);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Get Inputs
        PI.UpdateButtons();

        //Update position
        //UpdatePosition();

        //Set Movement
        UpdateMovement();
    }

    //called by the fight manager
    public void UpdatePosition(Transform OPP)
    {
        if (on_Right && transform.position.x < OPP.position.x - 0.15f)
            on_Right = false;
        else if (!on_Right && transform.position.x > OPP.position.x + 0.15f)
            on_Right = true;
    }

    private void UpdateMovement()
    {
        float XVel = RB2.velocity.x;
        float YVel = RB2.velocity.y;

        //Set X Input
        if (OnGround)
            XVel = WalkSpeed * PI.HorizInput();

        //Set Y Input (Jump)
        if (OnGround && PI.VertInput() == 1 && PI.VertInputChanged())
            YVel = JumpForce;

        RB2.velocity = new Vector2(XVel, YVel - Gravity * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //(If touching the ground, set to grounded)
        if (collision.gameObject.CompareTag("Ground"))
            OnGround = true;
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        //(If leaving the ground, set to not grounded)
        if (collision.gameObject.CompareTag("Ground"))
            OnGround = false;
    }

    public bool IsOnRight()
    {
        return on_Right;
    }

    public bool IsOnGround()
    {
        return OnGround;
    }
}

class Player_Input
{
    private Button Up, Down, Left, Right, A, B, C;

    public void INIT()
    {
        Up = new Button();
        Down = new Button();
        Left = new Button();
        Right = new Button();
        A = new Button();
        B = new Button();
        C = new Button();
    }
    public void AssignInputs(KeyCode Uk, KeyCode Dk, KeyCode Lk, KeyCode Rk, KeyCode Ak, KeyCode Bk, KeyCode Ck)
    {
        Up.INIT(Uk);
        Down.INIT(Dk);
        Left.INIT(Lk);
        Right.INIT(Rk);
        A.INIT(Ak);
        B.INIT(Bk);
        C.INIT(Ck);
    }

    public void UpdateButtons()
    {
        Up.UpdateState();
        Down.UpdateState();
        Left.UpdateState();
        Right.UpdateState();
        A.UpdateState();
        B.UpdateState();
        C.UpdateState();
    }

    public int HorizInput()
    {
        return 0 + (Left.Pressed() ? -1 : 0) + (Right.Pressed() ? 1 : 0);
    }

    public int VertInput()
    {
        return 0 + (Down.Pressed() ? -1 : 0) + (Up.Pressed() ? 1 : 0);
    }

    public bool VertInputChanged()
    {
        return Up.JustChanged() || Down.JustChanged();
    }
}

class Button
{
    private KeyCode Key;
    private float StateDuration; //Max at 1 second
    private bool IsPressed, StateChanged;

    public void INIT(KeyCode K)
    {
        Key = K;
        StateDuration = 0;
        StateChanged = false;
        IsPressed = false;
    }

    public void UpdateState()
    {
        if((Input.GetKeyDown(Key) || Input.GetKey(Key)) && !IsPressed)
        {
            IsPressed = true;
            StateDuration = 0;
        }
        else if ((Input.GetKeyUp(Key) || !Input.GetKey(Key)) && IsPressed)
        {
            IsPressed = false;
            StateDuration = 0;
        }
        else if (StateDuration < 1)
        {
            StateDuration = Mathf.Clamp(StateDuration + Time.deltaTime, 0, 1);
        }
        StateChanged = (StateDuration == 0);
    }

    public bool Pressed()
    {
        return IsPressed;
    }
    public bool JustPressed()
    {
        return StateChanged && IsPressed;
    }

    public bool JustReleased()
    {
        return StateChanged && !IsPressed;
    }

    public bool JustChanged()
    {
        return StateChanged;
    }

    public float TimePressed()
    {
        return StateDuration;
    }

}
