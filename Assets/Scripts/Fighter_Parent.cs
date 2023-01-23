using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum FighterState { IDLE, ATTACK, BLOCK, FLINCH, AIRFLINCH, CRUMPLE, TUMBLE, PRONE, ANIMATED};
public class Fighter_Parent : MonoBehaviour
{
    //Access to outside gameobjects
    private Game_Manager MGR;                   // Access to the game manager (may not be used, since the fight manager handles gameplay systems)
    private Fighter_Parent otherPlayer;         // Access to the other player. Currently used to inflict pushback when in the corner
    private FightStats FMGR;                    // Access to the fight manager object
    public GameObject HBox_Prefab;              // A copy of the hitbox prefab, referenced to instantiate new hitboxes
    private TextMeshProUGUI HP_Disp;            // A temporary text object that displays the fighter's health
    
    //Access to object's scripts or components
    private Rigidbody2D RB2;
    private SpriteRenderer SR;
    private AnimControl animController;         // A custom class - handles animation transitions and resets triggers that aren't used
    private Player_Input pInput;                // A custom class - parses player input and stores the current controls (for now)
    private Hurtbox myHurtbox;                  // A custom class - contains the fighter's child hurtbox objects. Reference needed to turn them on and off
    private EnemyHitbox_Data Enemy_HB;          // A custom class - stores information from the last hitbox interacted with, up until it is processed

    //Access to universal character state variables
    public int playerPort;                      // Stores whether the fighter is controlled by player 1 or player 2
    [SerializeField]
    private FighterState currentState;          // Stores the current state of the fighter's state machine
    private bool facingRight;                   // When true, the fighter is currently facing to the right (Animations and knocback calculations assume this is true)
    private bool isOnRight;                     // When true, the fighter is currently to the right of their opponent
    public bool IsOnRight { get { return isOnRight; } }
    [SerializeField] private bool onGround;     // When true, the fighter is currently on the ground
    public bool OnGround { get { return onGround; } }
    private bool landed;                        // When true, the fighter has just landed on the ground
    private bool controlsActive;                // When true, the fighter's controls are currently active

    //Universal Fighter Stats <=> Expected to be referenced by all scripts inheriting from this class
    public int maxHealth;
    private int health; public int Health { get { return health; } }
    private int superMeter; public int SuperMeter { get { return superMeter; } }
    [SerializeField] SO_HitboxList moveList;

    //Movement Parameters <=> Expected to be different from fighter to fighter
    [SerializeField] float walkSpeed, jumpForce, gravityForce;
    public float animHoriz, animVert; //Set by animator - determines movement forwards or upwards.

    /// <summary>
    /// A script called after first creating the character. Imports / finds all components needed to run, and sets initial variable values.
    /// </summary>
    /// <param name="portNum"> Sets the player's port number. </param>
    /// <param name="otherFighter"> Reference to the opposing fighter. </param>
    /// <param name="healthDisplay"> The text object player health will be displayed on. </param>
    public void INIT(int portNum, Fighter_Parent otherFighter, TextMeshProUGUI healthDisplay)
    {
        //First - save the references received from the function parameters
        playerPort = portNum; otherPlayer = otherFighter; HP_Disp = healthDisplay;

        //Get access to the active instances of the two game management scripts [REMEMBER - Game_Manager may go unused and be removed]
        FMGR = GameObject.FindGameObjectWithTag("FightMGR").GetComponent<FightStats>();
        MGR = Game_Manager.GetMGR();
        
        //Get access to fighter components and save the references.
        RB2 = GetComponent<Rigidbody2D>(); SR = GetComponent<SpriteRenderer>();
        animController = new AnimControl(GetComponent<Animator>());
        myHurtbox = GetComponentInChildren<Hurtbox>();

        //Create new instances of character classes
        Enemy_HB = new EnemyHitbox_Data();

        //Create the player input class, and save the player's controls (based on if it is controlled by player 1 or 2)
        if (playerPort == 1)
        {
            pInput = new Player_Input(KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Z, KeyCode.X, KeyCode.C);
            //pInput.AssignInputs(KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Z, KeyCode.X, KeyCode.C);
            /*For debug*/ SR.color = Color.cyan;
        }
        else
        {
            pInput = new Player_Input(KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.G, KeyCode.H, KeyCode.J);
            //pInput.AssignInputs(KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.G, KeyCode.H, KeyCode.J);
        }
            

        //Reset the fighter (setting health + HP text, turn hurtboxes on, set to IDLE state, and set initial positions)
        HardResetFighter();
    }

