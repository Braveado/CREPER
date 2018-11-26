using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    //bool isColliding;
    Vector3 direction;
    public float speed;
    float angle;
    [HideInInspector]
    public float gunDamage;

	void Update ()
    {
        // move the projectile
        transform.position += direction * speed * Time.deltaTime;

        //// reset the anti double trigger safeguard bool
        //isColliding = false;
    }

    public void Setup(Vector3 target, Vector3 origin, float damage)
    {
        // get direction to move
        direction = Vector3.Normalize(target - origin);
        // get angle to rotate
        angle = -1 * Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // rotate the projectile
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.back);
        // get the damage of the projectile
        gunDamage = damage;
    }

    void OnTriggerEnter2D(Collider2D trigger)
    {
        ////to prevent the message from firing twice in the same frame
        //if (isColliding)
        //    return;        

        // collision with enemy
        if (trigger.tag == "Enemy")
        {
            //isColliding = true;
            trigger.GetComponent<Enemy>().RecieveDamageNumbers(gunDamage);
            DestroyObject(gameObject);
        }
        else if (trigger.tag != "Player")
            DestroyObject(gameObject);
    }
}
