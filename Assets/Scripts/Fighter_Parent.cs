using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FighterState { IDLE, ATTACK, BLOCK, FLINCH, AIRFLINCH, CRUMPLE, TUMBLE, PRONE, ANIMATED};
public class Fighter_Parent : MonoBehaviour
{
    private Game_Manager MGR;
    
    private FighterState State;
    private bool facing_Right; //Indicates which direction the fighter is currently facing.
    private bool on_Right; //Indicates which side the fighter is currently on relative to the opponent.
    [SerializeField] private bool OnGround;

    public int PlayerPort; //1 or 2
    private Fighter_Parent OtherPlayer;
    private FightStats FMGR;

    private Rigidbody2D RB2;
    private SpriteRenderer SR;
    private AnimControl ANIMCTRL;
    private Player_Input PI;
    public GameObject HBox_Prefab;

    //Character specific fields (serializable)
    [SerializeField] SO_HitboxList MoveList;
    private EnemyHitbox_Data Enemy_HB;

    //Movement Parameters
    [SerializeField] float WalkSpeed, JumpForce, Gravity;
    public float AnimHoriz, AnimVert; //Set by animator - determines movement forwards or upwards.

    public void INIT(int P, Fighter_Parent O)
    {
        PlayerPort = P;
        OtherPlayer = O;
        FMGR = GameObject.FindGameObjectWithTag("FightMGR").GetComponent<FightStats>();
        MGR = Game_Manager.GetMGR();
        RB2 = GetComponent<Rigidbody2D>();
        SR = GetComponent<SpriteRenderer>();
        ANIMCTRL = new AnimControl();
        ANIMCTRL.Init(GetComponent<Animator>());
        State = FighterState.IDLE;

        Enemy_HB = new EnemyHitbox_Data();

        PI = new Player_Input(); PI.INIT();
        if (PlayerPort == 1)
        {
            transform.position = new Vector2(-5.25f, -4);
            PI.AssignInputs(KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Z, KeyCode.X, KeyCode.C);
            /*For debug*/ SR.color = Color.cyan;
        }
        else
        {
            transform.position = new Vector2(5.25f, -4);
            PI.AssignInputs(KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.G, KeyCode.H, KeyCode.J);
        }

    }

    // Update is called once per frame
    void Update()
    {
        //Get Inputs
        PI.UpdateButtons();

        if(OnGround)
            UpdateFacing();

        ANIMCTRL.UpdateTimers();
        if(State != FighterState.IDLE)
        {
            UpdateRecovery();
        }
        
        //Check if an attack is being used
        UpdateAttack();
        
        //Set Movement
        UpdateMovement();
    }

    //Knock prone
    public void LandProne()
    {
        State = FighterState.PRONE;
        ANIMCTRL.SetRecoveryTrigger(20);
    }

    #region //Update methods [UpdatePosition; UpdateFacing; UpdateMovement; UpdateAttack]
    //called by the fight manager
    public void UpdatePosition(Transform OPP)
    {
        if (on_Right && transform.position.x < OPP.position.x - 0.15f)
            on_Right = false;
        else if (!on_Right && transform.position.x > OPP.position.x + 0.15f)
            on_Right = true;
    }

    public void UpdateFacing()
    {
        facing_Right = !on_Right;
        SR.flipX = !facing_Right;
    }

    private void UpdateMovement()
    {

        float XVel = 0;
        float YVel = RB2.velocity.y;
        if (State == FighterState.IDLE)
        {
            XVel = RB2.velocity.x;
            //Set X Input
            if (OnGround)
                XVel = WalkSpeed * PI.HorizInput();

            //Set Y Input (Jump)
            if (OnGround && PI.VertInput() == 1 && PI.VertInputChanged())
                YVel = JumpForce;
        }
        //If fighter is being hurt on the ground, reduce horizontal speed through acceleration
        else if(State == FighterState.FLINCH || State == FighterState.CRUMPLE || State == FighterState.ATTACK)
        {
            XVel = Mathf.Clamp((Mathf.Abs(RB2.velocity.x) - 3.9f * Time.deltaTime),0,1200) * Mathf.Sign(RB2.velocity.x);
        }
        //If fighter is being hurt in the air, maintain horizontal speed
        else if (State == FighterState.AIRFLINCH || State == FighterState.TUMBLE)
        {
            XVel = RB2.velocity.x;
        }
        RB2.velocity = new Vector2(XVel, YVel - Gravity * Time.deltaTime);

    }