    #region RESET METHODS [ResetFighter; HardResetFighter]
    /// <summary>
    ///  Resets the fighter so the next round can begin. Refills health, turns hurtboxes on, resets player animator, and teleports to start position.
    /// </summary>
    public void ResetFighter()
    {
        //Set to max health and update display
        health = maxHealth;
        HP_Disp.text = "P" + playerPort + ": " + health + "hp";

        //Activate  hurtboxes
        myHurtbox.SetActive(true);

        //Set state machine to default state
        currentState = FighterState.IDLE;

        //Reset the player animator
        animController.SetTrigger("RESET");

        //Set position based on port number
        if (playerPort == 1)
            transform.position = new Vector2(-5.25f, -4);
        else
            transform.position = new Vector2(5.25f, -4);
    }

    /// <summary>
    ///  Resets the fighter so a new game can start. Does everything ResetFighter() does, plus resetting the player's universal and character specific meter.
    /// </summary>
    public void HardResetFighter()
    {
        superMeter = 0;
        ResetFighter();
    }
    #endregion

    void Update()
    {
        //If the fighter's controls are activated, update the controls
        if(controlsActive)
            pInput.UpdateButtons();

        //If on the ground, the fighter updates whether it is facing right or not (also updates hurtbox and sprite flip)
        if(onGround)
            UpdateFacing();

        //Update the trigger timers for the animator, then (if not idle) check if the fighter has recovered from any states.
        animController.UpdateTriggerTimers();
        if(currentState != FighterState.IDLE)
            UpdateRecovery();
        
        //Check attack inputs
        UpdateAttack();

        //Set Movement based on fighter state
        UpdateMovement();

        //Update basic animator variables
        UpdateAnimator();

        //If the fighter has landed, reset the landed parameter.
        if(landed) landed = false;
    }

    #region //CONTROL MANAGEMENT [SetControlsActive]
    /// <summary>
    /// Turns the fighter's controls on or off.
    /// </summary>
    /// <param name="active">Controls are activated when true, and deactivated when false.</param>
    public void SetControlsActive(bool active)
    {
        controlsActive = active;
        //If the controls were deactivated, the state of all buttons are set to default.
        if(!active)
            pInput.ResetButtons();
    }
    #endregion

    #region //UPDATE METHODS [UpdatePosition; UpdateFacing; UpdateMovement; UpdateAttack; UpdateRecovery (and LandProne); UpdateAnimator]
    /// <summary>
    /// Updates the fighter's position relative to their opponent. Called by FightStats, not the fighter itself.
    /// </summary>
    /// <param name="OPP">The opposing fighter's transform.</param>
    public void UpdatePosition(Transform OPP)
    {
        if (isOnRight && transform.position.x < OPP.position.x - 0.15f)
            isOnRight = false;
        else if (!isOnRight && transform.position.x > OPP.position.x + 0.15f)
            isOnRight = true;
    }

    /// <summary>
    /// Sets which way the fighter is facing, which also sets whether the sprites / hurtboxes are flipped. Should be called when on the ground, or after being hit.
    /// </summary>
    public void UpdateFacing()
    {
        facingRight = !isOnRight;       // The fighter should face right if they are on the left, and vice versa
        SR.flipX = !facingRight;        // Player sprites face right by default; they should be flipped if facing left, and vice versa.
        myHurtbox.Flip(!facingRight);   // Like the sprites, the fighter's hurtbox should flip horizontally when facing left.
    }

