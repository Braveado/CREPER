using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Goal : MonoBehaviour
{
    SpriteRenderer goalRender;
    Color goalColor;
    Vector2 goalPos, playerPos;
    GameObject player;
    bool icon;
    public GameObject goalMinimapIcon;
    float playerMinimapViewDistance;
    [HideInInspector]
    public bool levelFinished;

    void Start ()
    {
        // set goal position
        goalPos.x = transform.position.x;
        goalPos.y = transform.position.y;
        // get player
        player = GameObject.Find("Player");        
        // get goal render and color
        goalRender = GetComponent<SpriteRenderer>();
        goalColor = goalRender.color;
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
            if (Vector2.Distance(goalPos, playerPos) <= playerMinimapViewDistance)
            {
                goalMinimapIcon.SetActive(true);
                icon = true;
            }
        }

        if(levelFinished)
        {
            // fade out
            if(goalColor.a > 0)
            {
                // fade out the goal
                goalColor.a -= 1 * Time.deltaTime;
                goalRender.color = goalColor;
            }
        }
	}

    void OnTriggerEnter2D(Collider2D other)
    {
        // player collision
        if(other.tag == "Player")
        {
            // start the level end transition    
            
            gameObject.GetComponent<ParticleSystem>().Stop();
            levelFinished = true;
        }
    }
}