    private void UpdateAttack()
    {
        //If a valid attack button is pressed, tell the animator an attack has started (with an Anim trigger)
        if(PI.A.JustPressed() && State == FighterState.IDLE) //Update later to include input buffer
        {
            ANIMCTRL.SetTrigger("A");
        }
        else if(PI.B.JustPressed() && State == FighterState.IDLE) //Update later to include input buffer
        {
            ANIMCTRL.SetTrigger("B");
        }
        //(The animator will change state, spawn hitboxes or move the player until damage is received or the move ends)
    }

    private void UpdateRecovery()
    {
        //If in an air damage state, return to neutral or land prone once you touch the ground
        if (State == FighterState.AIRFLINCH && OnGround)
            State = FighterState.IDLE;
        else if (State == FighterState.TUMBLE && OnGround)
            LandProne();
        //If in a ground damage state, return to neutral or fall prone once hitstun elapses
        else if (ANIMCTRL.UpdateRecoveryTimer()) //If this returns true, the fighter has just recovered from hitstun
        {
            if (State == FighterState.CRUMPLE)
                LandProne();
            else
                State = FighterState.IDLE;
        }
    }
    #endregion

    #region //Player Damage Methods [PlayerHit; UpdateDamage]
    public void PlayerHit(SO_Hitbox data, bool R, int port)
    {
        Enemy_HB.LastHitbox = data;
        Enemy_HB.FacingRight = R;
    }

    public void UpdateDamage()
    {
        if(Enemy_HB.LastHitbox != null)
        {
            //In any case, apply hitstop
            
            //If you were blocking (holding back on the ground in idle state), enter a block state
            if(State == FighterState.IDLE && OnGround && PI.HoldingBack(facing_Right))
            {
                State = FighterState.BLOCK;
                //Tell animator you blocked
                ANIMCTRL.SetTrigger("Block");
                //switch to idle state after time limit
                ANIMCTRL.SetRecoveryTrigger(Enemy_HB.LastHitbox.blockStun);
            }
            //If you were otherwise invulnerable, play an effect [IGNORE FOR NOW]
            //If hit hard on the ground, fall to the floor
            else if(!OnGround)
            {
                if (Enemy_HB.HasProperty(Properties.CRUMPLE))
                {
                    State = FighterState.TUMBLE;
                    //Tell animator you got hit
                    ANIMCTRL.SetTrigger("Crumple");
                }
                else
                {
                    State = FighterState.AIRFLINCH;
                    //Tell animator you got hit
                    ANIMCTRL.SetTrigger("Hurt");
                    //Set knockback based on ground state
                    RB2.velocity = Enemy_HB.LastHitbox.knockback;
                }
                //Set knockback
                RB2.velocity = SetKnockback(true, Enemy_HB.FacingRight, Enemy_HB.LastHitbox.knockback);
            }
            else
            {
                if(Enemy_HB.HasProperty(Properties.CRUMPLE))
                {
                    State = FighterState.CRUMPLE;
                    //Tell animator you got hit
                    ANIMCTRL.SetTrigger("Crumple");
                    //switch to idle state after time limit
                    ANIMCTRL.SetRecoveryTrigger(Enemy_HB.LastHitbox.hitStun);
                }
                //If launcher property, use the air crumple state instead
                else
                {
                    State = FighterState.FLINCH;
                    //Tell animator you got hit
                    ANIMCTRL.SetTrigger("Hurt");
                    //switch to idle state after time limit
                    ANIMCTRL.SetRecoveryTrigger(Enemy_HB.LastHitbox.hitStun);
                }
                //Set knockback
                if (FMGR.FighterNearEdge(transform))
                    OtherPlayer.SetPushback(Enemy_HB.FacingRight, Enemy_HB.LastHitbox.knockback);
                else
                    RB2.velocity = SetKnockback(false, Enemy_HB.FacingRight, Enemy_HB.LastHitbox.knockback);
                //BUT, use air knockback if launcher property is true
            }
            //In any case, clear the hitbox system
            Enemy_HB.ClearValues();
        }
    }