    /// <summary>
    /// Sets the X and Y speed of the fighter based primarily on player input and the fighter's state. Also where gravity is applied.
    /// </summary>
    private void UpdateMovement()
    {
        float XVel = 0;
        float YVel = RB2.velocity.y; //It is assumed that Y speed is carried over, since it is more often affected by acceleration / gravity.

        switch (currentState)
        {
            case FighterState.IDLE:
                //Maintain X speed in the air, set to horizontal input if grounded and not crouching, else set to zero
                if(!onGround)
                    XVel = RB2.velocity.x;
                else if (pInput.VertInput() > -1)
                    XVel = walkSpeed * pInput.HorizInput();
                else
                    XVel = 0;
                //Also jump if a jump input has been received. Otherwise maintain Y speed (by not changing it)
                if (onGround && pInput.VertInput() == 1 && pInput.VertInputChanged())
                {
                    YVel = jumpForce;
                    animController.SetTrigger("Jump");
                }
                break;
            //If blocking, hit on the ground, or attacking, gradually reduce X speed. Maintain Y speed (by not changing it)
            case FighterState.BLOCK: case FighterState.FLINCH: case FighterState.CRUMPLE: case FighterState.ATTACK:
                
                XVel = Mathf.Clamp((Mathf.Abs(RB2.velocity.x) - 3.9f * Time.deltaTime), 0, 1200) * Mathf.Sign(RB2.velocity.x);
                break;
            //If hit in the air, maintain X speed. Maintain Y speed (by not changing it)
            case FighterState.AIRFLINCH: case FighterState.TUMBLE:
                XVel = RB2.velocity.x;
                break;

            //If none of the above apply, X speed remains 0 and Y speed is preserved.
        }

        //Apply the calculated X and Y velocities, applying the force of gravity to the Y speed;
        RB2.velocity = new Vector2(XVel, YVel - gravityForce * Time.deltaTime);

        /*
        if (currentState == FighterState.IDLE)
        {
            if(!onGround)
            XVel = RB2.velocity.x;
            //Set X Input
            else if (pInput.VertInput() > -1)
                XVel = walkSpeed * pInput.HorizInput();
            else
                XVel = 0;

            //Set Y Input (Jump)
            if (onGround && pInput.VertInput() == 1 && pInput.VertInputChanged())
            {
                YVel = jumpForce;
                animController.SetTrigger("Jump");
            }
                
        }
        else if (currentState == FighterState.BLOCK)
            XVel = RB2.velocity.x;
        //If fighter is being hurt on the ground, reduce horizontal speed through acceleration
        else if(currentState == FighterState.FLINCH || currentState == FighterState.CRUMPLE || currentState == FighterState.ATTACK)
        {
            XVel = Mathf.Clamp((Mathf.Abs(RB2.velocity.x) - 3.9f * Time.deltaTime),0,1200) * Mathf.Sign(RB2.velocity.x);
        }
        //If fighter is being hurt in the air, maintain horizontal speed
        else if (currentState == FighterState.AIRFLINCH || currentState == FighterState.TUMBLE)
        {
            XVel = RB2.velocity.x;
        }

        RB2.velocity = new Vector2(XVel, YVel - gravityForce * Time.deltaTime);
        */
    }

    /// <summary>
    /// Checks if a valid attack input has been received. If one has been, an animator trigger is set.
    /// </summary>
    private void UpdateAttack()
    {
        //If a valid attack button is pressed, tell the animator an attack has started (with an Anim trigger). [NOTE: Update later to include input buffer]
        if (pInput.A.JustPressed() && currentState == FighterState.IDLE)
            animController.SetTrigger("A");
        else if (pInput.B.JustPressed() && currentState == FighterState.IDLE)
            animController.SetTrigger("B");
    }

    /// <summary>
    /// Checks if the fighter should recover from any harmful states, such as hitstun, air flinch/tumble, or air attack frames.
    /// </summary>
    private void UpdateRecovery()
    {
        //Update the recover timer. Store if the fighter just recoverd
        bool JustRecovered = animController.UpdateRecoveryTimer();
        
        switch (currentState)
        {
            //If in the ATTACK OR AIRFLINCH state, skip ahead to the Idle state if the fighter landed
            case FighterState.ATTACK: case FighterState.AIRFLINCH:
                if (landed)
                    currentState = FighterState.IDLE;
                break;
            //If in the TUMBLE State, switch to the prone state if the fighter landed
            case FighterState.TUMBLE:
                if (landed)
                    LandProne();
                break;
            //If in the CRUMPLE State, switch to the prone state once hitstun ends (indicated by RecoveryTimer expiring)
            case FighterState.CRUMPLE:
                if (JustRecovered)
                    LandProne();
                break;
            //If in the FLINCH or BLOCK state, return to normal once time elapses
            case FighterState.FLINCH: case FighterState.BLOCK:
                if(JustRecovered)
                    currentState = FighterState.IDLE;
                break;
            //If in the PRONE state, stand up once the knockdown time ends (indicated by RecoveryTimer expiring)
            case FighterState.PRONE:
                if (JustRecovered)
                {
                    currentState = FighterState.IDLE;
                    myHurtbox.SetActive(true);
                }
                break;
        }

        /* Old IF/Else format
        if (currentState == FighterState.AIRFLINCH && landed)
            currentState = FighterState.IDLE;
        else if (currentState == FighterState.TUMBLE && landed)
            LandProne();
        //If in a ground damage state, return to neutral or fall prone once hitstun elapses
        else if (animController.UpdateRecoveryTimer()) //If this returns true, the fighter has just recovered from hitstun
        {
            if (currentState == FighterState.CRUMPLE)
                LandProne();
            else
            {
                currentState = FighterState.IDLE;
                myHurtbox.SetActive(true);
            }  
        }
        else if (currentState == FighterState.ATTACK && landed)
            currentState = FighterState.IDLE;
        */
    }

    /// <summary>
    /// Sets the fighter to the prone state, while also deactivating their hurtbox. Only sets recovery timer if not dead.
    /// </summary>
    public void LandProne()
    {
        currentState = FighterState.PRONE;
        myHurtbox.SetActive(false);
        if (health > 0)
            animController.SetRecoveryTrigger(20);
    }

