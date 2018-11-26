using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    SortingGroup playerGroup;
    SpriteRenderer playerRender;
    Animator playerAnimator;
    Animator[] weaponsAnimator;
    CameraControl playerCamera;
    // to recieve damage only once per trigger enter
    bool recieveDamage;
    float damageRecieved;
    bool recieveShake;
    Vector3 enemyPosRecieved;
    float magnitudRecieved;
    float durationRecieved;

    [Header("Misc")]
    public Text levelText;
    public Text scoreText;
    [HideInInspector]
    public bool updateScore;
    [HideInInspector]
    public int playerScore;
    public float maxMinimapDistance;
    public float minMinimapDistance;
    [HideInInspector]
    public float minimapDistance;

    [Header("Life")]
    public Image lifeBarBackground;
    public Image lifeBarBacklayer;
    public Image lifeBar;
    public SpriteRenderer overlay;
    Color overlayColor;
    public float maxLife;
    public float life;
    bool updateLifeBacklayer;
    float lifeBacklayerTimer;
    bool damaged;
    [HideInInspector]
    public bool dead;

    [Header("Stamina")]
    public Image staminaBarBackground;
    public Image staminaBarBacklayer;
    public Image staminaBar;
    public float maxStamina;
    public float stamina;
    bool updateStaminaBacklayer;
    float staminaBacklayerTimer;
    bool staminaRecovery;
    public float staminaRecBySec;
    bool tired;
    float tiredTimer;
    public float tiredTimerSeconds;

    [Header("Movement")]
    public float speed;
    Vector2 movementDirection;
    float movementMagnitude;
    public float runSpeedMultiplier;
    public float tiredSpeedMultiplier;
    public float staminaBySecRan;   
    [HideInInspector]
    public bool inDodgeRoll;
    public float staminaByDodgeRoll;
    public float dodgeRollDistance;
    Vector3 dodgeRollDirection;
    public float DodgeRollSpeed;
    [HideInInspector]
    public bool iframes;

    [Header("Actions")]
    public GameObject leftActionsElement;
    bool leftActions;
    public GameObject rightActionsElement;
    bool rightActions;
    bool actionChangePossible;

    [Header("Weapons")]
    public Transform Crosshair;
    public GameObject leftWeaponSlot;
    Weapon leftWeapon;
    SpriteRenderer[] leftWeaponRenders;
    bool leftWeaponEquipded;
    public GameObject rightWeaponSlot;
    Weapon rightWeapon;
    SpriteRenderer[] rightWeaponRenders;
    bool rightWeaponEquipded;
    [HideInInspector]
    public bool inAnimation;
    [HideInInspector]
    public bool outAnimation;
    [HideInInspector]
    public bool checkAmmo;

    void Start()
    {
        // setup hud elements of player
        lifeBarBackground.fillAmount = maxLife / lifeBarBackground.rectTransform.rect.width + 0.007f;
        lifeBar.fillAmount = life / lifeBar.rectTransform.rect.width;
        lifeBarBacklayer.fillAmount = life / lifeBarBacklayer.rectTransform.rect.width;
        staminaBarBackground.fillAmount = maxStamina / staminaBarBackground.rectTransform.rect.width + 0.007f;
        staminaBar.fillAmount = stamina / staminaBar.rectTransform.rect.width;
        staminaBarBacklayer.fillAmount = stamina / staminaBarBacklayer.rectTransform.rect.width;
        overlayColor = new Color(1f, 1f, 1f, 0f);
        // get the player camera
        playerCamera = transform.Find("Player_Camera").GetComponent<CameraControl>();
        // setup animator controllers
        playerAnimator = GetComponent<Animator>();
        weaponsAnimator = new Animator[2];
        weaponsAnimator[0] = rightWeaponSlot.GetComponentInChildren<Animator>();
        weaponsAnimator[1] = leftWeaponSlot.GetComponentInChildren<Animator>();
        // setup weapons
        leftWeapon = leftWeaponSlot.GetComponentInChildren<Weapon>();
        rightWeapon = rightWeaponSlot.GetComponentInChildren<Weapon>();
        // setup hud elements of weapons
        checkAmmo = true;
        UpdateAmmo();
        // setup renders
        leftWeaponRenders = leftWeaponSlot.GetComponentsInChildren<SpriteRenderer>();
        rightWeaponRenders = rightWeaponSlot.GetComponentsInChildren<SpriteRenderer>();
        playerRender = GetComponent<SpriteRenderer>();
        playerGroup = GetComponent<SortingGroup>();
        // setup default active weapon
        rightWeaponRenders[0].enabled = true;
        rightWeaponRenders[1].enabled = true;
        rightWeaponEquipded = true;
        leftWeaponRenders[0].enabled = false;
        leftWeaponRenders[1].enabled = false;
        leftWeaponEquipded = false;
    }

    void Update()
    {
        // changes the sorting order for the whole player
        UpdateSortingOrder();
        // if attacked recieve damage
        if (recieveDamage)
            RecieveDamage(damageRecieved, recieveShake, enemyPosRecieved, magnitudRecieved, durationRecieved);
        if (!dead)
        {
            if (!damaged)
            {
                // rotates the crosshair and weapons around the player
                UpdateCrosshair();
                // check at wich speed to move and apply movement
                UpdateMovement();
                // check for dodging and move the player
                UpdateDodging();
                // change between active weapon
                UpdateWeapon();
                // controls wich attacks or skills to activate
                UpdateAttacks();
                // manage stamina recovery and tired effect
                UpdateStamina();
            }
            // check to activate or deactivate the actions hud elements
            UpdateActions();
            // update the ammo count when needed
            UpdateAmmo();
            // update the score when needed
            UpdateScore();
        }
        // updates the life and stamina bars backlayers
        UpdateBacklayers();
        // updates the black overlay size and the minimap view distance
        UpdateFOV();
    }

    void UpdateSortingOrder()
    {
        // based on the y axis
        playerGroup.sortingOrder = -(int)((transform.position.y + 0.2) * 10);
    }

    void UpdateCrosshair()
    {
        float crosshairAngle = 0f;
        // setup for analog stick
        if (Input.GetAxis("RightAnalogX") != 0 || Input.GetAxis("RightAnalogY") != 0)
        {
            // to read full left
            float dirx = 0.00001f;
            float diry = 0.00001f;
            dirx += Input.GetAxis("RightAnalogX");
            diry += Input.GetAxis("RightAnalogY");
            Mathf.Clamp(dirx, -1f, 1f);
            Mathf.Clamp(diry, -1f, 1f);
            crosshairAngle = -1 * Mathf.Atan2(diry, dirx) * Mathf.Rad2Deg;
        }
        // setup for mouse
        else if (Input.GetAxis("MouseX") != 0 || Input.GetAxis("MouseY") != 0)
        {
            Vector3 mousePos;
            mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = transform.position.z;
            Vector3 mouseVector;
            mouseVector = (mousePos - transform.position).normalized;
            crosshairAngle = -1 * Mathf.Atan2(mouseVector.y, mouseVector.x) * Mathf.Rad2Deg;
        }
        // check if the angle was changed
        if (crosshairAngle != 0)
        {
            // rotate the crosshair
            Crosshair.rotation = Quaternion.AngleAxis(crosshairAngle, Vector3.back);
            // check if the player isnt attacking to avoid animation issues
            if (!inAnimation)
            {
                // rotate the weapons
                leftWeaponSlot.transform.rotation = Crosshair.rotation;
                rightWeaponSlot.transform.rotation = Crosshair.rotation;
                // set the sorting order of the weapons
                if (crosshairAngle <= 0 && leftWeaponRenders[0].sortingOrder > playerRender.sortingOrder)
                {
                    // weapons
                    leftWeaponRenders[0].sortingOrder = playerRender.sortingOrder - 2;
                    rightWeaponRenders[0].sortingOrder = playerRender.sortingOrder - 2;
                    // hands
                    leftWeaponRenders[1].sortingOrder = playerRender.sortingOrder - 1;
                    rightWeaponRenders[1].sortingOrder = playerRender.sortingOrder - 1;
                }
                else if (crosshairAngle > 0 && leftWeaponRenders[0].sortingOrder < playerRender.sortingOrder)
                {
                    // weapons
                    leftWeaponRenders[0].sortingOrder = playerRender.sortingOrder + 1;
                    rightWeaponRenders[0].sortingOrder = playerRender.sortingOrder + 1;
                    // hands
                    leftWeaponRenders[1].sortingOrder = playerRender.sortingOrder + 2;
                    rightWeaponRenders[1].sortingOrder = playerRender.sortingOrder + 2;
                }
                // set the alignment of the weapons and player
                if ((crosshairAngle >= -90f && crosshairAngle <= 90f) && leftWeaponSlot.transform.localScale.y < 0)
                {
                    leftWeaponSlot.transform.localScale = new Vector3(1f, 1f, 1f);
                    rightWeaponSlot.transform.localScale = new Vector3(1f, 1f, 1f);
                    playerRender.flipX = false;
                }
                else if ((crosshairAngle < -90f || crosshairAngle > 90f) && leftWeaponSlot.transform.localScale.y > 0)
                {
                    leftWeaponSlot.transform.localScale = new Vector3(1f, -1f, 1f);
                    rightWeaponSlot.transform.localScale = new Vector3(1f, -1f, 1f);
                    playerRender.flipX = true;
                }
            }
        }
        // to keep the weapons rotated with the crosshair after an animation ends
        else if (crosshairAngle == 0 && !inAnimation)
        {
            // rotate the weapons
            leftWeaponSlot.transform.rotation = Crosshair.rotation;
            rightWeaponSlot.transform.rotation = Crosshair.rotation;
            // set the sorting order of the weapons
            if (Crosshair.rotation.z <= 0 && leftWeaponRenders[0].sortingOrder < playerRender.sortingOrder)
            {
                // weapons
                leftWeaponRenders[0].sortingOrder = playerRender.sortingOrder + 1;
                rightWeaponRenders[0].sortingOrder = playerRender.sortingOrder + 1;
                // hands
                leftWeaponRenders[1].sortingOrder = playerRender.sortingOrder + 2;
                rightWeaponRenders[1].sortingOrder = playerRender.sortingOrder + 2;
            }
            else if (Crosshair.rotation.z > 0 && leftWeaponRenders[0].sortingOrder > playerRender.sortingOrder)
            {
                // weapons
                leftWeaponRenders[0].sortingOrder = playerRender.sortingOrder - 2;
                rightWeaponRenders[0].sortingOrder = playerRender.sortingOrder - 2;
                // hands
                leftWeaponRenders[1].sortingOrder = playerRender.sortingOrder - 1;
                rightWeaponRenders[1].sortingOrder = playerRender.sortingOrder - 1;
            }
            // set the alignment of the weapons and player
            //print(Crosshair.rotation.z);
            if ((Crosshair.rotation.z >= -0.707f && Crosshair.rotation.z <= 0.707f) && leftWeaponSlot.transform.localScale.y < 0)
            {
                leftWeaponSlot.transform.localScale = new Vector3(1f, 1f, 1f);
                rightWeaponSlot.transform.localScale = new Vector3(1f, 1f, 1f);
                GetComponent<SpriteRenderer>().flipX = false;
            }
            else if ((Crosshair.rotation.z < -0.707f || Crosshair.rotation.z > 0.707f) && leftWeaponSlot.transform.localScale.y > 0)
            {
                leftWeaponSlot.transform.localScale = new Vector3(1f, -1f, 1f);
                rightWeaponSlot.transform.localScale = new Vector3(1f, -1f, 1f);
                GetComponent<SpriteRenderer>().flipX = true;
            }
        }
    }

    void UpdateWeapon()
    {
        if (!inAnimation)
        {
            if (Input.GetButtonDown("SwitchWeapons"))
            {
                // switch active weapon
                if (rightWeaponEquipded)
                {
                    leftWeaponEquipded = true;
                    leftWeaponRenders[0].enabled = true;
                    leftWeaponRenders[1].enabled = true;
                    rightWeaponEquipded = false;
                    rightWeaponRenders[0].enabled = false;
                    rightWeaponRenders[1].enabled = false;

                }
                else if (leftWeaponEquipded)
                {
                    rightWeaponEquipded = true;
                    rightWeaponRenders[0].enabled = true;
                    rightWeaponRenders[1].enabled = true;
                    leftWeaponEquipded = false;
                    leftWeaponRenders[0].enabled = false;
                    leftWeaponRenders[1].enabled = false;
                }
            }
        }
    }

    void UpdateAttacks()
    {
        if (!tired && !inAnimation)
        {
            if (outAnimation)
            {
                // reset the state of the weapons animators so they dont get stuck
                weaponsAnimator[0].SetInteger("State", 0);
                weaponsAnimator[1].SetInteger("State", 0); 
                // reset the weapons hud icons color
                rightActionsElement.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
                leftActionsElement.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
                outAnimation = false;
            }
            else if (Input.GetButtonDown("RightBasicAttack"))
            {
                // check if you are using a gun and have ammo or just using another weapon
                if ((rightWeapon.weaponType == Weapon.WeaponType.Gun && rightWeapon.ammunition > 0) ||
                     rightWeapon.weaponType != Weapon.WeaponType.Gun)
                {
                    if (rightWeaponEquipded)
                    {
                        weaponsAnimator[0].SetInteger("State", 1);
                        rightActionsElement.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.4f);
                    }
                    else if (leftWeaponEquipded)
                    {
                        // switch active weapon
                        rightWeaponEquipded = true;
                        rightWeaponRenders[0].enabled = true;
                        rightWeaponRenders[1].enabled = true;
                        leftWeaponEquipded = false;
                        leftWeaponRenders[0].enabled = false;
                        leftWeaponRenders[1].enabled = false;
                        weaponsAnimator[0].SetInteger("State", 1);
                        rightActionsElement.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.4f);
                    }
                }
            }
            else if (Input.GetButtonDown("LeftBasicAttack"))
            {
                // check if you are using a gun and have ammo or just using another weapon
                if ((leftWeapon.weaponType == Weapon.WeaponType.Gun && leftWeapon.ammunition > 0) ||
                     leftWeapon.weaponType != Weapon.WeaponType.Gun)
                {
                    if (leftWeaponEquipded)
                    {
                        weaponsAnimator[1].SetInteger("State", 1);
                        leftActionsElement.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.4f);
                    }
                    else if (rightWeaponEquipded)
                    {
                        // switch active weapon
                        leftWeaponEquipded = true;
                        leftWeaponRenders[0].enabled = true;
                        leftWeaponRenders[1].enabled = true;
                        rightWeaponEquipded = false;
                        rightWeaponRenders[0].enabled = false;
                        rightWeaponRenders[1].enabled = false;
                        weaponsAnimator[1].SetInteger("State", 1);
                        leftActionsElement.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.4f);
                    }
                }
            }
        }
    }

    void UpdateAmmo()
    {
        if (checkAmmo)
        {
            if (leftWeapon.weaponType == Weapon.WeaponType.Gun)
                leftActionsElement.GetComponentInChildren<Text>().text = leftWeapon.ammunition.ToString("0");
            else
                leftActionsElement.GetComponentInChildren<Text>().text = "";
            if (rightWeapon.weaponType == Weapon.WeaponType.Gun)
                rightActionsElement.GetComponentInChildren<Text>().text = leftWeapon.ammunition.ToString("0");
            else
                rightActionsElement.GetComponentInChildren<Text>().text = "";
            checkAmmo = false;
        }
    }

    void UpdateMovement()
    {
        // check for moving
        if ((Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0) && !inDodgeRoll)
        {
            // running
            if (Input.GetButton("Run") && !tired && !inAnimation)
            {
                playerAnimator.SetInteger("State", 2);
                Movement(speed * runSpeedMultiplier);
                LoseStamina(staminaBySecRan * Time.deltaTime);
                // dont allow to recover stamina
                staminaRecovery = false;
            }
            else if (!tired)
            {
                playerAnimator.SetInteger("State", 1);
                Movement(speed);
            }
            else if (tired)
            {
                playerAnimator.SetInteger("State", 3);
                Movement(speed * tiredSpeedMultiplier);
            }
        }
        // idle
        else
        {
            playerAnimator.SetInteger("State", 0);
        }
    }

    void Movement(float speed)
    {
        // get input
        movementDirection.x = Input.GetAxis("Horizontal");
        movementDirection.y = Input.GetAxis("Vertical");
        // check for diagonal movement using keyboard but also allow slow movement of joystick
        //get the length of the vector and see if it's greater than 1. If so, divide the movement vector by the length.
        movementMagnitude = movementDirection.magnitude;
        if (movementMagnitude > 1)
            movementDirection /= movementMagnitude;
        // movement
        movementDirection = movementDirection * Time.deltaTime * speed;
        transform.Translate(movementDirection.x, movementDirection.y, 0);
    }

    void UpdateDodging()
    {
        // dodge roll
        if (Input.GetButtonDown("DodgeRoll") && !tired)
        {
            if (!inAnimation && !inDodgeRoll)
            {
                {
                    //InitDodgeRoll(); the animation calls this
                    //playerAnimator.SetInteger("State", 4);
                    playerAnimator.SetTrigger("DodgeRoll");
                    LoseStamina(staminaByDodgeRoll);
                }
            }
        }
        // rolling
        else if (inDodgeRoll)
        {
            // move the player
            transform.position += dodgeRollDirection * dodgeRollDistance * Time.deltaTime;
            // rotate player weapons
            // if looking right
            if (!GetComponent<SpriteRenderer>().flipX)
            {
                leftWeaponSlot.transform.Rotate(new Vector3(0f, 0f, -360 * Time.deltaTime * DodgeRollSpeed));
                rightWeaponSlot.transform.Rotate(new Vector3(0f, 0f, -360 * Time.deltaTime * DodgeRollSpeed));
            }
            //if looking left
            else if (GetComponent<SpriteRenderer>().flipX)
            {
                leftWeaponSlot.transform.Rotate(new Vector3(0f, 0f, 360 * Time.deltaTime * DodgeRollSpeed));
                rightWeaponSlot.transform.Rotate(new Vector3(0f, 0f, 360 * Time.deltaTime * DodgeRollSpeed));
            }
        }
    }

    void InitDodgeRoll()
    {
        inAnimation = true;
        inDodgeRoll = true;
        // get direction of roll
        Vector3 movement = new Vector3(0f, 0f, 0f);
        // if player is moving thats the direction
        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
            movement.x = Input.GetAxis("Horizontal");
            movement.y = Input.GetAxis("Vertical");
            movement.z = transform.position.z;
            dodgeRollDirection = Vector3.Normalize((transform.position + movement) - transform.position);
            // override the flipping of the crosshair
            if (movement.x < 0)
            {
                // turn the player
                playerRender.flipX = true;
                // rotate the weapons
                leftWeaponSlot.transform.rotation = Quaternion.AngleAxis(180f, Vector3.back);
                rightWeaponSlot.transform.rotation = leftWeaponSlot.transform.rotation;
                // flip weapons
                leftWeaponSlot.transform.localScale = new Vector3(1f, -1f, 1f);
                rightWeaponSlot.transform.localScale = new Vector3(1f, -1f, 1f);
            }
            else if (movement.x > 0)
            {
                // turn the player
                playerRender.flipX = false;
                // rotate the weapons
                leftWeaponSlot.transform.rotation = Quaternion.AngleAxis(0f, Vector3.back);
                rightWeaponSlot.transform.rotation = leftWeaponSlot.transform.rotation;
                //flip weapons
                leftWeaponSlot.transform.localScale = new Vector3(1f, 1f, 1f);
                rightWeaponSlot.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }
        // if player isnt moving the direction is the crosshair
        else
        {
            dodgeRollDirection = Vector3.Normalize(Crosshair.Find("Player_Crosshair_Sprite").position - transform.position);
        }
        // setup dodgeroll speed
        playerAnimator.speed = DodgeRollSpeed;
    }

    void StartIframes()
    {
        iframes = true;
    }

    void EndIframes()
    {
        iframes = false;
    }

    void EndDodgeRoll()
    {
        inAnimation = false;
        inDodgeRoll = false;
        iframes = false;
        playerAnimator.ResetTrigger("DodgeRoll");
        // reset the roll direction
        dodgeRollDirection = Vector3.zero;
        // fix the flip
        if (Crosshair.rotation.z >= -0.707f && Crosshair.rotation.z <= 0.707f)
        {
            playerRender.flipX = false;
        }
        else if (Crosshair.rotation.z < -0.707f || Crosshair.rotation.z > 0.707f)
        {
            playerRender.flipX = true;
        }
        // return to normal speed
        playerAnimator.speed = 1;
    }

    void UpdateActions()
    {
        // trick to use the dpad axis as buttons
        if (Input.GetAxis("RightActions") == 0 && Input.GetAxis("LeftActions") == 0 && !actionChangePossible)
        {
            actionChangePossible = true;
            leftActionsElement.transform.Find("Player_Left_Actions_Inactive_Background").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
            rightActionsElement.transform.Find("Player_Right_Actions_Inactive_Background").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
        }
        if (!leftActions && Input.GetAxis("LeftActions") == -1 && actionChangePossible)
        {
            actionChangePossible = false;
            leftActions = true;
            leftActionsElement.transform.Find("Player_Left_Actions_Active_Background").Find("Player_Left_Actions_1st_Image").gameObject.SetActive(false);
            leftActionsElement.transform.Find("Player_Left_Actions_Active_Background").Find("Player_Left_Actions_2nd_Image").gameObject.SetActive(true);
            leftActionsElement.transform.Find("Player_Left_Actions_Inactive_Background").Find("Player_Left_Actions_1st_Image").gameObject.SetActive(true);
            leftActionsElement.transform.Find("Player_Left_Actions_Inactive_Background").Find("Player_Left_Actions_2nd_Image").gameObject.SetActive(false);
            leftActionsElement.transform.Find("Player_Left_Actions_Inactive_Background").GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.4f);
        }
        else if (leftActions && Input.GetAxis("LeftActions") == -1 && actionChangePossible)
        {
            actionChangePossible = false;
            leftActions = false;
            leftActionsElement.transform.Find("Player_Left_Actions_Active_Background").Find("Player_Left_Actions_1st_Image").gameObject.SetActive(true);
            leftActionsElement.transform.Find("Player_Left_Actions_Active_Background").Find("Player_Left_Actions_2nd_Image").gameObject.SetActive(false);
            leftActionsElement.transform.Find("Player_Left_Actions_Inactive_Background").Find("Player_Left_Actions_1st_Image").gameObject.SetActive(false);
            leftActionsElement.transform.Find("Player_Left_Actions_Inactive_Background").Find("Player_Left_Actions_2nd_Image").gameObject.SetActive(true);
            leftActionsElement.transform.Find("Player_Left_Actions_Inactive_Background").GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.4f);
        }
        if (!rightActions && Input.GetAxis("RightActions") == 1 && actionChangePossible)
        {
            actionChangePossible = false;
            rightActions = true;
            rightActionsElement.transform.Find("Player_Right_Actions_Active_Background").Find("Player_Right_Actions_1st_Image").gameObject.SetActive(false);
            rightActionsElement.transform.Find("Player_Right_Actions_Active_Background").Find("Player_Right_Actions_2nd_Image").gameObject.SetActive(true);
            rightActionsElement.transform.Find("Player_Right_Actions_Inactive_Background").Find("Player_Right_Actions_1st_Image").gameObject.SetActive(true);
            rightActionsElement.transform.Find("Player_Right_Actions_Inactive_Background").Find("Player_Right_Actions_2nd_Image").gameObject.SetActive(false);
            rightActionsElement.transform.Find("Player_Right_Actions_Inactive_Background").GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.4f);
        }
        else if (rightActions && Input.GetAxis("RightActions") == 1 && actionChangePossible)
        {
            actionChangePossible = false;
            rightActions = false;
            rightActionsElement.transform.Find("Player_Right_Actions_Active_Background").Find("Player_Right_Actions_1st_Image").gameObject.SetActive(true);
            rightActionsElement.transform.Find("Player_Right_Actions_Active_Background").Find("Player_Right_Actions_2nd_Image").gameObject.SetActive(false);
            rightActionsElement.transform.Find("Player_Right_Actions_Inactive_Background").Find("Player_Right_Actions_1st_Image").gameObject.SetActive(false);
            rightActionsElement.transform.Find("Player_Right_Actions_Inactive_Background").Find("Player_Right_Actions_2nd_Image").gameObject.SetActive(true);
            rightActionsElement.transform.Find("Player_Right_Actions_Inactive_Background").GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.4f);
        }
    }

    void UpdateStamina()
    {
        // check for the tired effect
        if (!tired && stamina <= 0)
        {
            tired = true;
            tiredTimer = Time.time + tiredTimerSeconds;
        }

        // recover from tired
        if (tired && tiredTimer <= Time.time)
        {
            tired = false;
            staminaRecovery = true;
        }

        // recover stamina
        if (!tired && staminaRecovery && stamina < maxStamina)
        {
            GainStamina(staminaRecBySec * Time.deltaTime);
        }

        // update the stamina after everything got checked
        staminaBar.fillAmount = stamina / staminaBar.rectTransform.rect.width;
        if (inAnimation)
            staminaRecovery = false;
        else
            staminaRecovery = true;
    }

    public void LoseStamina(float amount)
    {
        // lose the amount
        stamina -= amount;
        if (stamina < 0)
            stamina = 0;
        // update the stamina backlayer
        updateStaminaBacklayer = true;
        staminaBacklayerTimer = Time.time + 1f;
    }

    public void GainStamina(float amount)
    {
        stamina += amount;
        if (stamina > maxStamina)
            stamina = maxStamina;
    }

    void UpdateScore()
    {
        if (updateScore)
        {
            scoreText.text = playerScore.ToString("0");
            updateScore = false;
        }
    }

    void UpdateBacklayers()
    {
        // check if time has passed since you recieve damage
        if(updateLifeBacklayer && lifeBacklayerTimer < Time.time)
        {
            // empty the backlayer until you reach the life bar
            if (lifeBarBacklayer.fillAmount > lifeBar.fillAmount)
                lifeBarBacklayer.fillAmount -= 1f * Time.deltaTime;
            else            
                updateLifeBacklayer = false;            
        }
        // check to refill the life backlayer
        else if(!updateLifeBacklayer)
        {
            if (lifeBarBacklayer.fillAmount < lifeBar.fillAmount)
                lifeBarBacklayer.fillAmount = lifeBar.fillAmount;
        }
        // check if time has passed since you wasted stamina
        if (updateStaminaBacklayer && staminaBacklayerTimer < Time.time)
        {            
            // empty the backlayer until you reach the life bar
            if (staminaBarBacklayer.fillAmount > staminaBar.fillAmount)
                staminaBarBacklayer.fillAmount -= 1f * Time.deltaTime;
            else
                updateStaminaBacklayer = false;
        }
        // check to refill the stamina backlayer
        else if (!updateStaminaBacklayer)
        {
            if (staminaBarBacklayer.fillAmount < staminaBar.fillAmount)                            
                staminaBarBacklayer.fillAmount = staminaBar.fillAmount;            
        }
    }

    void UpdateFOV()
    {
        float lifePercent = life / maxLife;
        float lifePercentAbsolute = Mathf.Abs(lifePercent - 1f);
        if (overlayColor.a < lifePercentAbsolute)
        {
            // smoothly reduce fov on damage taken
            overlayColor.a += lifePercentAbsolute * Time.deltaTime;
            overlay.color = overlayColor;
        }
        // update the minimap view distance
        minimapDistance = maxMinimapDistance * lifePercent;
        // clamp minimap distance
        minimapDistance = Mathf.Clamp(minimapDistance, minMinimapDistance, maxMinimapDistance);
    }

    public void RecieveDamageNumbers(float damage, bool shake, Vector3 enemyPos, float magnitud, float duration)
    {
        recieveDamage = true;
        damageRecieved = damage;
        if(shake)
        {
            recieveShake = true;
            enemyPosRecieved = enemyPos;
            magnitudRecieved = magnitud;
            durationRecieved = duration;
        }
    }

    void RecieveDamage(float amount, bool shake, Vector3 enemyPos, float magnitud, float duration)
    {
        // set the damaged animation
        damaged = true;
        playerAnimator.SetInteger("State", 5);
        // interrupt the weapon animation and dodge roll
        InterruptActions();
        // do the damage
        life -= amount;
        if (life < 0)
            life = 0;
        // update life bar
        lifeBar.fillAmount = life / lifeBar.rectTransform.rect.width;
        updateLifeBacklayer = true;
        lifeBacklayerTimer = Time.time + 1f;
        // account for death
        if (life <= 0)
        {
            Death();
        }
        // reset the damage numbers
        recieveDamage = false;
        damageRecieved = 0;
        if(shake)
        {
            // shake the camera when an enemy hits you
            playerCamera.Shake((transform.position - enemyPos).normalized, -magnitud, duration);
            recieveShake = false;
            enemyPosRecieved = Vector3.zero;
            magnitudRecieved = 0f;
            durationRecieved = 0f;
        }
    }

    void InterruptActions()
    {
        // interrupt attacks
        if (inAnimation)
        {
            inAnimation = false;
            outAnimation = true;
            if (rightWeaponEquipded)
            {
                rightWeaponSlot.GetComponentInChildren<Collider2D>().enabled = false;
                weaponsAnimator[0].SetInteger("State", 0);
            }
            else if (leftWeaponEquipded)
            {
                rightWeaponSlot.GetComponentInChildren<Collider2D>().enabled = false;
                weaponsAnimator[1].SetInteger("State", 0);
            }
        }
        // interrupts dodgeroll
        if (inDodgeRoll)
            EndDodgeRoll();
    }

    void EndRecieveDamage()
    {
        //enemyAnimator.SetInteger("State", 0);
        damaged = false;
        inAnimation = false;
        // update life bar
    }

    void Death()
    {
        // setup dead
        dead = true;
        GetComponent<Animator>().SetBool("Death", true);
        GetComponent<BoxCollider2D>().enabled = false;
        transform.Find("Player_Minimap_Icon").gameObject.SetActive(false);
        // reset variables
        stamina = 0f;
        staminaBar.fillAmount = 0f;
        updateStaminaBacklayer = true;
        staminaBacklayerTimer = Time.time + 1f;
        // TODO: drop weapons, meanwhile just deactivate them
        Crosshair.gameObject.SetActive(false);
        rightWeaponSlot.SetActive(false);       
        leftWeaponSlot.SetActive(false);
    }
}
