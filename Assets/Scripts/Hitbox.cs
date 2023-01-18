using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HBox_Height { LOW, MID, HIGH };
public class Hitbox : MonoBehaviour
{
    private SO_Hitbox HB_Data;
    private bool facingRight;
    private int OwnerID;
    private Fighter_Parent Owner;
    
    public void INIT(SO_Hitbox data, Fighter_Parent O, bool right)
    {
        HB_Data = data;
        Owner = O;
        OwnerID = O.PlayerPort;
        facingRight = right;

        //Set size and position
        transform.localScale = HB_Data.size;
        transform.localPosition = facingRight ? HB_Data.position : Vector3.Reflect(HB_Data.position, Vector3.left);

        //Destroy the hitbox after the lifespan elapses
        Destroy(gameObject, HB_Data.lifespan / 60.0f);
    }

    //Triggered by the hurtbox after this hitbox comes in contact with it
    public void Contact()
    {
        Destroy(gameObject);
    }

    //GET methods for the hitbox data - retrieved by the hurtbox on contact
    public SO_Hitbox hb_Data { get { return HB_Data; } }
    public bool FacingRight { get { return facingRight; } }
    public int ownerID { get { return OwnerID; } }
}