    /// <summary>
    /// Update the non-trigger animator components, based on the inputs and fighter states. Includes X/Y Input and the Grounded state.
    /// </summary>
    private void UpdateAnimator()
    {
        //Set horiz and vert inputs, relative to facing
        animController.SetInt("X_Input", facingRight ? pInput.HorizInput() : -pInput.HorizInput());
        animController.SetInt("Y_Input", pInput.VertInput());
        //Update animator bool for grounded state
        animController.SetBool("Grounded", onGround);
    }
    #endregion

    #region //DAMAGE METHODS [PlayerHit; UpdateDamage; CalculateKnockback; CalculatePushback]
    /// <summary>
    /// When the fighter overlaps with a hitbox, stores the hitboxes data to be processed later.
    /// </summary>
    /// <param name="data">The hitbox scriptable object</param>
    /// <param name="R">Whether the hitbox is facing right.</param>
    /// <param name="port">The portnum of the player that spawned the hitbox.</param>
    public void PlayerHit(SO_Hitbox data, bool R, int port)
    {
        //Saves the hitbox if there is none currently, or the prior one has lower priority.
        if(Enemy_HB.LastHitbox == null || Enemy_HB.LastHitbox.priority < data.priority)
        {
            Enemy_HB.LastHitbox = data;
            Enemy_HB.FacingRight = R;
        }
    }

    /// <summary>
    /// If a hitbox has been saved, manages whether the attack was blocked or if the fighter was hit. Called by FightStats, not the fighter itself.
    /// </summary>
    public void UpdateDamage()
    {
        //If a hitbox has been saved
        if(Enemy_HB.LastHitbox != null)
        {
            //TO-DO: Apply Hitstop

            //Check if the fighter is idle, on the ground, and holding back
            bool Blocking = false;
            if (currentState == FighterState.IDLE && onGround && pInput.HoldingBack(facingRight))
            {
                //Block if the attack was a mid
                if (Enemy_HB.LastHitbox.HB_HighLow == HBox_Height.MID)
                    Blocking = true;
                //Block if the attack was high and the fighter is standing
                else if (Enemy_HB.LastHitbox.HB_HighLow == HBox_Height.HIGH && pInput.VertInput() > -1)
                    Blocking = true;
                //Block if the attack was a low and the fighter is crouching
                else if (Enemy_HB.LastHitbox.HB_HighLow == HBox_Height.LOW && pInput.VertInput() < 0)
                    Blocking = true;
            }
            //If the fighter was blocking (holding back on the ground in idle state), negate damage
            if (Blocking)
            {
                //Switch to the block state and set the block animation trigger (to be reset after blockstun)
                currentState = FighterState.BLOCK;
                animController.SetTrigger("Block");
                animController.SetRecoveryTrigger(Enemy_HB.LastHitbox.blockStun);

                //Apply pushback the opponent if the fighter is hit near the edge of the screen
                if (FMGR.FighterNearEdge(transform))
                    otherPlayer.CalculatePushback(Enemy_HB.FacingRight, Enemy_HB.LastHitbox.knockback);
                //Otherwise, apply standard knockback. Don't use air knockback
                else
                    RB2.velocity = CalculateKnockback(false, Enemy_HB.FacingRight, Enemy_HB.LastHitbox.knockback);
            }
            //If the attack was not blocked, check for damage and knocback [To-Do: Check if fighter is vulnerable.]
            else
            {
                //Apply damage to the fighter and update damage display
                health = Mathf.Clamp(health - Enemy_HB.LastHitbox.damage, 0, maxHealth);
                HP_Disp.text = "P" + playerPort + ": " + health + "hp";

                //If the fighter was in the air, apply either a tumble state or air flinch state.
                if (!onGround)
                {
                    if (Enemy_HB.HasProperty(Property.CRUMPLE) || health == 0)
                    {
                        currentState = FighterState.TUMBLE;
                        animController.SetTrigger("Crumple");
                    }
                    else
                    {
                        currentState = FighterState.AIRFLINCH;
                        animController.SetTrigger("Hurt");
                    }
                    //Apply air-based knockback
                    RB2.velocity = CalculateKnockback(true, Enemy_HB.FacingRight, Enemy_HB.LastHitbox.knockback);
                }
                //If fighter was on the ground, apply either launch, crumple, or flinch state
                else
                {
                    if (Enemy_HB.HasProperty(Property.LAUNCH))
                    {
                        currentState = FighterState.TUMBLE;
                        animController.SetTrigger("Launch");
                    }
                    else if (Enemy_HB.HasProperty(Property.CRUMPLE) || health == 0)
                    {
                        currentState = FighterState.CRUMPLE;
                        animController.SetTrigger("Crumple");
                        //Set recovery timer. If knocked out, stand for at least 20 frames.
                        if (health == 0)
                            animController.SetRecoveryTrigger(Mathf.Max(Enemy_HB.LastHitbox.hitStun, 20));
                        else
                            animController.SetRecoveryTrigger(Enemy_HB.LastHitbox.hitStun);
                    }
                    else
                    {
                        currentState = FighterState.FLINCH;
                        animController.SetTrigger("Hurt");
                        //Set recovery timer.
                        animController.SetRecoveryTrigger(Enemy_HB.LastHitbox.hitStun);
                    }

                    //Apply pushback the opponent if the fighter is hit near the edge of the screen
                    if (FMGR.FighterNearEdge(transform))
                        otherPlayer.CalculatePushback(Enemy_HB.FacingRight, Enemy_HB.LastHitbox.knockback);
                    //Apply air based knockback ONLY if the hitbox was a launcher.
                    RB2.velocity = CalculateKnockback(Enemy_HB.HasProperty(Property.LAUNCH), Enemy_HB.FacingRight, Enemy_HB.LastHitbox.knockback);
                }
            }
            //Clear the hitbox system once done
            Enemy_HB.ClearValues();
        }
    }

