using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hitbox_List", menuName = "ScriptableObjects/New Hitbox List")]
public class SO_HitboxList : ScriptableObject
{
    [Tooltip("The Character ID the movelist is associated with")]
    public int CharacterID;
    public SO_Hitbox[] Hitbox;
}
