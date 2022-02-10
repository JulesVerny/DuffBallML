using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;

public class GameControl : MonoBehaviour
{

    // =========================================================================================
    public Text ScoreTextDisplay;
    public Text LevelTextDisplay;
    public Text NarrativeTextDisplay;

    public GameObject RedPlayer1;
    public GameObject RedPlayer2;
    public GameObject BluePlayer1;
    public GameObject BluePlayer2;
    public GameObject BlueGoalKeeper;
    public GameObject RedGoaKeeper;

    public GameObject TheBall;

    public int GameLevel; 

    private int BlueScore;
    private int RedScore;
    private string NarrativeString;
    private int DecisionCounter;
    private int MaxNumberOfDecisions = 600;    // Consider a very fast strike game to be around 40 decsion steps - However typical svae rates puts this back to around 150 steps once Golaie kicks off

    // Coordination of the ML Agents
    private SimpleMultiAgentGroup BlueAgentsTeamGroup;
    private SimpleMultiAgentGroup RedAgentsTeamGroup;


    private float ScoreRatio; 
    // =========================================================================================


    // =========================================================================================
    // Start is called before the first frame update
    void Start()
    {
        BlueAgentsTeamGroup = new SimpleMultiAgentGroup();   // Create the Blue ML Agents Group
        RedAgentsTeamGroup = new SimpleMultiAgentGroup();   // Create the Red ML Agents Group

        // Now Register Both the Blue Team Player Agents
        BlueAgentsTeamGroup.RegisterAgent(BluePlayer1.GetComponent<MLPlayer>());
        BlueAgentsTeamGroup.RegisterAgent(BluePlayer2.GetComponent<MLPlayer>());

        RedAgentsTeamGroup.RegisterAgent(RedPlayer1.GetComponent<MLPlayer>());
        RedAgentsTeamGroup.RegisterAgent(RedPlayer2.GetComponent<MLPlayer>());

        GameLevel = 0;

        UpdateAllPlayersGameLevel();

        BlueScore = 0;
        RedScore = 0;
        UpdateNarrativeString("Have a Great Game");
        UpdateScoreDisplay();

        // Start the Match
        ResetKickoff();

    } // Start
      // =========================================================================================

    public void ResetKickoff()
    {
        // ================================================================
        if (GameLevel > 0)
        { 
            // Curriclulum based learning Review The game performance  from the Respective Score lines
            if (RedScore >= 1) ScoreRatio = (float)BlueScore / ((float)RedScore);
            else
            {
                // No Red Score yet - so simply a fn of Blue Score
                if (BlueScore > 10) ScoreRatio = 5.0f;
            }

            if ((BlueScore > 50) && (ScoreRatio > 1.25f))
            {
                // Blue Player seems to Be Playing Well so Can Increment the Game Level 
                if (GameLevel < 12) GameLevel = GameLevel + 1;   // Up to a max of 12
                BlueScore = 0;
                RedScore = 0;
                UpdateScoreDisplay();

                // Send Updated Game level to All Players
                UpdateAllPlayersGameLevel();

                Debug.Log(" A GamePlayer is Updaing the Game level To: " + GameLevel.ToString());
            }

            if (BlueScore > 40 && RedScore == 0 && GameLevel > 8) Debug.Log("[ERROR] A Red Team is Not working !: ");
        }  // Game level > 0 So Updating For Curriculum 
        // =============================================================

       
        LevelTextDisplay.text = "League: " + GameLevel.ToString();

        // Now Reset All Players and the Ball
        RedPlayer1.SendMessage("ResetKickOff"); ;
        RedPlayer2.SendMessage("ResetKickOff"); ;
        BluePlayer1.SendMessage("ResetKickOff"); ;
        BluePlayer2.SendMessage("ResetKickOff"); ;
        BlueGoalKeeper.SendMessage("ResetKickOff"); ;
        RedGoaKeeper.SendMessage("ResetKickOff");

        TheBall.SendMessage("Reset");
        DecisionCounter = 0; 

    } // ResetKickoff
    // =========================================================================================
    void UpdateAllPlayersGameLevel()
    {
        RedPlayer1.SendMessage("UpdatePlayerLevel", GameLevel);
        RedPlayer2.SendMessage("UpdatePlayerLevel", GameLevel);
        BluePlayer1.SendMessage("UpdatePlayerLevel", GameLevel);
        BluePlayer2.SendMessage("UpdatePlayerLevel", GameLevel);
        BlueGoalKeeper.SendMessage("UpdatePlayerLevel", GameLevel);
        RedGoaKeeper.SendMessage("UpdatePlayerLevel", GameLevel);

    } // UpdateAllPlayersGameLevel
    // =========================================================================================
    // Update is called once per frame
    void Update()
    {
        // No UI Input to The Game Manager

    } // Update
    // =========================================================================================
    public void UpdateDecisionCount()
    {
        DecisionCounter++;
    }
    // =========================================================================================
    private void FixedUpdate()
    {
        if(DecisionCounter> MaxNumberOfDecisions)
        {
            // Exceeded the Tactical Decisions - so shoud abort the Training
            NarrativeTextDisplay.text = "Times Up";

            BlueAgentsTeamGroup.GroupEpisodeInterrupted();      // Need to Send Episode Interrupt to the Blue team ML Agents
            RedAgentsTeamGroup.GroupEpisodeInterrupted();       // Similarly for the Red team 

            ResetKickoff();

        }  // Excessive Number of Decisions

    }  // FixedUpdate

    // ====================================================================================
    public void IncrementBlueScore()
    {
        BlueScore = BlueScore + 1;
        UpdateScoreDisplay();

        // Provide a Positive Reward to the Blue Team Group  - And a Negative Reward to the Red team 
        BlueAgentsTeamGroup.AddGroupReward(1 - 0.5f*(float)DecisionCounter / MaxNumberOfDecisions);
        RedAgentsTeamGroup.AddGroupReward(-1);

        // End the Episode and Restart With a New Kick Off
        BlueAgentsTeamGroup.EndGroupEpisode();
        RedAgentsTeamGroup.EndGroupEpisode();

        ResetKickoff();

    }  // Increment Blue Score
    // ====================================================
    public void IncrementRedScore()
    {
        RedScore = RedScore + 1;
        UpdateScoreDisplay();

        // Provide a Positive Reward to the Red Team Group  - And a Negative Reward to the Blue team 
        RedAgentsTeamGroup.AddGroupReward(1 - 0.5f * (float)DecisionCounter / MaxNumberOfDecisions);
        BlueAgentsTeamGroup.AddGroupReward(-1);

        // End the Episode and Restart With a New Kick Off
        BlueAgentsTeamGroup.EndGroupEpisode();
        RedAgentsTeamGroup.EndGroupEpisode();
        ResetKickoff();

    }  // Increment Red Score
    // =============================================
    public void UpdateNarrativeString(string NewDisplayString)
    {
        NarrativeString = NewDisplayString;
        NarrativeTextDisplay.text = NarrativeString;
    }
    // =========================================================================================
    void UpdateScoreDisplay()
    {
        ScoreTextDisplay.text = "Blue: " + BlueScore.ToString() + " Red: " + RedScore.ToString();
    }
    // =========================================================================================
}
