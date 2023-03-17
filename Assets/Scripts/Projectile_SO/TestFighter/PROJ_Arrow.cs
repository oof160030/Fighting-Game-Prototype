using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PROJ_Arrow : Projectile
{
    public float moveSpeed;
    // Start is called before the first frame update
    void Start()
    {
        Create();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Create()
    {
        //Turn to face correct direction
        if(!facingRight)
        {
            SR.flipX = true;
        }

        //Begin moving in correct direction
        RB.velocity = (facingRight ? Vector3.right : Vector3.left) * moveSpeed;

        //Create hitbox
        GameObject Box = Instantiate(hitboxPrefab, transform);
        Hitbox HBox = Box.GetComponent<Hitbox>();
        HBox.INIT(hitboxList[0], owner, facingRight);
        
    }

    public override void Contact()
    {
        base.Contact();
        //Destroy on contact
        Destroy(gameObject);
    }
}
