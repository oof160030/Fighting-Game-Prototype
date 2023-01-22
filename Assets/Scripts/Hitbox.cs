using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HBox_Height { MID, LOW, HIGH };
public enum Property { CRUMPLE, LAUNCH };
public class Hitbox : MonoBehaviour
{
    private SO_Hitbox hb_data; public SO_Hitbox HB_Data { get { return hb_data; } }
    private bool facingRight; public bool FacingRight { get { return facingRight; } }
    private int ownerID; public int OwnerID { get { return ownerID; } }
    private Fighter_Parent Owner;
    
    public void INIT(SO_Hitbox data, Fighter_Parent O, bool right)
    {
        hb_data = data;
        Owner = O;
        ownerID = O.playerPort;
        facingRight = right;

        //Set size and position
        transform.localScale = hb_data.size;
        transform.localPosition = facingRight ? hb_data.position : Vector3.Reflect(hb_data.position, Vector3.left);

        //Destroy the hitbox after the lifespan elapses
        Destroy(gameObject, hb_data.lifespan / 60.0f);
    }

    //Triggered by the hurtbox after this hitbox comes in contact with it
    public void Contact()
    {
        Destroy(gameObject);
    }
}
