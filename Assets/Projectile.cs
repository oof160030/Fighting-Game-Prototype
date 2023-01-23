using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected SO_Projectile projectileData;
    protected Fighter_Parent owner;
    protected int ownerID;
    protected bool facingRight;

    [SerializeField] protected GameObject hitboxPrefab;
    [SerializeField] protected SO_Hitbox[] hitboxList;

    public void INIT(SO_Projectile pData, Fighter_Parent fighterOwner, bool lookingRight)
    {
        projectileData = pData;
        owner = fighterOwner;
        ownerID = owner.playerPort;
        facingRight = lookingRight;
    }
}