    public Vector3 SetKnockback(bool AirKnockback, bool FR, Vector3 KB)
    {
        Vector3 NKB = KB;
        //If using ground knockback, zero out y value
        if (!AirKnockback)
            NKB = new Vector3(NKB.x, 0);
        if (!FR)
            NKB = Vector3.Reflect(NKB, Vector3.left);
        return NKB;
    }

    public void SetPushback(bool FR, Vector3 KB)
    {
        //Since pushback should be opposite, send the opposite FR value
        RB2.velocity = SetKnockback(false, !FR, KB);
    }
    #endregion

    #region //Animator Methods [Anim_StartAttack; Anim_EndAttack; Anim_Create_Hitbox]
    public void ANIM_StartAttack()
    {
        if (State != FighterState.ATTACK)
        {
            State = FighterState.ATTACK;
            if (OnGround) //Halt horizontal speed if attacking on the ground
                RB2.velocity = new Vector2(0, RB2.velocity.y);
        }
            
    }

    public void ANIM_EndAttack()
    {
        if (State == FighterState.ATTACK)
            State = FighterState.IDLE;
    }

    public void ANIM_Create_Hitbox(string X)
    {
        int ID = int.Parse(X);
        //Hitbox ID - [ Move ID ][ HitBox ID ]
        int M_ID = ID / 100;
        int H_ID = ID % 100;

        SO_Hitbox data = Find_Hitbox_Data(M_ID, H_ID);
        if(data != null)
        {
            GameObject H = Instantiate(HBox_Prefab, transform, false);
            //H.transform.parent = transform;
            H.GetComponent<Hitbox>().INIT(data, this, facing_Right);
        }
    }
    #endregion

    #region //ON-COLLISION METHODS
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //(If touching the ground, set to grounded)
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (!OnGround)
                ANIMCTRL.SetTrigger("Landed");
            OnGround = true;
            ANIMCTRL.SetBool("Grounded", true);
        }
            
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        //(If leaving the ground, set to not grounded)
        if (collision.gameObject.CompareTag("Ground"))
        {
            OnGround = false;
            ANIMCTRL.SetBool("Grounded", false);
        }
    }
    #endregion

    #region //Get / Set Methods [Get on_Right, Get onGround]
    public bool IsOnRight()
    {
        return on_Right;
    }

    public bool IsOnGround()
    {
        return OnGround;
    }
    #endregion

    /// <summary> Searches the player's hitbox list for the specified hitbox, indicate by move and hitbox number. Returns the hitbox if it exists, or null if it does not. </summary>
    public SO_Hitbox Find_Hitbox_Data(int move, int box)
    {
        SO_Hitbox value = null;
        foreach(SO_Hitbox H in MoveList.Hitbox)
        {
            if (H.Move_IDNum == move && H.Hitbox_IDNum == box)
            {
                value = H;
                break;
            }
        }
        return value;
    }
}

#region //Enemy Hitbox Storage [EnemyHitbox_Data]
class EnemyHitbox_Data
{
    private SO_Hitbox lastHitbox; 
    private bool facingRight;

    public SO_Hitbox LastHitbox
    {
        get { return lastHitbox; }
        set { lastHitbox = value; }
    }

    public bool FacingRight
    {
        get { return facingRight; }
        set { facingRight = value; }
    }