    /// <summary>
    /// Calculates the amount of knockback to apply to the fighter, based on if the fighter's on the ground and the hitbox is facing right.
    /// </summary>
    /// <param name="airKnockback"> When true, the knockback vector's Y value is not zeroed out. </param>
    /// <param name="facingRight"> When true, the hitbox is facing right. </param>
    /// <param name="knockbackVector"> The direction the hitbox is trying to send the fighter. </param>
    /// <returns></returns>
    public Vector3 CalculateKnockback(bool airKnockback, bool facingRight, Vector3 knockbackVector)
    {
        Vector3 finalVector = knockbackVector;
        //If using ground knockback, zero out y value
        if (!airKnockback)
            finalVector = new Vector3(finalVector.x, 0);
        //If facing left, flip the knockback vector
        if (!facingRight)
            finalVector = Vector3.Reflect(finalVector, Vector3.left);
        return finalVector;
    }

    /// <summary>
    /// Pushes a fighter away from their target. Called by the opposing fighter. Note the pushback force will be the opposite of the original hitbox's.
    /// </summary>
    /// <param name="facingRight">When true, the hitbox was spawned facing right.</param>
    /// <param name="HBoxKnockback">The vector the hitbox should apply to the OTHER player, if it hit.</param>
    public void CalculatePushback(bool facingRight, Vector3 HBoxKnockback)
    {
        //Retain the Y velocity of the current player while applying pushback.
        float PreservedY = RB2.velocity.y;
        //Flip the pushback force by inverting the facingRight value
        RB2.velocity = CalculateKnockback(false, !facingRight, HBoxKnockback) + (Vector3.up * PreservedY);
    }
    #endregion

    #region //ANIMATOR METHODS [Anim_StartAttack; Anim_EndAttack; Anim_Create_Hitbox (and SearchHitBoxData)]
    /// <summary>
    /// Called by the animator to signal the start of an attack. If not already in attack state, sets the fighter to attack state.
    /// </summary>
    public void ANIM_StartAttack()
    {
        if (currentState != FighterState.ATTACK)
        {
            currentState = FighterState.ATTACK;
            if (onGround) //Halt horizontal speed if attacking on the ground
                RB2.velocity = new Vector2(0, RB2.velocity.y);
        }
            
    }

    /// <summary>
    /// Called by the animator to signal the end of an attack. Returns the fighter to Idle state.
    /// </summary>
    public void ANIM_EndAttack()
    {
        if (currentState == FighterState.ATTACK)
            currentState = FighterState.IDLE;
    }

    /// <summary>
    /// Called by the animator to prompt the creation of a hitbox. The hitbox is referenced by a 4 digit string.
    /// </summary>
    /// <param name="MoveID_HBoxID">String of the hitbox to be created, in the form [ Move ID ][ HitBox ID ].</param>
    public void ANIM_Create_Hitbox(string MoveID_HBoxID)
    {
        int ID = int.Parse(MoveID_HBoxID);
        int M_ID = ID / 100;
        int H_ID = ID % 100;

        SO_Hitbox data = SearchHitboxData(M_ID, H_ID);
        if(data != null)
        {
            GameObject H = Instantiate(HBox_Prefab, transform, false);
            //H.transform.parent = transform;
            H.GetComponent<Hitbox>().INIT(data, this, facingRight);
        }
    }
    
    /// <summary>
    /// Searches the player's hitbox list for the specified hitbox, indicate by move and hitbox number. Returns the hitbox if it exists, or null if it does not.
    /// </summary>
    public SO_Hitbox SearchHitboxData(int move, int box)
    {
        SO_Hitbox value = null;
        foreach(SO_Hitbox H in moveList.Hitbox)
        {
            if (H.Move_IDNum == move && H.Hitbox_IDNum == box)
            {
                value = H;
                break;
            }
        }
        return value;
    }
    #endregion

