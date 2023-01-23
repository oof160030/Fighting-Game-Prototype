using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Projectile_List", menuName = "ScriptableObjects/New Projectile List")]
public class SO_ProjectileList : ScriptableObject
{
    [Tooltip("The Character ID the projectile list is associated with")]
    public int CharacterID;
    public SO_Projectile[] Projectile;
}
