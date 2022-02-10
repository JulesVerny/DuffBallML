using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class MLPlayer : Agent
{
    // ===============================================================================
    public enum PlayerState { IdleAtPosition, RunningToBall, RunningToAttack, RunningToDefence, DribblingForward, PassingBall, StrikingTheBall, Celebrating };
    public enum PlayerSide { None, BlueTeam, RedTeam };

    public enum TacticalActions { None, ChaseBallPlayer, DribbleForward, DribbleWide, Goto_Defence, Goto_Attack, StrikeBall, Pass_Ball };

    // =========================================================================================
    public GameObject TheBallObject;
    public GameObject TheGameManager;
    public GameObject TheOppoGoalArea;
    public GameObject TheOwnGoalArea;
    public GameObject[] OppoPlayers;
    public GameObject OppoGoallie;
    public GameObject TeamPlayer;
    public int OwnIdentity;
   // public int PlayerGameLevel;

    public PlayerSide ThePlayerSide;

    private float DistanceToBall;
    private TheBallControl TheBallController;

    // Motion Controls
    private CharacterController TheCharController;

    private float RunSpeed = 2.5f;
    private float gravity = -20.0f;
    private float PlayerRotationRate = 100.0f;
    private int InterPlayerCollisionCount = 0;

    private Vector3 KickTarget;

    // Character Animations 
    public PlayerState ThePlayerCurrentState;
    private Animator ThePlayerAnimator;
    public string CurrentAnimationName;

    // Main State Controls
    public bool CurrentlyHasTheBall;
    private float TacticalDistanceThreshold = 1.5f;
    private float OppoPlayerCloseThreshold = 3.0f;
    private float DecisionCountDown;
    public TacticalActions ProposedAction;

    // GameControl
    public int TheCurrentGameLevel;
    private bool CelebrationPeriod;

    // Player Tactical Controls
    private int TheCurrentBallOwner;
    private PlayerSide CurrentBallSide;
    private Vector3 TargetOpPosition;
    // Review change in Tactic
    private TacticalActions PrevTacticalAction;

    // ==================================================================================
    #region Initialisation and Startup Stuff
    // ===================================================================================================
    private void Awake()
    {
        TheCharController = GetComponent<CharacterController>();
        ThePlayerAnimator = GetComponentInChildren<Animator>();
        TheBallController = TheBallObject.GetComponent<TheBallControl>();

    } // Awake
    // ======================================================================================================
    public override void Initialize()
    {

        TheCurrentGameLevel = 0;
        RunSpeed = 2.5f;
        ResetKickOff();
        
    } // Initialize
    // ======================================================================================================
    public void UpdatePlayerLevel(int NewGameLevel)
    {
        TheCurrentGameLevel = NewGameLevel;

    }  // UpdatePlayerLevel
    // ======================================================================================================

    public override void OnEpisodeBegin()
    {
        ResetKickOff();
    } // OnEpisodeBegin
      // ======================================================================================================
    public void ResetKickOff()
    {

        InitialisePlayerStartPosition();

        ProposedAction = TacticalActions.None;
        TargetOpPosition = transform.position;
        KickTarget = Vector3.zero;

        SetPlayerWaitingAtPosition();

        CurrentlyHasTheBall = false;
        TheCurrentBallOwner = 0;
        DecisionCountDown = 0;
        InterPlayerCollisionCount = 0;
        CelebrationPeriod = false;

        CurrentBallSide = PlayerSide.None;

    }  // ResetKickOff

    // =========================================================================================
    void InitialisePlayerStartPosition()
    {
        // Reset the Player to its Start Position and Orientation  - REMEMBER: Need to Disbable the Character Controller if perfoming an Explict transform assignment 

        TheCharController.enabled = false;
        float MidZPoint = TheOwnGoalArea.transform.position.z;
        float PlayerForwardFactor = 0.25f;   // (Range from 0 to 0.5f ) 
        float PlayerZPos = MidZPoint;
        // Blue Side Set Mostly performant at all levels
        if (ThePlayerSide == PlayerSide.BlueTeam)
        {

            // ======================================================
            // Self Play Settings
            if (TheCurrentGameLevel ==0)
            {
                RunSpeed = 2.5f; 
                if (OwnIdentity == 11) PlayerForwardFactor = 0.4f;
                else PlayerForwardFactor = 0.2f;
                PlayerZPos = MidZPoint + Random.Range(-3.0f, 3.0f);
            }
            // ======================================================

            // Curriclum Play Settings
            RunSpeed = 2.5f;   // Blue Player alwys at Optimum Speed
            if (TheCurrentGameLevel == 1)
            {
                if (OwnIdentity == 11) PlayerForwardFactor = 0.45f;
                else PlayerForwardFactor = 0.35f;
                PlayerZPos = MidZPoint + Random.Range(-1.0f, 1.0f);
            }
            if (TheCurrentGameLevel == 2)
            {
                if (OwnIdentity == 11) PlayerForwardFactor = 0.44f;
                else PlayerForwardFactor = 0.325f;
                PlayerZPos = MidZPoint + Random.Range(-1.0f, 1.0f);
            }
            if (TheCurrentGameLevel == 3)
            {
                if (OwnIdentity == 11) PlayerForwardFactor = 0.43f;
                else PlayerForwardFactor = 0.3f;
                PlayerZPos = MidZPoint + Random.Range(-1.0f, 1.0f);
            }
            if (TheCurrentGameLevel == 4)
            {
                if (OwnIdentity == 11) PlayerForwardFactor = 0.42f;
                else PlayerForwardFactor = 0.275f;
                PlayerZPos = MidZPoint + Random.Range(-2.0f, 2.0f);
            }
            if (TheCurrentGameLevel == 5)
            {
                if (OwnIdentity == 11) PlayerForwardFactor = 0.41f;
                else PlayerForwardFactor = 0.25f;
                PlayerZPos = MidZPoint + Random.Range(-3.0f, 3.0f);
            }
            if (TheCurrentGameLevel == 6)
            {
                if (OwnIdentity == 11) PlayerForwardFactor = 0.4f;
                else PlayerForwardFactor = 0.2f;
                PlayerZPos = MidZPoint + Random.Range(-3.5f, 3.5f);
            }
            if (TheCurrentGameLevel > 6)
            {
                if (OwnIdentity == 11) PlayerForwardFactor = 0.4f;
                else PlayerForwardFactor = 0.2f;
                PlayerZPos = MidZPoint + Random.Range(-4.0f, 4.0f);
            }
        } // Blue Team Set Up 
        // ===================================================================
        else
        {
            // ======================================================
            // Self Play Settings
            if (TheCurrentGameLevel ==0)
            {
                RunSpeed = 2.5f;
                if (OwnIdentity == 21) PlayerForwardFactor = 0.4f;
                else PlayerForwardFactor = 0.2f;
                PlayerZPos = MidZPoint + Random.Range(-3.0f, 3.0f);
            } // Self Play
            // ======================================================

            // Curricluum Player  Settings
            RunSpeed = 2.5f;  // Default Run Speed
            if (TheCurrentGameLevel == 1)
            {
                RunSpeed = 0.1f;
                if (OwnIdentity == 21)
                {
                    PlayerForwardFactor = 0.1f;
                    PlayerZPos = MidZPoint - 4.0f;
                }
                else
                {
                    PlayerForwardFactor = 0.1f;
                    PlayerZPos = MidZPoint + 4.0f;
                }
            } // Game Level 1
            if (TheCurrentGameLevel == 2)
            {
                RunSpeed = 0.2f;
                if (OwnIdentity == 21)
                {
                    PlayerForwardFactor = 0.15f;
                    PlayerZPos = MidZPoint - 4.0f;
                }
                else
                {
                    PlayerForwardFactor = 0.15f;
                    PlayerZPos = MidZPoint + 4.0f;
                }
            } // Game Level 2
            if (TheCurrentGameLevel == 3)
            {
                RunSpeed = 0.25f;
                if (OwnIdentity == 21)
                {
                    PlayerForwardFactor = 0.175f;
                    PlayerZPos = MidZPoint - 3.0f;
                }
                else
                {
                    PlayerForwardFactor = 0.175f;
                    PlayerZPos = MidZPoint + 3.0f;
                }
            } // Game Level 3
            if (TheCurrentGameLevel == 4)
            {
                RunSpeed = 0.3f;
                if (OwnIdentity == 21)
                {
                    PlayerForwardFactor = 0.2f;
                    PlayerZPos = MidZPoint - 2.0f;
                }
                else
                {
                    PlayerForwardFactor = 0.2f;
                    PlayerZPos = MidZPoint + 2.0f;
                }
            } // Game Level 4
            if (TheCurrentGameLevel == 5)
            {
                RunSpeed = 0.5f;
                if (OwnIdentity == 21)
                {
                    PlayerForwardFactor = 0.25f;
                    PlayerZPos = MidZPoint - 2.0f;
                }
                else
                {
                    PlayerForwardFactor = 0.2f;
                    PlayerZPos = MidZPoint + 2.0f;
                }
            } // Game Level 5
            if (TheCurrentGameLevel == 6)
            {
                RunSpeed = 0.75f;
                if (OwnIdentity == 21)
                {
                    PlayerForwardFactor = 0.25f;
                    PlayerZPos = MidZPoint - 2.0f;
                }
                else
                {
                    PlayerForwardFactor = 0.2f;
                    PlayerZPos = MidZPoint + 2.0f;
                }
            } // Game Level 6
            if (TheCurrentGameLevel == 7)
            {
                RunSpeed = 1.0f;
                if (OwnIdentity == 21) PlayerForwardFactor = 0.275f;
                else PlayerForwardFactor = 0.2f;
                PlayerZPos = MidZPoint + Random.Range(-4.0f, 4.0f);
            } // Game Level 7
            if (TheCurrentGameLevel == 8)
            {
                RunSpeed = 1.25f;
                if (OwnIdentity == 21) PlayerForwardFactor = 0.3f;
                else PlayerForwardFactor = 0.2f;
                PlayerZPos = MidZPoint + Random.Range(-3.5f, 3.0f);
            } // Game Level 8
            if (TheCurrentGameLevel == 9)
            {
                RunSpeed = 1.5f;
                if (OwnIdentity == 21) PlayerForwardFactor = 0.325f;
                else PlayerForwardFactor = 0.2f;
                PlayerZPos = MidZPoint + Random.Range(-3.0f, 3.0f);
            } // Game Level 9
            if (TheCurrentGameLevel == 10)
            {
                RunSpeed = 2.0f;
                if (OwnIdentity == 21) PlayerForwardFactor = 0.35f;
                else PlayerForwardFactor = 0.2f;
                PlayerZPos = MidZPoint + Random.Range(-2.0f, 2.0f);
            } // Game Level 10
            if (TheCurrentGameLevel == 11)
            {
                RunSpeed = 2.25f;
                if (OwnIdentity == 21) PlayerForwardFactor = 0.375f;
                else PlayerForwardFactor = 0.2f;
                PlayerZPos = MidZPoint + Random.Range(-1.0f, 1.0f);
            } // Game Level 11
            if (TheCurrentGameLevel >= 12)
            {
                RunSpeed = 2.5f;
                if (OwnIdentity == 21) PlayerForwardFactor = 0.4f;
                else PlayerForwardFactor = 0.2f;
                PlayerZPos = MidZPoint + Random.Range(-1.0f, 1.0f);
            } // Game Level 12

        } // Red Team Player Set Up
          // ===============================

        float PlayerStartXPos = TheOwnGoalArea.transform.position.x + PlayerForwardFactor * (TheOppoGoalArea.transform.position.x - TheOwnGoalArea.transform.position.x);
        transform.position = new Vector3(PlayerStartXPos, 0.0f, PlayerZPos);

        // Correctly orientate the Player - just the same as own Goal Area Direction
        transform.rotation = TheOwnGoalArea.transform.rotation;

        TheCharController.enabled = true;
    } // InitialisePlayerStartPosition
    // =================================================================================================================

    #endregion

    // ========================================================================================================
    public override void CollectObservations(VectorSensor sensor)
    {
        // Collect the Player Observations 
        // float x - Fraction up the Pitch to Oppo Goal Area  (Note this is a Function of Side
        float NormalisedDistanceForward = 0.0f;
        if(ThePlayerSide== PlayerSide.RedTeam) NormalisedDistanceForward = (transform.position.x - TheOwnGoalArea.transform.position.x) / 26.0f;
        else NormalisedDistanceForward = -(transform.position.x - TheOwnGoalArea.transform.position.x) / 26.0f;

        sensor.AddObservation(NormalisedDistanceForward);

        // The Ball States
        sensor.AddObservation(CurrentlyHasTheBall);
        sensor.AddObservation((CurrentBallSide == PlayerSide.None));  // If Ball free or Not
        sensor.AddObservation((CurrentBallSide == ThePlayerSide));  // To Discrimate if the Ball Owner By Oppo Team or Team mate

        // Discrete Actionable Check States
        sensor.AddObservation(CheckIfAbleToStrike());
        sensor.AddObservation(CheckIfOppoPlayerIsClose());
        sensor.AddObservation(CheckIfClosestToBall());
        sensor.AddObservation(CheckIfTeamMateIsForward());

        // Need to Check If Able to currently Act  - Should really use this as a Mask of Actions
        sensor.AddObservation(CheckIfCurrentlyAbleToAct());

        // A Total of 9 Observations

    }   // CollectObservations
    // ======================================================================================================
    // Attempting to Mask Some of the Actions
    // As explained at  https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Learning-Environment-Design-Agents.md#masking-discrete-actions
    // TacticalActions Mappling:  { 0: None, 1:ChaseBallPlayer, 2:DribbleForward, 3:DribbleWide, 4:Goto_Defence, 5:Goto_Attack, 6:StrikeBall, 7:Pass_Ball };
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if(!CheckIfCurrentlyAbleToAct())
        {
            // Not Really Able to Do Any Actions Apart from None
            actionMask.SetActionEnabled(0, 1, false);
            actionMask.SetActionEnabled(0, 2, false);
            actionMask.SetActionEnabled(0, 3, false);
            actionMask.SetActionEnabled(0, 4, false);
            actionMask.SetActionEnabled(0, 5, false);
            actionMask.SetActionEnabled(0, 6, false);
            actionMask.SetActionEnabled(0, 7, false);
        }  // Nota Able to Act

        if(CurrentlyHasTheBall)
        {
            // If Currently Have the Ball do not really want to 1: Chase, 4: Goto Defence 5: Goto Attack
            actionMask.SetActionEnabled(0, 1, false);
            actionMask.SetActionEnabled(0, 4, false);
            actionMask.SetActionEnabled(0, 5, false);
        }
        else
        {
            // Do Not Have the Ball so Unable to 2: Dribbe Forward, 3: Dribble Wide, 6: Strike or 7: Pass the Ball
            actionMask.SetActionEnabled(0, 2, false);
            actionMask.SetActionEnabled(0, 3, false);
            actionMask.SetActionEnabled(0, 6, false);
            actionMask.SetActionEnabled(0, 7, false);
        }

    }  // WriteDiscreteActionMask
    // ===========================================================================================================
    bool CheckIfCurrentlyAbleToAct()
    {
        bool RtnAbleToAction;
        RtnAbleToAction = true;
        CurrentAnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((CurrentAnimationName == "Strike") || (CurrentAnimationName == "Pass") || (CurrentAnimationName == "Victory")) RtnAbleToAction = false;
        if (DecisionCountDown > 0) RtnAbleToAction = false;
        if (CelebrationPeriod) RtnAbleToAction = false;

        return RtnAbleToAction;
    } // CheckIfCurrentlyAbleToAct
    // =========================================================================================================

    #region Main Update Action Processing 
    // =========================================================================================================
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // The Main ML Player Processing Loop  - Function of NN Recomended Actions

        // Note Can only really excute an Action if NOT Celebrating &&  Decision Count ==0  && any Kick Annimations Progress     *** REALLY NEED TO MASK Actions OUT on that Basis !      

        DistanceToBall = Vector3.Distance(transform.position, TheBallObject.transform.position);
        if (DecisionCountDown > 0) DecisionCountDown--;
        CurrentAnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;

        // Review and Execute the Proposed Action
        if (!(CelebrationPeriod))
        {
            // Automated Control : We need to Review the Proposed new Action if Scheduled
            if (DecisionCountDown == 0)
            {
                // Do Not Request a new Action if currently in a Fixed Animation Sequence
                if (!((CurrentAnimationName == "Strike") || (CurrentAnimationName == "Pass") || (CurrentAnimationName == "Victory")))
                {
                    // Process the NN Recommended Action
                    ProposedAction = MappedTacticalAction(actionBuffers.DiscreteActions[0]); 

                    if (ProposedAction != PrevTacticalAction) TheGameManager.SendMessage("UpdateDecisionCount"); // A Change in Tactical Decision  - So Increment the Game Managers Decsion Counter  
                }
            } // Decision Countdown Qualifier
            // ==========================================================================


            // Now Attempt to Execute the Proposed Action - (if its a Change in State and consistent with current Ball Ownership) 
            if ((ProposedAction == TacticalActions.ChaseBallPlayer) && (ThePlayerCurrentState != PlayerState.RunningToBall) && !CurrentlyHasTheBall) SetRunningToBall();
            if ((ProposedAction == TacticalActions.DribbleWide) && (ThePlayerCurrentState != PlayerState.DribblingForward) && CurrentlyHasTheBall) SetDribblingWide();
            if ((ProposedAction == TacticalActions.DribbleForward) && (ThePlayerCurrentState != PlayerState.DribblingForward) && CurrentlyHasTheBall) SetDribblingForward();

            // In Reviewing the Run To Ops - Need to qualify if there has Actually been a Tactical change - as same recommendation would Needs to stay in Waiting at Idle 
            if ((ProposedAction == TacticalActions.Goto_Attack) && (ThePlayerCurrentState != PlayerState.RunningToAttack) && !CurrentlyHasTheBall)
            {
                // Then need to Go to the Attack Ops Posiiton - Unless reached Idle Position AND thre has been No Change in Ops Tactic Idle
                if (!((ThePlayerCurrentState == PlayerState.IdleAtPosition) && (PrevTacticalAction == TacticalActions.Goto_Attack))) SetGoToAttack();
            }
            if ((ProposedAction == TacticalActions.Goto_Defence) && (ThePlayerCurrentState != PlayerState.RunningToDefence) && !CurrentlyHasTheBall)
            {
                // Then need to Go to the Defende Ops Posiiton - Unless reached Idle Position AND thre has been No Change in Ops Tactic Idle
                if (!((ThePlayerCurrentState == PlayerState.IdleAtPosition) && (PrevTacticalAction == TacticalActions.Goto_Defence))) SetGoToDefence();
            }

            if ((ProposedAction == TacticalActions.StrikeBall) && (ThePlayerCurrentState != PlayerState.StrikingTheBall) && (DecisionCountDown == 0))
            {
                if (CurrentlyHasTheBall && CheckIfAbleToStrike())
                {
                    // Perform a Strike Action A little after any Previous Decision, to allow any previous Strike Animation to complete via a Idle state.  
                    KickTarget = PickAGoalAimPoint();
                    transform.LookAt(new Vector3(KickTarget.x, transform.position.y, KickTarget.z));
                    SetStrikingTheBall();
                }
            } // Strike Action proposed

            if ((ProposedAction == TacticalActions.Pass_Ball) && (ThePlayerCurrentState != PlayerState.PassingBall) && (DecisionCountDown == 0))
            {
                if (CurrentlyHasTheBall)
                {
                    // Perform a Pass Action A little after any Previous Decision, to allow any previous Strike Animation to complete via a Idle state. 
                    KickTarget = TeamPlayer.transform.position;
                    transform.LookAt(new Vector3(KickTarget.x, transform.position.y, KickTarget.z));
                    SetPassingTheBall();
                }
            } // Pass Action proposed

            PrevTacticalAction = ProposedAction;
        } // Automated Player Control
        // =====================================================================
        // Ball Update Checks
        if ((CurrentlyHasTheBall) && (ThePlayerCurrentState == PlayerState.DribblingForward))
        {
            // Update the Ball Postion, Just in Front of Player
            Vector3 BallDribblePosition = transform.position + transform.forward * 0.75f + new Vector3(0.0f, 0.25f, 0.0f);
            // Update the Ball Position
            TheBallObject.SendMessage("UpdateDribblePosition", BallDribblePosition);
        }
        else
        {
            // Only Perform a Check After a decsion Delay (To allow a Free Ball to leave any previous  Kick Actions
            if (DecisionCountDown <= 0) CheckCaptureFreeBall();
        }  // Does Not Have the Ball - Checks
        // ========================================================================

        if (ThePlayerCurrentState == PlayerState.RunningToBall)
        {
            RunningWithoutBallChecks();
        } // Running Towards Ball
        // =================================================
        if ((ThePlayerCurrentState == PlayerState.RunningToAttack) || (ThePlayerCurrentState == PlayerState.RunningToDefence))
        {
            RunningWithoutBallChecks();

        } // Running Towards Ops Position
        // =================================================
        if (ThePlayerCurrentState == PlayerState.DribblingForward)
        {
            // Need Rotate Dribble Towards Opposite Goal Area
            transform.LookAt(new Vector3(TheOppoGoalArea.transform.position.x, transform.position.y, TheOppoGoalArea.transform.position.z));
            PerformForwardDeltaMovement(RunSpeed);
            // Note DO NOT need to  Check of Reaching Destination as Strike should be Recommended By review Actions  
        } // Running Towards Ops Position
        // =================================================


        // =================================================
        // Now Check Animations Progress Events
        CurrentAnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;

        // Check Completed Strike
        if ((CurrentAnimationName == "Strike") && (ThePlayerCurrentState == PlayerState.StrikingTheBall))
        {
            // Check the Draw a Debug Ray 
            //if (OwnIdentity == 11) Debug.DrawRay(StrikeStart, StrikeDirection * 12.0f, Color.red);

            // Actually Strike The Ball when Animation 25% Complete
            if ((ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.2f) && (CurrentlyHasTheBall))
            {
                PerformTheBallStrike();
            }  // Strike is 25 % Complete  - Actually Strike  the Ball
            // ====================================
            // Only Change Player State when Animation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.975f)
            {
                SetPlayerWaitingAtPosition();
            }  // 100 % Complte
        }  // Check Completed Strike
        // ==================================================
        // Check Completed Pass
        if ((CurrentAnimationName == "Pass") && (ThePlayerCurrentState == PlayerState.PassingBall))
        {
            // Actually Pass The Ball when Animation 25% Complete
            if ((ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.25f) && (CurrentlyHasTheBall))
            {
                PerformPlayerPass();
            }  // Pass is 25 % Complete  - Actually Pass  the Ball
            // ====================================

            // Only Change Player State when Animation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.975f)
            {
                SetPlayerWaitingAtPosition();
            }  // 100 % Complte
        }  // Check Completed Pass
        // ====================================================
        // Check Completed Celebration
        if ((CurrentAnimationName == "Victory") && (ThePlayerCurrentState == PlayerState.Celebrating))
        {
            // Only Change Player State when Animation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.975f)
            {
                // Finished Celebrating So Now Assign the Score Line   (Which Will also End the Episode
                if (ThePlayerSide == PlayerSide.BlueTeam) TheGameManager.SendMessage("IncrementBlueScore");
                else TheGameManager.SendMessage("IncrementRedScore");

            }  // 100 % Complte
        }  // Check End of Celebration
        // ====================================================

    }//  OnActionReceived   Processing
     // ======================================================================================================
    TacticalActions MappedTacticalAction(int NNAction)
    {
        // TacticalActions Mappling:  { 0: None, 1:ChaseBallPlayer, 2:DribbleForward, 3:DribbleWide, 4:Goto_Defence, 5:Goto_Attack, 6:StrikeBall, 7:Pass_Ball };
        TacticalActions RtnAction = TacticalActions.None;
        if (NNAction == 1) RtnAction = TacticalActions.ChaseBallPlayer;
        if (NNAction == 2) RtnAction = TacticalActions.DribbleForward;
        if (NNAction == 3) RtnAction = TacticalActions.DribbleWide;
        if (NNAction == 4) RtnAction = TacticalActions.Goto_Defence;
        if (NNAction == 5) RtnAction = TacticalActions.Goto_Attack;
        if (NNAction == 6) RtnAction = TacticalActions.StrikeBall;
        if (NNAction == 7) RtnAction = TacticalActions.Pass_Ball;

        return RtnAction;
    } // MappedTacticalAction
      // =====================================================================================================
    #endregion
    // ======================================================================================================
    public override void Heuristic(in ActionBuffers actionsOut)
    // Hueristic Manual Actions
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;     // Deafult to None Action

        // TacticalActions Mappling:  { 0: None, 1:ChaseBallPlayer, 2:DribbleForward, 3:DribbleWide, 4:Goto_Defence, 5:Goto_Attack, 6:StrikeBall, 7:Pass_Ball };
      
        // ==============================================================
        // B- Goto To Ball Player
        if (Input.GetKey(KeyCode.B)) discreteActionsOut[0] = 1;

        // F- Dribble Forward
        if (Input.GetKey(KeyCode.F)) discreteActionsOut[0] = 2;

        // W- Dribble Wide
        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 3;

        // D- Goto To Defence Position
        if (Input.GetKey(KeyCode.D)) discreteActionsOut[0] = 4;

        // A- Goto To Attack Position
        if (Input.GetKey(KeyCode.A)) discreteActionsOut[0] = 5;

        // S - Pass the Ball
        if (Input.GetKey(KeyCode.S)) discreteActionsOut[0] = 6;

        // P - Pass the Ball
        if (Input.GetKey(KeyCode.P)) discreteActionsOut[0] = 7;
       
        // ===========================================================
        /*
        // Manual Movement Controls
        if ((UnderManualControl) && !(CelebrationPeriod))
        {
            // Check if in one of tyhe Running States
            if ((ThePlayerCurrentState == PlayerState.RunningToBall) || (ThePlayerCurrentState == PlayerState.RunningToDefence) || (ThePlayerCurrentState == PlayerState.RunningToAttack))
            {
                RunningWithoutBallChecks();
                PerformForwardDeltaMovement(RunSpeed);
            }
            if (ThePlayerCurrentState == PlayerState.DribblingForward) PerformForwardDeltaMovement(RunSpeed);

        } // Manual Motion Control
        */

    }  // Heuristic  Manual Controls
    // ======================================================================================================



    // ==========================================================================================================
    #region Simple Dynamics Check
    // ==========================================================================================================
    void RunningWithoutBallChecks()
    {
        if (ThePlayerCurrentState == PlayerState.RunningToBall)
        {
            transform.LookAt(new Vector3(TheBallObject.transform.position.x, transform.position.y, TheBallObject.transform.position.z), Vector3.up);
            PerformForwardDeltaMovement(RunSpeed * 1.5f);
        }
        if ((ThePlayerCurrentState == PlayerState.RunningToDefence) || (ThePlayerCurrentState == PlayerState.RunningToAttack))
        {
            transform.LookAt(new Vector3(TargetOpPosition.x, transform.position.y, TargetOpPosition.z));
            PerformForwardDeltaMovement(RunSpeed * 1.25f);

            // Now Check if Reached the Op Destination
            float DistanceToOpTarget = Vector3.Distance(TargetOpPosition, transform.position);
            if (DistanceToOpTarget < TacticalDistanceThreshold)
            {
                // Then have reached the Oppo Target Position so can Set Back to Waiting At Idle
                if (ThePlayerCurrentState != PlayerState.IdleAtPosition) SetPlayerWaitingAtPosition();
            }
        }  //  Running to an Attack or Defence Position 
        // ============================================
        // Now Check if in close proximitary to the Ball Player - To either Take the Ball or Perform a Player Tackle Attempt
        if ((DistanceToBall < TacticalDistanceThreshold) && (DecisionCountDown <= 0))
        {
            // Then have reached the Ball or Player
            if (CurrentBallSide == PlayerSide.None) CheckCaptureFreeBall();
            if (CurrentBallSide != ThePlayerSide)
            {
                // Randomise the Tackle Attempt
                int RandomTackeAttempt = Random.Range(0, 100);
                // Only perfom a Ball Tackle Action if greater than 95% probabiltiy  to try and keep some Ball continuity  
                if (RandomTackeAttempt > 95) TheBallObject.SendMessage("RequestBallTackle", OwnIdentity);
            }  // Oppo Player has the Ball
        } // Reached the Ball or Player     
    }  // RunningWithoutBallChecks()
    // =============================================================================================
    void CheckCaptureFreeBall()
    {
        // Check that Neither Player Has te Ball
        if (CurrentBallSide == PlayerSide.None)
        {
            // Check Distance To Ball
            if ((DistanceToBall < TacticalDistanceThreshold))
            {
                // Within the Capture Distance, so Can Now Wait at Postion 
                if (!CurrentlyHasTheBall) SetPlayerWaitingAtPosition();     // Has Reached the Ball  - So Set idle (Await the Tactical Review to change Action to Dribbling Forward

                // Check if Ball is In Front of Player
                Vector3 DirectionToBall = (TheBallObject.transform.position - transform.position).normalized;
                if (Vector3.Dot(transform.forward, DirectionToBall) > 0.4f)
                {
                    // Ball is In front of Player and within Pickup so Can Make a Claim on the Free Ball Ownership
                    TheBallObject.SendMessage("RequestBallOwnership", OwnIdentity);
                } // Ball is In front of Player 
            }  // Within Capture Distance

        } // Ball is Free to perfom a Capture
    }  // CheckCaptureFreeBall
    // =========================================================================================================
    #endregion
    
    // ===========================================================================================================
    #region Basic Movement Stuff
    void PerformForwardDeltaMovement(float CharacterSpeed)
    {
        // May need a better Grounded Function, Ray cast Down Height Calculation
        Vector3 TheDeltaMovement = transform.forward;
        TheDeltaMovement.y = 0.0f;    // ** Try to Avoid Sky Walking !
        if (!TheCharController.isGrounded)
        {
            TheDeltaMovement.y = 100.0f * gravity;
        }

        if (InterPlayerCollisionCount > 0)
        {
            // Recovering Player Collision so Overide Forward By the Tackele Collision Assigned Direction
            transform.LookAt(new Vector3(TargetOpPosition.x, transform.position.y, TargetOpPosition.z));
            TheDeltaMovement = transform.forward;
            TheDeltaMovement = TheDeltaMovement * Time.deltaTime * CharacterSpeed;
            TheCharController.Move(TheDeltaMovement);
            InterPlayerCollisionCount--;
        }
        else
        {
            // Normal Movement
            TheDeltaMovement = TheDeltaMovement * Time.deltaTime * CharacterSpeed;
            TheCharController.Move(TheDeltaMovement);
        }
    }// PerformDeltaMovement
     // =========================================================================================
     // NOTE CHARACTER  CONTROLLERS DO Not use OnCollider Hit !!!  - Can only use OnControllerColliderHit
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Need to Check Player Collisions
        if ((hit.gameObject.tag == "BluePlayer") || (hit.gameObject.tag == "RedPlayer"))
        {
            // Only Move Aside if Not Owniong the Ball
            if (!CurrentlyHasTheBall)
            {
                TargetOpPosition = transform.position + transform.forward * 1.0f + transform.right * 1.0f;
                transform.LookAt(new Vector3(TargetOpPosition.x, transform.position.y, TargetOpPosition.z));
                InterPlayerCollisionCount = Random.Range(5, 10);
            }
            // Random Back Offs 
        }  // A Player Collision
    } // OnControllerColliderHit
      // =============================================================================================================
    #endregion region
    // ===========================================================================================================

    // =========================================================================================================
    #region Tactical Observations and Checks

    // ===============================================================================================
    // Tactical  Observations and Checks
    // =========================================================================================
    public bool CheckIfTeamMateIsForward()
    {
        bool TeamMateIsForward = false;
        // Review relative X Distance forward between Self and TeamPlayer

        if (TeamPlayer != null)
        {
            float TeamPlaywerX = TeamPlayer.transform.position.x;
            if ((ThePlayerSide == PlayerSide.BlueTeam) && (TeamPlaywerX < transform.position.x)) TeamMateIsForward = true;
            if ((ThePlayerSide == PlayerSide.RedTeam) && (TeamPlaywerX > transform.position.x)) TeamMateIsForward = true;
        }

        return TeamMateIsForward;
    }  // CheckIfTeamMateIsForward
    // ===============================================================================================
    public bool CheckIfClosestToBall()
    {
        bool PlayerIsClosestToBall = false;
        if (TeamPlayer != null)
        {
            // Review Team mate Distance to the Ball, and compare with own DistanceToBall
            float TeamMateDistanceToBall = Vector3.Distance(TheBallObject.transform.position, TeamPlayer.transform.position);
            if (DistanceToBall < TeamMateDistanceToBall) PlayerIsClosestToBall = true;
        }
        else PlayerIsClosestToBall = true;   // Team Mate is empty - so always closes to the Ball

        if (DecisionCountDown > 0) PlayerIsClosestToBall = false;

        return PlayerIsClosestToBall;
    }  // CheckIfClosestToBall
       // ===============================================================================================

    public bool CheckIfOppoPlayerIsClose()
    {
        bool OppoPlayerIsClose = false;
        // Review if any of the Oppo Game Objects are within OppoPlayerCloseThreshold distance
        // ASSUME Just Two Oppoplayers in the Array
        float OppoPlayer1Distance = Vector3.Distance(TheBallObject.transform.position, OppoPlayers[0].transform.position);
        if (OppoPlayer1Distance < OppoPlayerCloseThreshold) OppoPlayerIsClose = true;

        if (OppoPlayers.Length > 1)
        {
            float OppoPlayer2Distance = Vector3.Distance(TheBallObject.transform.position, OppoPlayers[1].transform.position);
            if (OppoPlayer2Distance < OppoPlayerCloseThreshold) OppoPlayerIsClose = true;
        }

        return OppoPlayerIsClose;
    }  // CheckIfOppoPlayerIsClose
    // ===============================================================================================
    public bool CheckIfAbleToStrike()
    {
        bool WithinStrikeArea = false;
        if (!CurrentlyHasTheBall) return WithinStrikeArea;     // Abort Check if currently does not Have the Ball

        Vector3 DirectionToOppoGoal = (TheOppoGoalArea.transform.position - transform.position).normalized;
        // Need to Check that is facing the Oppo Goal  - May Need to review this - as may result in dribble backwards
        if (Vector3.Dot(transform.forward, DirectionToOppoGoal) > 0.25f)
        {
            // Player Is facing the Oppo Goal  So Now Check Within Striking Range
            float DeltaZ = Mathf.Abs(transform.position.z - TheOppoGoalArea.transform.position.z);
            if (ThePlayerSide == PlayerSide.BlueTeam)
            {
                if ((transform.position.x > (TheOppoGoalArea.transform.position.x + 1.5f)) && (transform.position.x < (TheOppoGoalArea.transform.position.x + 8.0f)) && (DeltaZ < 3.5f)) WithinStrikeArea = true;
            }
            else
            {
                if ((transform.position.x < (TheOppoGoalArea.transform.position.x - 1.5f)) && (transform.position.x > (TheOppoGoalArea.transform.position.x - 8.0f)) && (DeltaZ < 3.5f)) WithinStrikeArea = true;
            }
        }
        return WithinStrikeArea;
    }  // CheckIfWithinStrikeArea
    // ==========================================================================================
    bool CheckIfKickBlocked(Vector3 TargetPosition)
    {
        bool KickDirectionBlocked = false;

        RaycastHit KickDirectionHit;
        Vector3 KickDirection = (TargetPosition - TheBallObject.transform.position).normalized;

        Vector3 AboveBallStrikePoint = new Vector3(TheBallObject.transform.position.x, +1.0f, TheBallObject.transform.position.z);

        // Debug.DrawRay(AboveBallStrikePoint, KickDirection * 12.0f, Color.red);
        if (Physics.Raycast(AboveBallStrikePoint, KickDirection, out KickDirectionHit, 14.0f))
        {
            string TheHitTag = KickDirectionHit.transform.gameObject.tag;
            if ((TheHitTag == "RedPlayer") && (ThePlayerSide == PlayerSide.BlueTeam))
            {
                // Then the Kick Direction is Obstructed by an oppo Red Player
                KickDirectionBlocked = true;
            }
            if ((TheHitTag == "BluePlayer") && (ThePlayerSide == PlayerSide.RedTeam))
            {
                // Then the Kick Direction is Obstructed by an oppo Blue Player
                KickDirectionBlocked = true;
            }
        }
        return KickDirectionBlocked;
    } // CheckIfKickBlocked
      // ======================================================================================================
    #endregion
    // ===========================================================================================================

    // ==========================================================================================================
    #region Ball Kick Actions
    // =========================================================================================
    void PerformTheBallStrike()
    {
        // Perform the Ball Strike  - Choose a Random Goal Strike Point
        CurrentlyHasTheBall = false;
        Vector3 DirectionToOppoGoal = (KickTarget - transform.position).normalized;
        TheBallObject.SendMessage("ApplyStrikeKick", DirectionToOppoGoal);

        // Predict Whether the Strike Will be Saved or Not by performing a Ray Cast Check to Check Intercept with the Oppo Goalie
        RaycastHit GoalieRayHit;
        if (Physics.Raycast(TheBallObject.transform.position, DirectionToOppoGoal, out GoalieRayHit, 14.0f))
        {
            if (GoalieRayHit.transform.gameObject.tag == "Goallie")
            {
                // Then the Strike will hit the Gaollie and be saved
                OppoGoallie.SendMessage("StartGoalSave");
            }
            else
            {
                // Will Not Intercept the Goallie and So will be a Score
                OppoGoallie.SendMessage("StartGoalMiss");
            }
        }
        else
        {
            //Debug.Log(" Srike Check: Will Score !");
            OppoGoallie.SendMessage("StartGoalMiss");
        }
    } // PerformTheBallStrike
    // =============================================================================================
    Vector3 PickAGoalAimPoint()
    {
        // Choose a Goal Strike Aim Point
        Vector3 RandomGoalPoint = TheOppoGoalArea.transform.position + new Vector3(0.0f, 0.0f, Random.Range(-2.5f, 2.5f));  // Random in Goal Sideways Z
        return RandomGoalPoint;
    } // PickAGoalAimPoint
    // ==================================================================================================

    void PerformPlayerPass()
    {
        // Perform the Balll Pass
        CurrentlyHasTheBall = false;
        Vector3 DirectionToOppoPlayer = (KickTarget - transform.position).normalized;
        TheBallObject.SendMessage("ApplyPlayerPassKick", DirectionToOppoPlayer);    // Assume Forward for Now - Change to Forward Player Position
        SetPassingTheBall();

    }  // PerformPlayerPass
    // ==============================================================================================
    #endregion

    // =====================================================================================================

    #region Ball Ownership Stuff
    // =========================================================================================
    public void ConfirmedBallOwner(int CurrentBallOwner)
    {
        TheCurrentBallOwner = CurrentBallOwner;
        // Confirm Which team has the Ball
        if (TheCurrentBallOwner == 0) CurrentBallSide = PlayerSide.None;
        else
        {
            if (TheCurrentBallOwner > 15) CurrentBallSide = PlayerSide.RedTeam;
            else CurrentBallSide = PlayerSide.BlueTeam;
        }

        // Assign Own Ball ownership
        if (CurrentBallOwner == OwnIdentity)
        {
            // If the Player does not currnelty have the Ball
            if (!CurrentlyHasTheBall)
            {
                // Ensure that the Ball is placed at Players feet
                Vector3 BallFeetPosition = transform.position + transform.forward * 0.75f + new Vector3(0.0f, 0.25f, 0.0f);
                // Update the Ball Position
                TheBallObject.SendMessage("UpdateGoalHandlingPosition", BallFeetPosition);
            }
            CurrentlyHasTheBall = true;
        }  // New Owner
        else CurrentlyHasTheBall = false;
       
    } // ConfirmedBallOwner
    // ===========================================================================================
    public void ConfirmedNewTackleOwner(int NewTackleBallOwner)
    {
        TheCurrentBallOwner = NewTackleBallOwner;
        // Confirm Which team has the Ball
        if (TheCurrentBallOwner == 0) CurrentBallSide = PlayerSide.None;
        else
        {
            if (TheCurrentBallOwner > 15) CurrentBallSide = PlayerSide.RedTeam;
            else CurrentBallSide = PlayerSide.BlueTeam;
        }

        // Assign Own Ball ownership
        if (NewTackleBallOwner == OwnIdentity)
        {
            // If the Player does not currnelty have the Ball
            if (!CurrentlyHasTheBall)
            {
                // Ensure that the Ball is placed at Players feet
                Vector3 BallFeetPosition = transform.position + transform.forward * 0.75f + new Vector3(0.0f, 0.25f, 0.0f);
                // Update the Ball Position
                TheBallObject.SendMessage("UpdateGoalHandlingPosition", BallFeetPosition);
            }
            CurrentlyHasTheBall = true;
        }  // New Owner
        else
        {
            CurrentlyHasTheBall = false;
            // May have Lost the Ball in a Tackle - so need to dealy any follow Actions
            if (TheBallController.PrevTackledOwner == OwnIdentity)
            {
                SetPlayerWaitingAtPosition();
                DecisionCountDown = 50;
            }
        }
    }  // ConfirmedNewTackleOwner
       // ============================================================================================
    public void ReviewCelebration(int ScoringPlayer)
    {
        if (ScoringPlayer == OwnIdentity) SetCelebrating();
        else
        {
            // A Goal has been scored - so just set the other players To Idle
            if (ThePlayerCurrentState != PlayerState.IdleAtPosition) SetPlayerWaitingAtPosition();
        }
        CelebrationPeriod = true;
    }  // ReviewCelebration

    // ======================================================================================================
    #endregion
    // ========================================================================================================

    // ======================================================================================================
    #region Animiations Section
    // Animations and Player Controls
    // ========= The Animation Control States =====================
    // PlayerState { IdleAtPosition, RunningToBall, RunningToPlayer, DribblingForward, PassingBall, TacklePlayer, Strike, GoalKeeping, GoalAction, Celebrating };
    void SetPlayerWaitingAtPosition()
    {
        // Set the Player Idle waiting at Postion. 
        ThePlayerCurrentState = PlayerState.IdleAtPosition;

        // Need to Ensure Looking Towards the Ball If Idle
        transform.LookAt(new Vector3(TheBallObject.transform.position.x, transform.position.y, TheBallObject.transform.position.z));

        ThePlayerAnimator.SetBool("IsRunning", false);
        ThePlayerAnimator.SetBool("IsStrikingBall", false);
        ThePlayerAnimator.SetBool("IsPassingBall", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);

    } // SetPlayerWaitingAtPosition
    // =====================================================
    void SetRunningToBall()
    {
        // Run Towards the Ball or Player With Ball
        ThePlayerCurrentState = PlayerState.RunningToBall;

        DecisionCountDown = 25;
        // Need to Ensure Looking Towrards the Ball If Idle
        transform.LookAt(new Vector3(TheBallObject.transform.position.x, transform.position.y, TheBallObject.transform.position.z));

        ThePlayerAnimator.SetBool("IsRunning", true);
        ThePlayerAnimator.SetBool("IsStrikingBall", false);
        ThePlayerAnimator.SetBool("IsPassingBall", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);

    } // SetRunningToBall
      // =====================================================

    void SetGoToAttack()
    {
        // Got to an Attacking Postion
        ThePlayerCurrentState = PlayerState.RunningToAttack;

        DecisionCountDown = 25;
        // Need to Set up the Ops Target Location
        float MidPitchX = 0.5f * (TheOwnGoalArea.transform.position.x + TheOppoGoalArea.transform.position.x);

        TargetOpPosition.z = TheOwnGoalArea.transform.position.z + Random.Range(-4.0f, 4.0f);
        if (ThePlayerSide == PlayerSide.BlueTeam) TargetOpPosition.x = MidPitchX + Random.Range(-2.0f, -6.0f);
        else TargetOpPosition.x = MidPitchX + Random.Range(2.0f, 6.0f);

        // Need to Ensure Now Looking Towards the Target Attacke Position 
        transform.LookAt(new Vector3(TargetOpPosition.x, transform.position.y, TargetOpPosition.z));

        ThePlayerAnimator.SetBool("IsRunning", true);
        ThePlayerAnimator.SetBool("IsStrikingBall", false);
        ThePlayerAnimator.SetBool("IsPassingBall", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);
    } // SetRunningToPlayer
    // =====================================================
    void SetGoToDefence()
    {
        // Got to a Defenzive Position
        ThePlayerCurrentState = PlayerState.RunningToDefence;

        DecisionCountDown = 25;
        // Need to Set up the Ops Target Location
        float MidPitchX = 0.5f * (TheOwnGoalArea.transform.position.x + TheOppoGoalArea.transform.position.x);

        TargetOpPosition.z = TheOwnGoalArea.transform.position.z + Random.Range(-3.5f, 3.5f);
        if (ThePlayerSide == PlayerSide.BlueTeam) TargetOpPosition.x = MidPitchX + Random.Range(3.0f, 7.0f);
        else TargetOpPosition.x = MidPitchX + Random.Range(-3.0f, -7.0f);

        // Need to Ensure Now Looking Towards the Target Defence Position 
        transform.LookAt(new Vector3(TargetOpPosition.x, transform.position.y, TargetOpPosition.z));

        ThePlayerAnimator.SetBool("IsRunning", true);
        ThePlayerAnimator.SetBool("IsStrikingBall", false);
        ThePlayerAnimator.SetBool("IsPassingBall", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);
    } // SetRunningToPlayer
      // =====================================================
    void SetDribblingWide()
    {
        // Dribble towards the Opposite Wing

        ThePlayerCurrentState = PlayerState.DribblingForward;
        DecisionCountDown = 50;

        int RandomWing = Random.Range(0, 100);
        if (RandomWing > 50) TargetOpPosition.z = TheOwnGoalArea.transform.position.z + 3.5f;
        else TargetOpPosition.z = TheOwnGoalArea.transform.position.z - 3.5f;
        // Dribble Close Towards other Goal, Strike Options Then should come in before reach the Other Goal Area

        if (ThePlayerSide == PlayerSide.BlueTeam) TargetOpPosition.x = TheOppoGoalArea.transform.position.x + 2.0f;
        else TargetOpPosition.x = TheOppoGoalArea.transform.position.x - 2.0f;

        // Need to Ensure Now Looking Towards the Oppo Goalie
        transform.LookAt(new Vector3(TargetOpPosition.x, transform.position.y, TargetOpPosition.z));

        ThePlayerAnimator.SetBool("IsRunning", true);
        ThePlayerAnimator.SetBool("IsStrikingBall", false);
        ThePlayerAnimator.SetBool("IsPassingBall", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);
    } // SetDribblingWide
    // ===========================================================
    void SetDribblingForward()
    {
        ThePlayerCurrentState = PlayerState.DribblingForward;

        DecisionCountDown = 50;
        // Dribble Close Towards other Goal, Strike Options Then should come in before reach the Other Goal Area
        TargetOpPosition.x = TheOppoGoalArea.transform.position.x;
        TargetOpPosition.z = TheOwnGoalArea.transform.position.z + Random.Range(-3.0f, 3.0f);

        // Need to Ensure Now Looking Towards the Oppo Goalie
        transform.LookAt(new Vector3(TargetOpPosition.x, transform.position.y, TargetOpPosition.z));

        ThePlayerAnimator.SetBool("IsRunning", true);
        ThePlayerAnimator.SetBool("IsStrikingBall", false);
        ThePlayerAnimator.SetBool("IsPassingBall", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);
    } // SetDribblingForward
    // =====================================================
    void SetPassingTheBall()
    {
        // Pass the Ball
        ThePlayerCurrentState = PlayerState.PassingBall;
        transform.LookAt(new Vector3(KickTarget.x, transform.position.y, KickTarget.z));

        DecisionCountDown = 50;
        ThePlayerAnimator.SetBool("IsRunning", false);
        ThePlayerAnimator.SetBool("IsStrikingBall", false);
        ThePlayerAnimator.SetBool("IsPassingBall", true);
        ThePlayerAnimator.SetBool("IsCelebrating", false);

        TheGameManager.SendMessage("UpdateNarrativeString", " ");  // Clear down the Narrative 
    } // SetPassingTheBall
    // =====================================================
    void SetStrikingTheBall()
    {
        // Strike the Ball
        ThePlayerCurrentState = PlayerState.StrikingTheBall;

        // Need to Set Facing the Strike Direction
        transform.LookAt(new Vector3(KickTarget.x, transform.position.y, KickTarget.z));

        ThePlayerAnimator.SetBool("IsRunning", false);
        ThePlayerAnimator.SetBool("IsStrikingBall", true);
        ThePlayerAnimator.SetBool("IsPassingBall", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);

        DecisionCountDown = 50;

        TheGameManager.SendMessage("UpdateNarrativeString", " ");  // Clear down the Narrative
    } // SetStrikingTheBall
    // ==================================================================================
    void SetCelebrating()
    {
        ThePlayerCurrentState = PlayerState.Celebrating;

        ThePlayerAnimator.SetBool("IsRunning", false);
        ThePlayerAnimator.SetBool("IsStrikingBall", false);
        ThePlayerAnimator.SetBool("IsPassingBall", false);
        ThePlayerAnimator.SetBool("IsCelebrating", true);
    } // SetCelebrating
    // ============================================================================================
    #endregion
    // ==================================================================================================================================

    // ======================================================================================================
}  // MLPlayer