    #region //ON-COLLISION METHODS
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //(If touching the ground, set to grounded)
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (!onGround)
            {
                landed = true;
                animController.SetTrigger("Landed");
            } 
            onGround = true;
        } 
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        //(If leaving the ground, set to not grounded)
        if (collision.gameObject.CompareTag("Ground"))
            onGround = false;
    }
    #endregion
}

#region //OPPONENT HITBOX STORAGE CLASS [EnemyHitbox_Data]
/// <summary>
/// A class used to store hitbox information after colliding with one. Saves the SO_Hitbox and whether the hitbox faces right.
/// </summary>
class EnemyHitbox_Data
{
    private SO_Hitbox lastHitbox; public SO_Hitbox LastHitbox { get { return lastHitbox; } set { lastHitbox = value; } }
    private bool facingRight; public bool FacingRight { get { return facingRight; } set { facingRight = value; } }

    /// <summary>
    /// Clears the stored hitbox data the class has stored.
    /// </summary>
    public void ClearValues()
    {
        lastHitbox = null;
        facingRight = true;
    }

    /// <summary>
    /// Checks if the currently stored hitbox has a particular hitbox property.
    /// </summary>
    /// <param name="HB_Property">The hitbox property to find.</param>
    /// <returns>Returns true if the hitbox exists and has the requested property.</returns>
    public bool HasProperty(Property HB_Property)
    {
        //Returns false if there is no hitbox saved, or it has no properties.
        if (lastHitbox == null || lastHitbox.Properties.Length == 0)
            return false;
        else
        {
            //Otherwise, it searches the list of hitboxes and returns true if a match is found.
            foreach (Property P in lastHitbox.Properties)
            {
                if (P == HB_Property)
                    return true;
            }
        }

        //If no match found, returns false
        return false;
    }
}
#endregion

#region // ANIMATION CONTROL CLASSES [AnimControl; Anim_TriggerTimer]
/// <summary>
/// A class that sets animator triggers and disables them after a few frames. Also contains a recovery timer used for several 
/// </summary>
class AnimControl
{
    private Animator AR;
    private Anim_TriggerTimer[] timers;
    //private float recoveryTimer;
    private Timer recoveryT;

    public AnimControl(Animator fighterAR)
    {
        AR = fighterAR;
        timers = new Anim_TriggerTimer[]
        { new Anim_TriggerTimer("A", fighterAR),
            new Anim_TriggerTimer("B", fighterAR),
            new Anim_TriggerTimer("C", fighterAR),
            new Anim_TriggerTimer("Block", fighterAR),
            new Anim_TriggerTimer("Hurt", fighterAR),
            new Anim_TriggerTimer("Recover", fighterAR),
            new Anim_TriggerTimer("Crumple", fighterAR),
            new Anim_TriggerTimer("Landed", fighterAR),
            new Anim_TriggerTimer("Launch", fighterAR),
            new Anim_TriggerTimer("Jump", fighterAR),
            new Anim_TriggerTimer("RESET", fighterAR)

        };
        //recoveryTimer = 0;
        recoveryT = new Timer();
    }

    /* [OLD animator INIT script]
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
            new Anim_TriggerTimer("Landed", fighterAR),
            new Anim_TriggerTimer("Launch", fighterAR),
            new Anim_TriggerTimer("Jump", fighterAR),
            new Anim_TriggerTimer("RESET", fighterAR)

        };
        RecoveryTimer = 0;
    }
    */

    /// <summary>
    /// Advances all active trigger reset timers. Prevents time-sensitive triggers from remaining active for too long.
    /// </summary>
    public void UpdateTriggerTimers()
    {
        foreach (Anim_TriggerTimer T in timers)
        {
            if (T.CountingDown)
                T.UpdateTimer();
        }
    }

    /// <summary>
    /// Activates a specific trigger based on its name, and sets the duration it is active for.
    /// </summary>
    /// <param name="trigger">The name of the trigger to set a timer for.</param>
    public void SetTrigger(string trigger)
    {
        bool found = false;
        foreach (Anim_TriggerTimer T in timers)
        {
            if (found)
            {
                //Nothing
            }
            else if (T.TriggerName == trigger)
            {
                T.SetTrigger();
                found = true;
            }
        }
    }

    /// <summary>
    /// Set the speed of the animator.
    /// </summary>
    /// <param name="speed">The speed, in normalized time, the animation should play at.</param>
    public void SetAnimSpeed(float speed)
    {
        AR.speed = speed;
    }

    /// <summary>
    /// Set the value of the animator's bool parameter.
    /// </summary>
    /// <param name="name">The name of the bool parameter to change.</param>
    /// <param name="value">The value to set the bool to.</param>
    public void SetBool(string name, bool value)
    {
        AR.SetBool(name, value);
    }