    public void ClearValues()
    {
        lastHitbox = null;
        facingRight = true;
    }

    public bool HasProperty(Properties XX)
    {
        if (lastHitbox.Effects.Length == 0)
            return false;
        else
        {
            foreach (Properties P in lastHitbox.Effects)
            {
                if (P == XX)
                    return true;
            }
        }
        return false;
    }
}
#endregion

#region // ANIMATION CONTROL CLASSES [AnimControl; Anim_TriggerTimer]
class AnimControl
{
    private Animator AR;
    private Anim_TriggerTimer[] Timers;
    private float RecoveryTimer;

    public void Init(Animator fighterAR)
    {
        AR = fighterAR;
        Timers = new Anim_TriggerTimer[]
        { new Anim_TriggerTimer("A", fighterAR),
            new Anim_TriggerTimer("B", fighterAR),
            new Anim_TriggerTimer("C", fighterAR),
            new Anim_TriggerTimer("Block", fighterAR),
            new Anim_TriggerTimer("Hurt", fighterAR),
            new Anim_TriggerTimer("Recover", fighterAR),
            new Anim_TriggerTimer("Crumple", fighterAR),
            new Anim_TriggerTimer("Landed", fighterAR)
        };
        RecoveryTimer = 0;
    }

    public void UpdateTimers()
    {
        foreach (Anim_TriggerTimer T in Timers)
        {
            if (T.Get_CountingDown())
                T.UpdateTimer();
        }
    }

    //Activate a specific timer, based on its name
    public void SetTrigger(string trigger)
    {
        bool found = false;
        foreach (Anim_TriggerTimer T in Timers)
        {
            if (found)
            {
                //Nothing
            }
            else if (T.Get_TriggerName() == trigger)
            {
                T.SetTrigger();
                found = true;
            }
        }
    }

    //Set the speed of the accompanying animator
    public void SetAnimSpeed(float speed)
    {
        AR.speed = speed;
    }

    public void SetBool(string name, bool value)
    {
        AR.SetBool(name, value);
    }

    public void SetRecoveryTrigger(int X)
    {
        RecoveryTimer = X / 60.0f;
    }
    public bool UpdateRecoveryTimer()
    {
        if(RecoveryTimer > 0)
        {
            RecoveryTimer = Mathf.Clamp(RecoveryTimer - Time.deltaTime, 0, 100);
            if (RecoveryTimer == 0)
            {
                SetTrigger("Recover");
                return true;
            }    
        }
        return false;
    }
}

class Anim_TriggerTimer
{
    private string triggerName;
    private Animator playerAnimator;
    private float triggerTimer;
    private bool countingDown;

    public Anim_TriggerTimer()
    {
        countingDown = false;
    }
    public Anim_TriggerTimer(string trigger, Animator AR)
    {
        triggerName = trigger;
        playerAnimator = AR;
        countingDown = false;
        triggerTimer = 0;
    }

    public void SetTrigger()
    {
        playerAnimator.SetTrigger(triggerName);
        triggerTimer = 4;
        countingDown = true;
    }
    public void UpdateTimer()
    {
        if (triggerTimer > 0)
        {
            triggerTimer--;
            if (triggerTimer == 0)
            {
                ResetTrigger();
            }
        }
    }
    public void ResetTrigger()
    {
        playerAnimator.ResetTrigger(triggerName);
        triggerTimer = 0;
        countingDown = false;
    }

    public bool Get_CountingDown()
    {
        return countingDown;
    }
    public string Get_TriggerName()
    {
        return triggerName;
    }
}
#endregion

#region // PLAYER INPUT CLASSES [Player_Input; Button]
class Player_Input
{
    public Button Up, Down, Left, Right, A, B, C;

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

    public bool HoldingBack(bool facingRight)
    {
        if (facingRight)
            return HorizInput() == -1;
        else
            return HorizInput() == 1;
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
#endregion
