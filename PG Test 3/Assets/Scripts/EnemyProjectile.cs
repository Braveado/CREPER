using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    //bool isColliding;
    Player player;
    Vector3 direction;
    public float speed;
    float angle;
    [HideInInspector]
    public float gunDamage;
    [HideInInspector]
    public bool gunShake;
    [HideInInspector]
    public float gunShakeMagnitud;
    [HideInInspector]
    public float gunShakeDuration;

    void Update()
    {
        // move the projectile
        transform.position += direction * speed * Time.deltaTime;

        //// reset the anti double trigger safeguard bool
        //isColliding = false;
    }

    public void Setup(Player target, Vector3 origin, float damage, bool shake, float magnitud, float duration)
    {
        // get player
        player = target;
        // get direction to move
        direction = Vector3.Normalize(player.transform.position - origin);
        // get angle to rotate
        angle = -1 * Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // rotate the projectile
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.back);
        // get the damage of the projectile
        gunDamage = damage;
        // get the shake stats
        if(shake)
        {
            gunShake = true;
            gunShakeMagnitud = magnitud;
            gunShakeDuration = duration;
        }
    }

    void OnTriggerEnter2D(Collider2D trigger)
    {
        ////to prevent the message from firing twice in the same frame
        //if (isColliding)
        //    return;        

        // collision with enemy
        if (trigger.tag == "Player")
        {
            //isColliding = true;
            if (!player.iframes)
            {
                trigger.GetComponent<Player>().RecieveDamageNumbers(gunDamage, gunShake, transform.position, gunShakeMagnitud, gunShakeDuration);
                DestroyObject(gameObject);
            }
        }
        else if (trigger.tag != "Enemy")
            DestroyObject(gameObject);
    }
}
