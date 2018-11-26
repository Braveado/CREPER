using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class Enemy : MonoBehaviour
{
    Animator enemyAnimator;
    Animator weaponAnimator;
    SortingGroup enemyGroup;
    SpriteRenderer enemyRender;
    Vector2 enemyPos, playerPos;
    GameObject player;
    bool icon;
    float antiStuck;
    // to recieve damage only once per trigger enter
    bool recieveDamage;
    float damageRecieved;

    [Header("General Settings")]
    public GameObject enemyMinimapIcon;
    float playerMinimapViewDistance;
    public int scoreAmount;
    public GameObject lifeHUDElement;
    public Image lifeBar;
    public Image lifeBarBacklayer;
    public Text damageText;
    public float maxLife;
    public float life;
    bool updateLifeBacklayer;
    float lifeBacklayerTimer;
    bool damaged;
    bool dead;
    [HideInInspector]
    public bool inAnimation;
    public GameObject weaponSlot;
    SpriteRenderer[] weaponRenders;
    EnemyWeapon weapon;

    [Header("Normal State")]
    public float movementChance;
    public float speed;
    public float movementWait;
    float waitedForMovement;
    bool moving;
    Vector3 direction;
    Vector3 targetPosition;
    bool rotateWeapon;
    float weaponRotation;

    [Header("Aggro State")]
    public float chaseSpeedMultiplier;
    public float aggroRange;
    public float minChaseDistance;
    public float actionsCD;
    float actionsCDtimer;
    bool aggro;
    Vector2 playerDirection;
    RaycastHit2D lineOfSight;
    Vector2 lastSeenPlayer;
    bool los;

    void Start()
    {        
        // get animator
        enemyAnimator = GetComponent<Animator>();
        weaponAnimator = weaponSlot.GetComponentInChildren<Animator>();
        // get render
        enemyRender = GetComponent<SpriteRenderer>();
        enemyGroup = GetComponent<SortingGroup>();
        weaponRenders = weaponSlot.GetComponentsInChildren<SpriteRenderer>();
        // get weapon
        weapon = weaponSlot.GetComponentInChildren<EnemyWeapon>();
        // set enemy position
        enemyPos.x = transform.position.x;
        enemyPos.y = transform.position.y;
        // get player
        player = GameObject.Find("Player");
        // set movement wait
        waitedForMovement = Time.time + movementWait;
    }

    void Update()
    {
        // changes the sorting order for the whole enemy
        UpdateSortingOrder();
        // if attacked recieve damage
        if (recieveDamage)
            RecieveDamage(damageRecieved);
        if (!dead)
        {
            // to check whether or not show the minimap icon
            UpdateMinimapIcon();
            // to see if the enemy should aggro or not
            UpdateAggro();
            if (!damaged && !inAnimation)
            {
                // to make the enemy move in random directions
                if (!aggro)
                    UpdateRandomMovement();
                // to make enemy chase the player
                else if (aggro)
                    UpdateChasing();
            }
        }
        // update the enemy lifebar
        UpdateLifeBacklayer();        
    }

    void UpdateSortingOrder()
    {
        // based on the y axis
        enemyGroup.sortingOrder = -(int)((transform.position.y + 0.2) * 10);
    }

    void UpdateMinimapIcon()
    {
        // update enemy position
        enemyPos.x = transform.position.x;
        enemyPos.y = transform.position.y;
        // set player position
        playerPos = player.transform.position;
        // get player minimap distance
        playerMinimapViewDistance = player.GetComponent<Player>().minimapDistance;
        // check if the icon is active
        if (icon)
        {            
            // deactivate the icon if the player is away
            if (Vector2.Distance(enemyPos, playerPos) > playerMinimapViewDistance)
            {
                enemyMinimapIcon.SetActive(false);
                icon = false;
            }
        }
        // check if the icon isnt active
        else if (!icon)
        {
            // activate the icon if the player is close
            if (Vector2.Distance(enemyPos, playerPos) <= playerMinimapViewDistance)
            {
                enemyMinimapIcon.SetActive(true);
                icon = true;
            }
        }        
    }

    void UpdateAggro()
    {
        // the enemy and player positions are already set by UpdateMinimapIcon
        if (!aggro && Vector2.Distance(enemyPos, playerPos) <= aggroRange)
        {
            //check if the enemy can see the player, line of sight
            playerDirection = (playerPos - enemyPos);
            lineOfSight = Physics2D.Raycast(enemyPos, playerDirection, aggroRange);
            if (lineOfSight.collider != null)
            {
                if (lineOfSight.collider.tag == "Player")
                {
                    // activate aggro
                    aggro = true;
                    los = true;
                    lastSeenPlayer = playerPos;
                    actionsCDtimer = actionsCD + Time.time;
                    //play idle aniamtion
                    enemyAnimator.SetInteger("State", 0);
                    // reset basic movement variables
                    moving = false;
                    direction = Vector2.zero;
                    waitedForMovement = 0;
                    //antiStuck = 0;
                }
            }
        }
        else if (aggro)
        {
            //check if the enemy can see the player, line of sight
            playerDirection = (playerPos - enemyPos);
            lineOfSight = Physics2D.Raycast(enemyPos, playerDirection, aggroRange);
            if (lineOfSight.collider != null && lineOfSight.collider.tag == "Player")
            {
                lastSeenPlayer = playerPos;
                los = true;
            }
            // player is out of sight
            else
            {
                // forget the player position if is dead
                if (player.GetComponent<Player>().dead)
                    lastSeenPlayer = enemyPos;
                // turn off aggro if the enemy has reached the last place where the player was seen
                if (Vector2.Distance(enemyPos, lastSeenPlayer) <= 0)
                {
                    aggro = false;
                    los = false;
                    // reset normal movement variables
                    waitedForMovement = Time.time + movementWait;
                    // check to flip
                    if (enemyRender.flipX)
                    {
                        weaponSlot.transform.localScale = new Vector3(1f, -1f, 1f);
                        weaponSlot.transform.eulerAngles = new Vector3(0f, 0f, -165f);
                    }
                    else if(!enemyRender.flipX)
                    {
                        weaponSlot.transform.localScale = new Vector3(1f, 1f, 1f);
                        weaponSlot.transform.eulerAngles = new Vector3(0f, 0f, -15f);
                    }
                    // check to sort order of renders
                    if (weaponRenders[0].sortingOrder < enemyRender.sortingOrder)
                    {
                        // weapon
                        weaponRenders[0].sortingOrder = enemyRender.sortingOrder + 1;
                        // hands
                        weaponRenders[1].sortingOrder = enemyRender.sortingOrder + 2;
                    }
                }
                else
                {
                    los = false;
                    //if (antiStuck > 1f)
                    //    antiStuck = 0f;
                }
            }
            // OPTIONAL stop chasing if player is out of range
            //else if (aggro && Vector2.Distance(enemyPos, playerPos) > aggroRange)
            //{
            //    aggro = false;
            //    waitedForMovement = Time.time + movementWait;
            //}
        }
    }

    void UpdateRandomMovement()
    {           
        if(!moving)
        {
            // play idle animation
            enemyAnimator.SetInteger("State", 0);
        }
        // chek to get a new direction
        if (!moving && waitedForMovement <= Time.time)
        {
            direction = RandomDirection();
            // direction was chosen
            if (direction != Vector3.zero)
            {
                moving = true;
                targetPosition = transform.position + direction;
                //antiStuck = 0;                
            }            
            // no direction chosed, reset the wait
            else
                waitedForMovement = Time.time + movementWait;
        }
        // move if a direction was chosed
        else if (moving)
        {
            // keep moving until you reached the destination
            if (Vector3.Distance(transform.position, targetPosition) > 0)
            {
                // move enemy towards the direction
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
                // play walking animation
                enemyAnimator.SetInteger("State", 1);
            }
            // destination reached, reset moving variables
            else
            {
                moving = false;
                direction = Vector2.zero;
                waitedForMovement = Time.time + movementWait;
                //play idle animation
                enemyAnimator.SetInteger("State", 0);
                // antiStuck = 0;
            }
        }
        // rotate the weapon a little
        if (!enemyRender.flipX)
        {
            // if looking right
            if (!rotateWeapon)
            {
                // check for limit
                if (weaponSlot.transform.eulerAngles.z <= 330)
                    rotateWeapon = true;
                else
                {
                    // rotate down a little
                    weaponSlot.transform.Rotate(Vector3.back * 30 * Time.deltaTime);
                }
            }
            else if (rotateWeapon)
            {
                // check for limit
                if (weaponSlot.transform.eulerAngles.z >= 345)
                    rotateWeapon = false;
                else
                {
                    //rotate up a little
                    weaponSlot.transform.Rotate(Vector3.back * -30 * Time.deltaTime);
                }
            }
        }
        else if (enemyRender.flipX)
        {
            // if looking left
            if (!rotateWeapon)
            {
                // check for limit
                if (weaponSlot.transform.eulerAngles.z >= 210)
                    rotateWeapon = true;
                else
                {
                    // rotate down a little
                    weaponSlot.transform.Rotate(Vector3.back * -30 * Time.deltaTime);
                }
            }
            else if (rotateWeapon)
            {
                // check for limit
                if (weaponSlot.transform.eulerAngles.z <= 195)
                    rotateWeapon = false;
                else
                {
                    //rotate up a little
                    weaponSlot.transform.Rotate(Vector3.back * 30 * Time.deltaTime);
                }
            }
        }
    }

    Vector2 RandomDirection()
    {
        //get the possible directions to move
        RaycastHit2D cardinal;
        bool Up = false, Down = false, Right = false, Left = false;
        // check for any collisions in the cardinal directions
        cardinal = Physics2D.Raycast(transform.position, Vector2.up, 1f);
        if (cardinal.collider != null)
            Up = true;
        cardinal = Physics2D.Raycast(transform.position, Vector2.down, 1f);
        if (cardinal.collider != null)
            Down = true;
        cardinal = Physics2D.Raycast(transform.position, Vector2.right, 1f);
        if (cardinal.collider != null)
            Right = true;
        cardinal = Physics2D.Raycast(transform.position, Vector2.left, 1f);
        if (cardinal.collider != null)
            Left = true;
        // get a direction
        int direction = Random.Range(0, 4);
        // try its chance to move
        switch (direction)
        {
            // return a direction if its chance succeds
            case 0:
                if (Random.value <= movementChance && !Up)
                    return Vector2.up;
                break;
            case 1:
                if (Random.value <= movementChance && !Down)
                    return Vector2.down;
                break;
            case 2:
                if (Random.value <= movementChance && !Right)
                {
                    // check to flip
                    if (enemyRender.flipX)
                    {
                        //flip the enemy
                        enemyRender.flipX = false;
                        weaponSlot.transform.localScale = new Vector3(1f, 1f, 1f);
                        weaponSlot.transform.eulerAngles = new Vector3(0f, 0f, -15f);
                    }
                    return Vector2.right;
                }
                break;
            case 3:
                if (Random.value <= movementChance && !Left)
                {
                    // check to flip
                    if (!enemyRender.flipX)
                    {
                        //flip the enemy
                        enemyRender.flipX = true;
                        weaponSlot.transform.localScale = new Vector3(1f, -1f, 1f);
                        weaponSlot.transform.eulerAngles = new Vector3(0f, 0f, -165f);
                    }
                    return Vector2.left;
                }
                break;
            default:                
                break;
        }
        // no direction succeded the movement chance
        return Vector2.zero;
    }    

    void UpdateChasing()
    {
        // the enemy and player positions are already set by UpdateMinimapIcon
        // follow the player if you have line of sight
        if (los)
        {
            // get player direction            
            playerDirection = player.transform.InverseTransformDirection(playerDirection);
            // get player angle
            float playerAngle = 0f;
            playerAngle = -1 * Mathf.Atan2(playerDirection.y, playerDirection.x) * Mathf.Rad2Deg;
            // rotate weapon to player angle
            weaponSlot.transform.rotation = Quaternion.AngleAxis(playerAngle, Vector3.back);
            // set the sorting order for the weapon            
            if (playerPos.y - enemyPos.y >= 0)
            {
                // weapon
                weaponRenders[0].sortingOrder = enemyRender.sortingOrder - 2;
                // hands
                weaponRenders[1].sortingOrder = enemyRender.sortingOrder - 1;
            }
            else if (playerPos.y - enemyPos.y < 0)
            {
                // weapon
                weaponRenders[0].sortingOrder = enemyRender.sortingOrder + 1;
                // hands
                weaponRenders[1].sortingOrder = enemyRender.sortingOrder + 2;
            }
            // flip to see the player
            if (playerPos.x - enemyPos.x >= 0)
            {
                weaponSlot.transform.localScale = new Vector3(1f, 1f, 1f);
                enemyRender.flipX = false;
            }
            else if (playerPos.x - enemyPos.x < 0)
            {
                weaponSlot.transform.localScale = new Vector3(1f, -1f, 1f);
                enemyRender.flipX = true;
            }            
            if (actionsCDtimer < Time.time)
            {
                //keep moving until you reached the player
                if (Vector2.Distance(enemyPos, playerPos) > minChaseDistance)
                {
                    //move enemy towards the player
                    transform.position = Vector3.MoveTowards(transform.position, playerPos, (speed * chaseSpeedMultiplier) * Time.deltaTime);
                    //play running aniamtion
                    enemyAnimator.SetInteger("State", 2);
                }
                //player reached
                else
                {
                    //play idle aniamtion
                    enemyAnimator.SetInteger("State", 0);
                    // make the enemy attack
                    weaponAnimator.SetInteger("State", 1);
                }
            }
            // wait for your cd
            else
                //play idle aniamtion
                enemyAnimator.SetInteger("State", 0);
        }
        // if you dont see the player, go to the last place you saw the player
        else if (!los)
        {
            //// flip to see last place the player was seen
            //if (lastSeenPlayer.x - enemyPos.x >= 0)
            //    enemyRender.flipX = false;
            //else if (lastSeenPlayer.x - enemyPos.x < 0)
            //    enemyRender.flipX = true;
            // keep moving until you reached the place
            if (Vector2.Distance(enemyPos, lastSeenPlayer) > 0)
            {
                // move enemy towards the place
                transform.position = Vector3.MoveTowards(transform.position, lastSeenPlayer, speed * Time.deltaTime);
                // play running aniamtion
                enemyAnimator.SetInteger("State", 1);
            }
            //player reached
            else
            {
                //play idle aniamtion
                enemyAnimator.SetInteger("State", 0);                
            }
        }
    }

    public void EndAttack()
    {
        //the weapon calls this so the enemy cant spam attacks
        actionsCDtimer = actionsCD + Time.time;
        // and dont get stuck if they kill you
        lastSeenPlayer = enemyPos;
    }

    public void RecieveDamageNumbers(float damage)
    {
        recieveDamage = true;
        damageRecieved = damage;
    }

    void RecieveDamage(float amount)
    {        
        // set the damaged animation
        damaged = true;
        enemyAnimator.SetInteger("State", 3);
        // interrupt the attack of the enemy
        InterruptAttack();
        // do the damage
        life -= amount;
        if (life < 0)
            life = 0;
        // update the life bar
        if (!lifeHUDElement.activeInHierarchy)
            lifeHUDElement.SetActive(true);
        if (damageText.text == "")
            damageText.text = amount.ToString("0");
        else
        {
            int dmgDone = int.Parse(damageText.text);
            amount += dmgDone;
            damageText.text = amount.ToString("0");
        }
        lifeBar.fillAmount = life / maxLife;
        // set the backlayer
        updateLifeBacklayer = true;
        lifeBacklayerTimer = Time.time + 1f;
        // make sure the enemy reacts even if the player is far away
        aggro = true;
        lastSeenPlayer = playerPos;
        // account for death
        if (life <= 0)
        {
            Death();
            GivePoints(scoreAmount);
        }
        // reset the damage numbers
        recieveDamage = false;
        damageRecieved = 0;
    }

    void InterruptAttack()
    {
        inAnimation = false;
        EndAttack();
        weaponAnimator.SetInteger("State", 0);
        if(weapon.weaponType == EnemyWeapon.WeaponType.Sword)
            weaponSlot.GetComponentInChildren<Collider2D>().enabled = false;
    }

    void EndRecieveDamage()
    {
        //enemyAnimator.SetInteger("State", 0);
        damaged = false;
        // update life bar              
    }

    void UpdateLifeBacklayer()
    {
        // check if time has passed since you recieve damage
        if (updateLifeBacklayer && lifeBacklayerTimer < Time.time)
        {
            // empty the backlayer until you reach the life bar
            if (lifeBarBacklayer.fillAmount > lifeBar.fillAmount)
                lifeBarBacklayer.fillAmount -= 1f * Time.deltaTime;
            else
            {
                updateLifeBacklayer = false;
                damageText.text = "";
            }
        }
        // check to refill the life backlayer
        else if (!updateLifeBacklayer)
        {
            if (lifeBarBacklayer.fillAmount < lifeBar.fillAmount)
                lifeBarBacklayer.fillAmount = lifeBar.fillAmount;
        }
        // check to disable on death
        if (dead && !updateLifeBacklayer)
            lifeHUDElement.SetActive(false);
    }

    void Death()
    {
        // setup dead
        dead = true;
        GetComponent<Animator>().SetBool("Death", true);
        GetComponent<BoxCollider2D>().enabled = false;
        enemyMinimapIcon.SetActive(false);
        // TODO: drop weapon, meanwhile just deactivate it
        weaponSlot.SetActive(false);
        // reset movement variables
        //antiStuck = 0f;
        moving = false;
        direction = Vector2.zero;
        waitedForMovement = 0;
        // reset aggro variables
        aggro = false;
        los = false;
        lastSeenPlayer = Vector2.zero;
    }

    void GivePoints(int amount)
    {
        player.GetComponent<Player>().playerScore += amount;
        player.GetComponent<Player>().updateScore = true;
    }

    //void OnTriggerEnter2D(Collider2D trigger)
    //{
    //    // to prevent the message from firing twice in the same frame
    //    if (isColliding)
    //        return;
    //    isColliding = true;
    //    // collision with player melee weapon
    //    if (trigger.tag == "PlayerWeapon")
    //    {
    //        RecieveDamage(trigger.GetComponent<Weapon>().basicAttackDamage);
    //        if (life <= 0)
    //        {
    //            Death();
    //            GivePoints(scoreAmount);
    //        }
    //    }
    //    //// collision with player ranged weapon
    //    if (trigger.tag == "PlayerProjectile")
    //    {
    //        RecieveDamage(trigger.GetComponent<Projectile>().gunDamage);
    //        if (life <= 0)
    //        {
    //            Death();
    //            GivePoints(scoreAmount);
    //        }
    //    }
    //}

    void OnCollisionEnter2D(Collision2D collider)
    {
        // collision wile moving, reset movement
        if(moving)
        {
            moving = false;
            direction = Vector2.zero;
            waitedForMovement = Time.time + movementWait;
        }        
    }

    void OnCollisionStay2D(Collision2D collider)
    {
        // sometimes colliders can get stuck, this is for safety
        // collision wile moving, reset movement
        if (antiStuck < 1f && moving)
            antiStuck += 1 * Time.deltaTime;
        else if (antiStuck >= 1f && moving)
        {
            antiStuck = 0f;
            moving = false;
            direction = Vector2.zero;
            waitedForMovement = Time.time + movementWait;
        }
        //if (antiStuck < 3f && aggro)
        //    antiStuck += 1 * Time.deltaTime;
        //else if (antiStuck >= 3f && aggro)
        //{
        //    antiStuck = 0f;
        //    aggro = false;
        //    los = false;
        //    lastSeenPlayer = enemyPos;
        //    waitedForMovement = Time.time + movementWait;
        //    print("antiStuck");            
        //}
    }

    void OnCollisionExit2D(Collision2D collider)
    {
        // reset for future uses
        //antiStuck = 0f;
    }
}