    /// <summary>
    /// Set the value of the animator's integer parameter.
    /// </summary>
    /// <param name="name">The name of the integer parameter to change.</param>
    /// <param name="value">The value to set the integer to.</param>
    public void SetInt(string name, int value)
    {
        AR.SetInteger(name, value);
    }

    /// <summary>
    /// Set the value of the animator's float parameter.
    /// </summary>
    /// <param name="name">The name of the float parameter to change.</param>
    /// <param name="value">The value to set the float to.</param>
    public void SetFloat(string name, float value)
    {
        AR.SetFloat(name, value);
    }

    /// <summary>
    /// Set the recovery trigger for a certain amount of time.
    /// </summary>
    /// <param name="X">The amount of time, in frames, that the recoveyr timer should run.</param>
    public void SetRecoveryTrigger(int X)
    {
        //recoveryTimer = X / 60.0f;
        recoveryT.SetTimer(X / 60.0f);
    }

    /// <summary>
    /// Advances the recovery timer if it is running.
    /// </summary>
    /// <returns>Returns true if the timer just reached zero, otherwise returns false.</returns>
    public bool UpdateRecoveryTimer()
    {
        recoveryT.UpdateTimer();
        if(recoveryT.TimeJustExpired)
        {
            SetTrigger("Recover");
            return true;
        }
        return false;

        /*
        if (recoveryTimer > 0)
        {
            recoveryTimer = Mathf.Clamp(recoveryTimer - Time.deltaTime, 0, 100);
            if (recoveryTimer == 0)
            {
                SetTrigger("Recover");
                return true;
            }    
        }
        return false;
        */
    }
}

/// <summary>
/// Controls an individual animator trigger. Sets the trigger, then automatically resets the trigger after 4 frames.
/// </summary>
class Anim_TriggerTimer
{
    private string triggerName; public string TriggerName { get { return triggerName; } }
    private Animator playerAnimator;
    private float triggerTimer;
    private bool countingDown; public bool CountingDown { get { return countingDown; } }

    public Anim_TriggerTimer(string trigger, Animator AR)
    {
        triggerName = trigger;
        playerAnimator = AR;
        countingDown = false;
        triggerTimer = 0;
    }

    /// <summary>
    /// Sets the saved trigger in the animator
    /// </summary>
    public void SetTrigger()
    {
        playerAnimator.SetTrigger(triggerName);
        triggerTimer = 4;
        countingDown = true;
    }

    /// <summary>
    /// Advances the trigger timer by one frame, if not already at 0. Resets the trigger if it reaches 0.
    /// </summary>
    public void UpdateTimer()
    {
        if (triggerTimer > 0)
        {
            triggerTimer--;
            if (triggerTimer <= 0)
            {
                ResetTrigger();
            }
        }
    }

    /// <summary>
    /// Resets the trigger, and indicates that it is no longer counting down.
    /// </summary>
    public void ResetTrigger()
    {
        playerAnimator.ResetTrigger(triggerName);
        triggerTimer = 0;
        countingDown = false;
    }
}
#endregion

#region // PLAYER INPUT CLASSES [Player_Input; Button]
/// <summary>
/// Class that stores the fighter's input method, updates the state of each individual input key, and reports when certain inputs are made.
/// </summary>
class Player_Input
{
    public Button Up, Down, Left, Right, A, B, C;
    private Button[] allButtons;

    public Player_Input(KeyCode Uk, KeyCode Dk, KeyCode Lk, KeyCode Rk, KeyCode Ak, KeyCode Bk, KeyCode Ck)
    {
        Up = new Button(Uk); Down = new Button(Dk); Left = new Button(Lk); Right = new Button(Rk);
        A = new Button(Ak); B = new Button(Bk); C = new Button(Ck);
        
        allButtons = new Button[] { Up, Down, Left, Right, A, B, C };
    }

    /* OLD input assignment method. To replace with single input assignment method
    /// <summary>
    /// Assigns which keycode each input button should reference.
    /// </summary>
    /// <param name="Uk">The keycode for the Up input</param>
    /// <param name="Dk">The keycode for the Down input</param>
    /// <param name="Lk">The keycode for the Left input</param>
    /// <param name="Rk">The keycode for the Right input</param>
    /// <param name="Ak">The keycode for the A (Light Attack) input</param>
    /// <param name="Bk">The keycode for the B (Heavy Attack) input</param>
    /// <param name="Ck">The keycode for the C (Special Attack) input</param>
    public void AssignInputs(KeyCode Uk, KeyCode Dk, KeyCode Lk, KeyCode Rk, KeyCode Ak, KeyCode Bk, KeyCode Ck)
    {
        Up.INIT(Uk); Down.INIT(Dk); Left.INIT(Lk); Right.INIT(Rk);
        A.INIT(Ak); B.INIT(Bk); C.INIT(Ck);
    }
    */

