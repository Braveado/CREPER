using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapFog : MonoBehaviour
{
    Vector2 pos, playerPos;
    GameObject player;
    bool icon;
    public GameObject fogIcon;
    float playerMinimapViewDistance;

    void Start()
    {
        // set position
        pos.x = transform.position.x;
        pos.y = transform.position.y;
        // initial icon state
        icon = false;
        // get player
        player = GameObject.Find("Player");
    }

	void Update ()
    {
        // check if the icon isnt active
        if (!icon)
        {
            // set player position
            playerPos = player.transform.position;
            // get player minimap distance
            playerMinimapViewDistance = player.GetComponent<Player>().minimapDistance;
            // activate the icon if the player is close
            if (Vector2.Distance(pos, playerPos) <= playerMinimapViewDistance - 1)
            {
                Color iconColor = fogIcon.GetComponent<SpriteRenderer>().color;
                iconColor.a += 1f * Time.deltaTime;
                fogIcon.GetComponent<SpriteRenderer>().color = iconColor;

                if (fogIcon.GetComponent<SpriteRenderer>().color.a >= 1)
                {
                    //fogIcon.SetActive(true);
                    icon = true;
                }
            }
            // keep the edges of the fog blurry
            else if (Vector2.Distance(pos, playerPos) <= playerMinimapViewDistance)
            {
                if (fogIcon.GetComponent<SpriteRenderer>().color.a < 0.5f)
                {
                    Color iconColor = fogIcon.GetComponent<SpriteRenderer>().color;
                    iconColor.a += 1f * Time.deltaTime;
                    fogIcon.GetComponent<SpriteRenderer>().color = iconColor;
                }
            }
        }
    }
}

// Using camera edges
//Vector2 goalPos;
//bool icon;
//public GameObject goalMinimapIcon;
//Vector3 screenPoint;

//void Start()
//{
//    // set goal position
//    goalPos = new Vector2(transform.position.x, transform.position.y);
//}

//void Update()
//{
//    // check if the icon isnt active
//    if (!icon)
//    {
//        // update the position of the goal with the camera
//        screenPoint = Camera.main.WorldToViewportPoint(goalPos);
//        // activate the icon if the goal is inside the camera           
//        if (screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
//        {
//            goalMinimapIcon.SetActive(true);
//            icon = true;
//        }
//    }
//}
