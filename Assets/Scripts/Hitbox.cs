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
    private Fighter_Parent owner;
    private bool contacted; //Useful for hitboxes that send a response back to the owner or change properties on contact.
    
    /// <summary>
    /// Sets up the basic parameters for the given hitbox, specifical the Scriptable Object reference, the direction faced, and the owner.
    /// </summary>
    /// <param name="data">The SO_Hitbox data to store</param>
    /// <param name="parentFighter">Reference to the fighter that created the hitbox.</param>
    /// <param name="right">Whether the fighter that spawned the hitbox was facing right.</param>
    public void INIT(SO_Hitbox data, Fighter_Parent parentFighter, bool right)
    {
        hb_data = data;
        owner = parentFighter;
        ownerID = parentFighter.playerPort;
        facingRight = right;

        //Set size and position
        transform.localScale = hb_data.size;
        transform.localPosition = facingRight ? hb_data.position : Vector3.Reflect(hb_data.position, Vector3.left);

        //Destroy the hitbox after the lifespan elapses
        Destroy(gameObject, hb_data.lifespan / 60.0f);
        contacted = false;
    }

    /// <summary>
    /// The behavior the hitbox should perform on contact. Can include destroying itself, or sending a method call back to the fighter that owns it.
    /// </summary>
    public void Contact()
    {
        Destroy(gameObject);
    }
}