    /// <summary>
    /// Updates the state for each button - each updates whether they are / are not being pressed, and for how long.
    /// </summary>
    public void UpdateButtons()
    {
        foreach(Button X in allButtons)
        {
            X.UpdateState();
        }
    }

    /// <summary>
    /// Retrieves the horizontal input based on whether the left or right buttons are each being pressed.
    /// </summary>
    /// <returns> -1 if only left is pressed, 1 if only right is pressed, and 0 if neither or both are pressed.</returns>
    public int HorizInput()
    {
        return 0 + (Left.IsPressed ? -1 : 0) + (Right.IsPressed ? 1 : 0);
    }

    /// <summary>
    /// Retrieves the vertical input based on whether the up or down buttons are each being pressed.
    /// </summary>
    /// <returns> -1 if only down is pressed, 1 if only up is pressed, and 0 if neither or both are pressed.</returns>
    public int VertInput()
    {
        return 0 + (Down.IsPressed ? -1 : 0) + (Up.IsPressed ? 1 : 0);
    }

    /// <summary>
    /// Checks if the vertical input has just changed. Checks both the up and down buttons.
    /// </summary>
    /// <returns>True if either the up or down input has changed.</returns>
    public bool VertInputChanged()
    {
        return Up.StateChanged || Down.StateChanged;
    }

    /// <summary>
    /// Checks whether the fighter is currently holding back horizontally, relative to the direction faced.
    /// </summary>
    /// <param name="facingRight">If true, the fighter is currently looking right.</param>
    /// <returns>True if the fighter's X-Input is strictly negative while facing right, OR positive while facing left.</returns>
    public bool HoldingBack(bool facingRight)
    {
        if (facingRight)
            return HorizInput() == -1;
        else
            return HorizInput() == 1;
    }

    /// <summary>
    /// Resets all buttons to the "Not Pressed" state, and sets button duration to 0. Button's StateChanged value also set to false;
    /// </summary>
    public void ResetButtons()
    {
        foreach (Button X in allButtons)
        {
            X.ResetButton();
        }
    }
}

/// <summary>
/// Class that stores and updates the state of a single input key.
/// </summary>
class Button
{
    private KeyCode Key;
    private float stateDuration;    public float StateDuration { get { return stateDuration; } }
    private bool isPressed;         public bool IsPressed { get { return isPressed; } }
    private bool stateChanged;      public bool StateChanged { get { return stateChanged; } }

    public Button(KeyCode K)
    {
        Key = K;
        stateDuration = 0;
        stateChanged = false;
        isPressed = false;
    }
    
    /// <summary>
    /// Reassigns which keycode this button should respond to.
    /// </summary>
    /// <param name="K">The New Keycode for this input. </param>
    public void ChangeKeyCode(KeyCode K)
    {
        Key = K;
        ResetButton();
    }

    /// <summary>
    /// Update if the button is being pressed, how long it has been released / pressed for, and if its state just changed.
    /// </summary>
    public void UpdateState()
    {
        //If in "not pressed" state and key is being pressed or was just pressed, set to "pressed" state.
        if(!isPressed && (Input.GetKeyDown(Key) || Input.GetKey(Key)))
        {
            isPressed = true;
            stateDuration = 0;
        }
        //If in "pressed" state and key is not being pressed or was just released, set to "not pressed" state.
        else if (isPressed && (Input.GetKeyUp(Key) || !Input.GetKey(Key)))
        {
            isPressed = false;
            stateDuration = 0;
        }
        //Else, increase the timer indicating how long the current state has lasted. Cap at 1 second.
        else if (stateDuration < 1)
        {
            stateDuration = Mathf.Clamp(stateDuration + Time.deltaTime, 0, 1);
        }

        //Record that the state has just changed if the duration timer is 0.
        stateChanged = (stateDuration == 0);
    }

    /// <summary>
    /// Checks if this button was just pressed down this frame.
    /// </summary>
    /// <returns>If the button is currently pressed AND its state just changed.</returns>
    public bool JustPressed()
    {
        return stateChanged && isPressed;
    }

    /// <summary>
    /// Checks if this button was just released this frame.
    /// </summary>
    /// <returns>If the button is currently relesased AND its state just changed.</returns>
    public bool JustReleased()
    {
        return stateChanged && !isPressed;
    }

    /// <summary>
    /// Resets an individual button's value. Duration = 0; IsPressed & Statechanged are set to false.
    /// </summary>
    public void ResetButton()
    {
        stateDuration = 0;
        isPressed = false;
        stateChanged = false;
    }
}
#endregion
