using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    enum Menu { None, Play, Exit};
    Menu optionSelected;
    public SpriteRenderer maskRender;
    Color maskColor;
    bool resetMask;
    [HideInInspector]
    public bool mask;
    public float transitionSeconds;
    [HideInInspector]
    public float secondsTransitioned;
    [HideInInspector]
    public int level;
    [HideInInspector]
    public int score;
    Goal goal;
    Player player;
    bool updateHUD;
    bool death;

    void Start()
    {
        // keep the manager in every scene
        DontDestroyOnLoad(this);
        //set default option
        optionSelected = Menu.None;
        // get mask render and color
        maskColor = maskRender.color;
        mask = true;
    }

	void Update ()
    {
        // at the main menu
        if(SceneManager.GetActiveScene() == SceneManager.GetSceneByBuildIndex(0))
        {
            //// unlock and show the cursor
            //if(Cursor.lockState == CursorLockMode.Confined || !Cursor.visible)
            //{                
            //    Cursor.lockState = CursorLockMode.None;
            //    Cursor.visible = true;
            //}
            // reset death screen
            death = false;
            // clear the mask at start    
            if (optionSelected == Menu.None)
            {
                if (Transition(false, 1f))
                    mask = false;
            }
            // fill the mask and go to the first level transition
            if (optionSelected == Menu.Play)
            {
                if (Transition(true, 1f))
                {
                    mask = true;
                    // reset the option selected from menu
                    optionSelected = Menu.None;
                    // set the wait time for a full transition
                    secondsTransitioned = Time.time + transitionSeconds;
                    level = 1;                    
                    SceneManager.LoadScene(1);
                }
            }
            // fill the mask and exit the game
            if(optionSelected == Menu.Exit)
            {
                if (Transition(true, 1f))
                {
                    mask = true;
                    // reset the option selected from menu
                    optionSelected = Menu.None;
                    // exit once the mask is filled
                    Application.Quit();
                }
            }
        }
        // at a transition         
        else if(SceneManager.GetActiveScene() == SceneManager.GetSceneByBuildIndex(1))
        {
            //// lock and hide the cursor
            //if (Cursor.lockState == CursorLockMode.None || Cursor.visible)
            //{
            //    Cursor.lockState = CursorLockMode.Confined;
            //    Cursor.visible = false;
            //}
            // to avoid the mask not affect the camera
            transform.position = new Vector3(0f, 0f, 0f);
            // setup level and score numbers
            if (!death)
            {
                GameObject.Find("Menu").transform.Find("Level").GetComponent<Text>().text = "LEVEL: " + level.ToString("0");
                GameObject.Find("Menu").transform.Find("Score").GetComponent<Text>().text = "SCORE: " + score.ToString("0");
                updateHUD = true;
            }
            else if (death)
            {
                GameObject.Find("Menu").transform.Find("Level").GetComponent<Text>().text = "FINAL LEVEL: " + level.ToString("0");
                GameObject.Find("Menu").transform.Find("Score").GetComponent<Text>().text = "FINAL SCORE: " + score.ToString("0");
            }
            // clear the mask at start
            if (mask && Time.time <= secondsTransitioned - 1f)
            {
                if (Transition(false, 1f))
                    mask = false;
            }
            // fill the mask before the transition ends
            else if(!mask && Time.time >= secondsTransitioned - 1f)
            {
                if (Transition(true, 1f))
                    mask = true;
            }
            else if(mask && secondsTransitioned <= Time.time)
            {
                secondsTransitioned = 0;
                if (!death)
                    SceneManager.LoadScene(2);
                else if (death)
                    SceneManager.LoadScene(0);
            }
        }
        // at a level
        else if(SceneManager.GetActiveScene() == SceneManager.GetSceneByBuildIndex(2))
        {
            UpdateLevelState();
        }
	}

    public void PlayButton()
    {
        if (optionSelected == Menu.None)
            optionSelected = Menu.Play;                 
    }

    public void ExitButton()
    {
        if (optionSelected == Menu.None)
            optionSelected = Menu.Exit;
    }

    public bool Transition(bool state, float seconds)
    {
        // clear or fill the mask
        if(state && maskRender.color.a < 1f)
            maskColor.a += (1 / seconds) * Time.deltaTime;           
        else if(!state && maskRender.color.a > 0f)
            maskColor.a -= (1 / seconds) * Time.deltaTime;

        // update the mask
        maskRender.color = maskColor;

        // check if the mask is done cleaning or filling
        if (state && maskRender.color.a >= 1f)
            return true;
        else if (!state && maskRender.color.a <= 0f)
            return true;
        else
            return false;
    }

    void UpdateLevelState()
    {
        // setup level and score on player hud
        if (updateHUD)
        {
            player = GameObject.Find("Player").GetComponent<Player>();
            player.playerScore = score;
            player.scoreText.text = score.ToString("0");
            player.levelText.text = level.ToString("0");
            updateHUD = false;
        }
        // get the goal
        if (goal == null)
            goal = GameObject.FindObjectOfType<Goal>().GetComponent<Goal>();
        // clear the mask at start
        if (mask && !player.dead && !goal.levelFinished)
        {
            if (Transition(false, 1f))
                mask = false;
        }
        // fill the mask on level completition
        else if (!mask && goal.levelFinished)
        {
            // to avoid the mask from afecting the camera
            transform.position = player.transform.position;
            if (Transition(true, 3f))
                mask = true;
        }
        // go to the next level transition
        else if (mask && goal.levelFinished)
        {
            // setup a full transition
            secondsTransitioned = Time.time + transitionSeconds;
            // send score to the game manager            
            score = player.playerScore;
            level++;
            SceneManager.LoadScene(1);
        }
        // fill the mask on death
        else if (!mask && player.dead)
        {
            // to avoid the mask from afecting the camera
            transform.position = player.transform.position;
            if (Transition(true, 5f))
            {
                mask = true;
                death = true;
            }
        }
        // go to the final transition to return to the menu
        else if (mask && death)
        {
            // send score to the game manager, the level is the same            
            score = player.playerScore;
            // set the wait time for a full transition to the results screen
            secondsTransitioned = Time.time + transitionSeconds;
            SceneManager.LoadScene(1);
        }
    }
}
