using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Hurtbox : MonoBehaviour
{
    public Fighter_Parent FP;
    public BoxCollider2D H1, H2, H3, H4;

    //On collission with a hitbox, send the data to the owner player
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Hitbox"))
        {
            Hitbox H = collision.GetComponent<Hitbox>();
            if(H.OwnerID != FP.playerPort)
            {
                FP.PlayerHit(H.HB_Data, H.FacingRight, H.OwnerID);
                H.Contact();
            }
        }
    }

    public void SetActive(bool state)
    {
        H1.enabled = state;
        H2.enabled = state;
        H3.enabled = state;
        H4.enabled = state;
    }
    public void Flip(bool state)
    {
        transform.localScale = new Vector3(state ? -1 : 1, 1, 1);
    }
}
