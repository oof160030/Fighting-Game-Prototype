using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Hurtbox : MonoBehaviour
{
    public Fighter_Parent FP;

    //On collission with a hitbox, send the data to the owner player
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Hitbox"))
        {
            Hitbox H = collision.GetComponent<Hitbox>();
            if(H.ownerID != FP.PlayerPort)
            {
                FP.PlayerHit(H.hb_Data, H.FacingRight, H.ownerID);
                H.Contact();
                Debug.Log("contact");
            }
        }
    }
}
