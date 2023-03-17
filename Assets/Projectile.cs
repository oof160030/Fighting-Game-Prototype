using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected SO_Projectile projectileData;
    protected Fighter_Parent owner;
    protected int ownerID;
    protected bool facingRight;

    protected SpriteRenderer SR;
    protected Rigidbody2D RB;
    protected Animator AR;

    [SerializeField] protected GameObject hitboxPrefab;
    [SerializeField] protected SO_Hitbox[] hitboxList;

    public void INIT(SO_Projectile pData, Fighter_Parent fighterOwner, bool lookingRight)
    {
        projectileData = pData;
        owner = fighterOwner;
        ownerID = owner.playerPort;
        facingRight = lookingRight;

        //transform.position = facingRight ? pData.startPosition : Vector3.Reflect(pData.startPosition, Vector3.left);

        SR = gameObject.GetComponent<SpriteRenderer>();
        RB = gameObject.GetComponent<Rigidbody2D>();
        //AR = gameObject.GetComponent<Animator>();
    }

    /// <summary>
    /// Called when the projectile is first created
    /// </summary>
    public virtual void Create()
    {

    }

    /// <summary>
    /// Called when the projectile comes in contact with the opponent
    /// </summary>
    public virtual void Contact()
    {

    }

    /// <summary>
    /// Called when the projectile is destroyed
    /// </summary>
    public virtual void Die()
    {

    }
}
