using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Projectile", menuName = "ScriptableObjects/New Projectile")]
public class SO_Projectile : ScriptableObject
{
    [Tooltip("A unique identifier for each kind of projectile.")]
    public int projectileID;
    [Tooltip("The gameobject associated with the projectile.")]
    public GameObject projectileObject;
    [Tooltip("The position relative to the fighter to spawn the projectile. Used if game logic does not dictate position.")]
    public Vector3 startPosition;
    [Tooltip("The direction the projectile should move in, assuming the fighter is facing right. Used if game logic does not dictate direction.")]
    public Vector3 startVelocity;
    [Tooltip("The amount of time the projectile should last, in seconds. Used if game logic does not dictate duration.")]
    public float lifetime;
}
