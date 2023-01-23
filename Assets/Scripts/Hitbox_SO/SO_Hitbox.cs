using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hitbox", menuName = "ScriptableObjects/New Hitbox")]
public class SO_Hitbox : ScriptableObject
{
    //All of the information associated with a specific hitbox
    [Tooltip("Referenced by animator. Refers to which move it is associated with.")]
    public int Move_IDNum;
    [Tooltip("Referenced by animator. For moves with multiple hitboxes, refers to which hitbox this one is.")]
    public int Hitbox_IDNum;
    [Tooltip("Position the hitbox instantiates in, relative to the fighter. Assumes right facing.")]
    public Vector3 position;
    [Tooltip("Hitbox dimensions")]
    public Vector3 size;
    [Tooltip("Damage the hitbox deals to its target")]
    public int damage;
    [Tooltip("The number of frames the hitbox persists for if not interacted with.")]
    public int lifespan;
    [Tooltip("If a move connects with multiple hitboxes, the one with the highest priority takes effect.")]
    public int priority;
    [Tooltip("The direction the hitbox will send the opponent on contact. Assumes right facing.")]
    public Vector3 knockback;
    [Tooltip("The number of frames the attacker's and defender's animations pause for on hit.")]
    public int hitStop;
    [Tooltip("The duration, in frames, of the defender's hit animation.")]
    public int hitStun;
    [Tooltip("The duration, in frames, of the defender's block animation.")]
    public int blockStun;

    [Tooltip("Determines whether hitbox deals more barrier damage when blocked high or low, both, or neither.")]
    public HBox_Height HB_HighLow;
    [Tooltip("Determines which hitbox effects apply;")]
    public Property[] Properties;
}
