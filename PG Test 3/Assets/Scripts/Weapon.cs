using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    Player player;
    CameraControl playerCamera;
    GameObject crosshair;
    Collider2D weaponCollider;
    Animator weaponAnimator;
    public enum WeaponType { Sword, Gun}
    //bool isColliding;

    [Header("General Settings")]
    public WeaponType weaponType;
    public float basicAttackDamage;
    public float basicAttackStamina;

    [Header("Shake Settings")]
    public bool shake;
    public float shakeMagnitud;
    public float shakeDuration;

    [Header("Gun Specifics")]
    public GameObject projectile;
    public int ammunition;

    void Start ()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
        playerCamera = player.transform.Find("Player_Camera").GetComponent<CameraControl>();
        crosshair = player.transform.Find("Player_Crosshair").Find("Player_Crosshair_Sprite").gameObject;
        weaponCollider = GetComponent<Collider2D>();
        weaponAnimator = GetComponent<Animator>();       
    }

    void Update()
    {
        //// reset the anti double trigger safeguard bool
        //isColliding = false;    

        // reset collider on rest if it gets stuck
        if (weaponType == WeaponType.Sword && weaponAnimator.GetInteger("State") == 0 && weaponCollider.enabled)
            weaponCollider.enabled = false;

        //if (weaponType == WeaponType.Sword && weaponCollider.enabled)
        //    print("player enabled");
    }

    void InitAnimation()
    {
        player.inAnimation = true;
        player.LoseStamina(basicAttackStamina);
    }

    void EndAnimation()
    {
        player.inAnimation = false;
        player.outAnimation = true;
        weaponAnimator.SetInteger("State", 0);
    }

    void Attack()
    {
        if (weaponType == WeaponType.Sword)
            weaponCollider.enabled = true;
        else if (weaponType == WeaponType.Gun)
            SpawnProjectile();

        //call camera shake for recoil
        if (shake)
            playerCamera.Shake((player.transform.position - crosshair.transform.position).normalized, shakeMagnitud, shakeDuration);
    }

    void Idle()
    {
        if (weaponType == WeaponType.Sword)
            weaponCollider.enabled = false;             
    }

    void SpawnProjectile()
    {
        // lose ammo
        ammunition--;
        player.checkAmmo = true;
        // spawn the projectile
        Vector3 spawnPos = transform.Find("Gun_Tip").position;
        Quaternion spawnRot = Quaternion.identity;
        Projectile proj = Instantiate(projectile, spawnPos, spawnRot).GetComponent<Projectile>();
        // give direction, rotation and damage to the projectile
        proj.Setup(crosshair.transform.position, spawnPos, basicAttackDamage);
    }

    void OnTriggerEnter2D(Collider2D trigger)
    {
        //// to prevent the message from firing twice in the same frame
        //if (isColliding)
        //    return;        

        // collision with enemy
        if (trigger.tag == "Enemy")
        {
            //isColliding = true;
            if (weaponType == WeaponType.Sword)
                trigger.GetComponent<Enemy>().RecieveDamageNumbers(basicAttackDamage);
        }
    }
}
