using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    Enemy enemy;
    Player player;
    Collider2D weaponCollider;
    Animator weaponAnimator;
    public enum WeaponType { Sword, Gun }
    //bool isColliding;

    [Header("Shake Settings")]
    public bool shake;
    public float shakeMagnitud;
    public float shakeDuration;

    [Header("General Settings")]
    public WeaponType weaponType;
    public float basicAttackDamage;

    [Header("Gun Specifics")]
    public GameObject projectile;

    void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        weaponCollider = GetComponent<Collider2D>();
        weaponAnimator = GetComponent<Animator>();
        // find the player  
        player = GameObject.Find("Player").GetComponent<Player>();
    }

    void Update()
    {
        //// reset the anti double trigger safeguard bool
        //isColliding = false;

        // reset collider on rest if it gets stuck
        if (weaponType == WeaponType.Sword)
        {
            if (weaponAnimator.GetInteger("State") == 0 && weaponCollider.enabled)
                weaponCollider.enabled = false;
        }

        //if (weaponCollider.enabled)
        //    print("enemy enabled");
    }

    void InitAnimation()
    {
        enemy.inAnimation = true;
    }

    void EndAnimation()
    {
        enemy.inAnimation = false;
        enemy.EndAttack();
        weaponAnimator.SetInteger("State", 0);
    }

    void Attack()
    {
        if (weaponType == WeaponType.Sword)
        weaponCollider.enabled = true;
        else if (weaponType == WeaponType.Gun)
            SpawnProjectile();
    }

    void Idle()
    {
        if (weaponType == WeaponType.Sword)
            weaponCollider.enabled = false;        
    }

    void SpawnProjectile()
    {        
        // spawn the projectile
        Vector3 spawnPos = transform.Find("Gun_Tip").position;
        Quaternion spawnRot = Quaternion.identity;
        EnemyProjectile proj = Instantiate(projectile, spawnPos, spawnRot).GetComponent<EnemyProjectile>();              
        // give direction, rotation and damage to the projectile, plus shake stats if enabled in the gun        
        proj.Setup(player, spawnPos, basicAttackDamage, shake, shakeMagnitud, shakeDuration);
    }

    void OnTriggerEnter2D(Collider2D trigger)
    {
        //// to prevent the message from firing twice in the same frame
        //if (isColliding)
        //    return;        

        //collision with player
        if (trigger.tag == "Player")
        {
            //isColliding = true;
            if (weaponType == WeaponType.Sword)
            {
                // we already got the player on start
                if (!player.iframes)
                {
                    // do the damage and stuff
                    player.RecieveDamageNumbers(basicAttackDamage, shake, transform.position, shakeMagnitud, shakeDuration);
                }
            }
        }
    }
}