//  NOT NEEDED IA WIP  //

//else if (aggro)
//{
//    if (!antiCorner)
//        UpdateChasing();
//    else if (antiCorner)
//        UpdateAntiCorner();
//}

//void SetupAntiCorner()
//{
//    //set auxiliar variables
//    RaycastHit2D ordinal;
//    Vector2 direction = Vector2.zero;
//    bool NW = false, NE = false, SW = false, SE= false;
//    float NWdist = 0, NEdist = 0, SWdist = 0, SEdist = 0;
//    Vector2 newPlayerDirection = Vector2.zero;
//    int usefulOrdinals = 0;
//    // raycast in the ordinal directions and check to avoid any collisions with walls
//    direction.x = -1;
//    direction.y = 1;
//    ordinal = Physics2D.Raycast(enemyPos, direction, 1f);
//    if (ordinal.collider == null || (ordinal.collider != null && ordinal.collider.tag != "Wall"))
//    {
//        // check if using this ordinal you can see the player
//        newPlayerDirection = (playerPos - (enemyPos + direction));
//        ordinal = Physics2D.Raycast(enemyPos + direction, newPlayerDirection);
//        if (ordinal.collider != null && ordinal.collider.tag == "Player")
//        {
//            NW = true;
//            NWdist = ordinal.distance;
//            usefulOrdinals++;
//        }
//    }
//    direction.x = 1;
//    direction.y = 1;
//    ordinal = Physics2D.Raycast(enemyPos, direction, 1f);
//    if (ordinal.collider == null || (ordinal.collider != null && ordinal.collider.tag != "Wall"))
//    {
//        // check if using this ordinal you can see the player
//        newPlayerDirection = (playerPos - (enemyPos + direction));
//        ordinal = Physics2D.Raycast(enemyPos + direction, newPlayerDirection);
//        if (ordinal.collider != null && ordinal.collider.tag == "Player")
//        {
//            NE = true;
//            NEdist = ordinal.distance;
//            usefulOrdinals++;
//        }
//    }
//    direction.x = -1;
//    direction.y = -1;
//    ordinal = Physics2D.Raycast(enemyPos, direction, 1f);
//    if (ordinal.collider == null || (ordinal.collider != null && ordinal.collider.tag != "Wall"))
//    {
//        // check if using this ordinal you can see the player
//        newPlayerDirection = (playerPos - (enemyPos + direction));
//        ordinal = Physics2D.Raycast(enemyPos + direction, newPlayerDirection);
//        if (ordinal.collider != null && ordinal.collider.tag == "Player")
//        {
//            SW = true;
//            SWdist = ordinal.distance;
//            usefulOrdinals++;
//        }
//    }
//    direction.x = 1;
//    direction.y = -1;
//    ordinal = Physics2D.Raycast(enemyPos, direction, 1f);
//    if (ordinal.collider == null || (ordinal.collider != null && ordinal.collider.tag != "Wall"))
//    {
//        // check if using this ordinal you can see the player
//        newPlayerDirection = (playerPos - (enemyPos + direction));
//        ordinal = Physics2D.Raycast(enemyPos + direction, newPlayerDirection);
//        if (ordinal.collider != null && ordinal.collider.tag == "Player")
//        {
//            SE = true;
//            SEdist = ordinal.distance;
//            usefulOrdinals++;
//        }
//    }
//    //
//    print("PLAYER COLLISIONS");
//    print("NW = " + NW);
//    print("NE = " + NE);
//    print("SW = " + SW);
//    print("SE = " + SE);
//    //
//    // if there were no good ordinals, forget the last seen position of the player, so the enemy will lose aggro
//    if (usefulOrdinals == 0)
//    {
//        lastSeenPlayer = enemyPos;
//        // reset the anticorner
//        antiCorner = false;
//        //
//        print(usefulOrdinals);
//    }
//    else
//    {
//        // if there is just 1 ordinal, check to se at wich cardinal to move
//        if(usefulOrdinals == 1)
//        {
//            print(usefulOrdinals);                
//        }
//        // if there is 2 ordinal, check wich is closer to the player
//        else if(usefulOrdinals == 2)
//        {
//            print(usefulOrdinals);
//        }
//        // any more ordinals means the enemy isnt really stuck;
//        else
//            antiCorner = false;
//    }
//}

//void UpdateAntiCorner()
//{

//}

//// collision while chasing, setup the avoid corner movement
//if (aggro && !antiCorner && collider.gameObject.tag == "Wall")
//{
//    SetupAntiCorner();
//    antiCorner = true;
//}
