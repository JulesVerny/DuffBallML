using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalKeeperControl : MonoBehaviour
{

    public enum GoalKeeperState { WaitingAtMiddle, PassingBall, GoalKeeping, GoalSaving, GoalMissing , Celebrating};
    public enum PlayerSide { None, BlueTeam, RedTeam }

   
    // =====================================================================================================
    // =========================================================================================
    public GameObject TheBallObject;
    public GameObject TheGameManager;
    public GameObject StartPosition;
    public GameObject TheGoalieSaveColliderGO;
    public GameObject[] TeamPlayers;
    public GameObject TheOppoGoal;

    public int OwnIdentity;

    public PlayerSide ThePlayerSide;

    private float DistanceToBall;
    private TheBallControl TheBallController;

    // Motion Controls
    private CharacterController TheCharController;

    private float gravity = -20.0f;
    bool CurrentlyGrounded;
    private Vector3 DeltaLocalMovement;
    private float GoalSideSpeed = 2.0f; 

    // Character Animations 
    public GoalKeeperState TheGoalKeeperCurrentState;
    private Animator ThePlayerAnimator;
    public string CurrentAnimationName;

    // Main State Controls
    private bool CurrentlyHasTheBall;
    private float FarBallThreshold = 12.5f;
    private float PickupThreshold = 1.5f; 
    private BoxCollider TheSaveCollider;

    private float DecisionCountDown;
   
    // GameControl
    public int CurrentGameLevel;

    // Player Tactical Controls
    private int TheCurrentBallOwner;
    private PlayerSide CurrentBallSide;
   
    // =========================================================================================
    private void Awake()
    {
        TheCharController = GetComponent<CharacterController>();
        ThePlayerAnimator = GetComponentInChildren<Animator>();
 
        TheSaveCollider = TheGoalieSaveColliderGO.GetComponent<BoxCollider>();

        TheBallController = TheBallObject.GetComponent<TheBallControl>();
    }  // Awake
    // ======================================================================================================
    void Start()
    {
        CurrentGameLevel = 1;

        ResetKickOff();
        
    }
    // ======================================================================================================
    public void UpdatePlayerLevel(int NewGameLevel)
    {
        CurrentGameLevel = NewGameLevel; 

    }  // UpdatePlayerLevel
    // =====================================================================================================
    public void ResetKickOff()
    {
        // reset the Start Position and orientation
        TheCharController.enabled = false;
        transform.position = StartPosition.transform.position;
        transform.rotation = StartPosition.transform.rotation;
        TheCharController.enabled = true;

        CurrentlyHasTheBall = false;
        TheCurrentBallOwner = 0;
        DecisionCountDown = 0;
        CurrentBallSide = PlayerSide.None;
        TheSaveCollider.enabled = false;
       // HasJustMissed = false;

        SetPlayerWaitingAtPosition();

        if (ThePlayerSide== PlayerSide.RedTeam)
        {
            // Adjust reduce the Goal Keeper Performance asa Function of Game Level
            // Note the SaveThrehoild NO Longer has any Impact !

            if(CurrentGameLevel<3) 
            {
                GoalSideSpeed = 0.5f;
            }
            if ((CurrentGameLevel >= 3) && (CurrentGameLevel <= 6))
            {   
                GoalSideSpeed = 1.0f;
            }
            if ((CurrentGameLevel > 6) && (CurrentGameLevel <= 9))
            {  
                GoalSideSpeed = 1.5f;
            }

            if (CurrentGameLevel >8 )
            {
                GoalSideSpeed = 2.0f;
            }
        }  // Red team Gaolie Adjustments
    }  // ResetKickOff

    // ================================================================================================
    // Update is called once per frame
    void Update()
    {
        // No User Controls on Goal Keepers
    } // Update
    // ======================================================================================================

    private void FixedUpdate()
    {
        if (DecisionCountDown > 0) DecisionCountDown--;
        CurrentAnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;

        // Need to Check the Ball State, Proximity and Ownership
        if (CurrentlyHasTheBall)
        {
            // Update the Ball Handling Position, Just in Front of Player
            Vector3 BallHandlingPosition = transform.position + transform.forward * 0.9f + transform.right * 0.25f+ new Vector3(0.0f, 0.25f,0.0f);
            TheBallObject.SendMessage("UpdateGoalHandlingPosition", BallHandlingPosition); 
        }
        else
        {
            // Goal Keeper Does not currently Have the Ball 
            DistanceToBall = Vector3.Distance(transform.position, TheBallObject.transform.position);

            if (DistanceToBall > FarBallThreshold)
            {
                // Far Far Away so can Chill
                if ((TheGoalKeeperCurrentState != GoalKeeperState.WaitingAtMiddle) && (TheGoalKeeperCurrentState != GoalKeeperState.Celebrating))
                {
                    SetPlayerWaitingAtPosition();
                }
            }
            else
            {
                // In Own Half so Check who has the Ball
                if (CurrentBallSide == ThePlayerSide)
                {
                    // Don't worry Own Side has the Ball
                    if ((TheGoalKeeperCurrentState != GoalKeeperState.WaitingAtMiddle) && (TheGoalKeeperCurrentState != GoalKeeperState.Celebrating)) SetPlayerWaitingAtPosition();
                }
                else
                {
                    // The Ball is Free (Not Owned), and on Goallies side of Pitch   
                    // So Typically will need to be Goal Keeping if NOT Saving, Missing or Just Missd the Goal

                    // If Not Currently perfoming a Save or a Miss
                    
                    if (!((CurrentAnimationName == "GoalSave") || (CurrentAnimationName == "GoalMiss") || (CurrentAnimationName == "Victory")))
                    {
                        if (BallIsBehindGoalKeeper())
                        {
                            // Presume that has just followed a Missed Save, and so needs to Wait in an Idle State, Until celebrate and Kick Off Restart
                            if ((TheGoalKeeperCurrentState != GoalKeeperState.WaitingAtMiddle) && (TheGoalKeeperCurrentState != GoalKeeperState.Celebrating)) SetPlayerWaitingAtPosition();
                        }
                        else
                        {
                            // Otherwise Should Normally be back into Goal Keeping  - So Long as Still not in Goal Saving Missing States
                            if (!((TheGoalKeeperCurrentState == GoalKeeperState.GoalSaving) || (TheGoalKeeperCurrentState == GoalKeeperState.GoalMissing)))
                            {
                                if (TheGoalKeeperCurrentState != GoalKeeperState.GoalKeeping) SetGoalKeeping();
                            }
                            // But If we are then in a Goal Keeping Mode and the Ball is in close proximrty - really need to Check if Should be Picking U the Ball to Kick away
                            if ((TheGoalKeeperCurrentState == GoalKeeperState.GoalKeeping) && (DistanceToBall < PickupThreshold) && (DecisionCountDown == 0))
                            {
                                // Take Ownership of the Ball  and Pass the Ball
                                TheBallObject.SendMessage("RequestBallOwnership", OwnIdentity);
                                if ((TheGoalKeeperCurrentState != GoalKeeperState.PassingBall)) SetBallPassing();
                            }  // Goal Keeper Pickup 

                        }  // Ball is in front of Goalie
                    }  // If Not in a Goal Saving, Missing State
                    // Outside of a Save or a Miss Animation 
                }  // Other Side Has the Ball (in Own Half) 

            } // Ball is in Own Half of Pitch
              // ========================================================

            // Perform Goalie Movements  For Goal Keeping and Aligning to centre of Goal
            DeltaLocalMovement = Vector3.zero;
            if (TheGoalKeeperCurrentState == GoalKeeperState.GoalKeeping)
            {
                // Move towards in Proportion to Ball offset from Goal Centre Z   ==>  Average of Ball + Goal Centre Z 
                float DeltaGoalZ = transform.position.z - 0.5f *(TheBallObject.transform.position.z + StartPosition.transform.position.z);

                if (ThePlayerSide == PlayerSide.RedTeam) DeltaLocalMovement = transform.right * GoalSideSpeed * DeltaGoalZ;
                else DeltaLocalMovement = -transform.right * GoalSideSpeed * DeltaGoalZ;
            }
            if (TheGoalKeeperCurrentState == GoalKeeperState.WaitingAtMiddle)
            {
                // Move towards Goal Centre Z   => Goal centreZ
                float DeltaGoalZ = (transform.position.z - StartPosition.transform.position.z);

                if (ThePlayerSide == PlayerSide.RedTeam) DeltaLocalMovement = transform.right * GoalSideSpeed * DeltaGoalZ;
                else DeltaLocalMovement = -transform.right * GoalSideSpeed * DeltaGoalZ;
            }
            // Now Move the Goalie (Sideways)
            PerformDeltaMovement(DeltaLocalMovement);

        }  // Does NOT have the Ball

        // =================================================
        // Check Completed Animations
        CurrentAnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        // Check Completed Goal Action
        if (CurrentAnimationName == "GoalSave")
        {
            // Take Ball Ownership Half Way through Save 
            if ((ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f) && (!CurrentlyHasTheBall))
            {
                TheBallObject.SendMessage("RequestBallOwnership", OwnIdentity);
            }  // 50% of Save

            // Only Change Player State when Animation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f)
            {
                // Now Goal keeper Pass
                if (TheGoalKeeperCurrentState != GoalKeeperState.PassingBall) SetBallPassing();    

            }  // 100 % Complte
        }  // Check GoalSave Action
        // ====================================
        if (CurrentAnimationName == "GoalMiss")
        {
            // Only Change Player State when Animation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.95f)
            {
                SetPlayerWaitingAtPosition(); 

            }  // 100 % Complte
        }  // Check GoalMiss Action
        //==================================================
        // Check Completed Celebration
        if ((CurrentAnimationName == "Victory")  && (TheGoalKeeperCurrentState == GoalKeeperState.Celebrating))
        {
            // Only Change Player State when Animation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f)
            {
                // Finished Celebrating So Now Assign the Score Line  (Which Will also End the Training Episode) 
                if (ThePlayerSide == PlayerSide.BlueTeam) TheGameManager.SendMessage("IncrementBlueScore");
                else TheGameManager.SendMessage("IncrementRedScore");

            }  // 100 % Complte
        }  // Check End of Celebration
        // ==================================================
        if ((CurrentAnimationName == "Strike") && (TheGoalKeeperCurrentState == GoalKeeperState.PassingBall))
        {
            if ((ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.1f) && (CurrentlyHasTheBall))
            {
                // Now Shoot the Ball Foward  
                DecisionCountDown = 25;

                PerformAGoallieKickOut(); 

            }  // 25 % Complete  - Fire the Ball
            // =================================================
            
            // =====================================================
            // Only Change Player State when Animation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f)
            {
                SetPlayerWaitingAtPosition();

            }  // 100 % Complete
            // ==============================================
        }  // Check Pass Action
        // ========================================================================

    }  // Fixed Update
    // ======================================================================================================
    void PerformAGoallieKickOut()
    {
        // Random Choice of Actions
        int RandomChoice = Random.Range(0, 100);

        if(RandomChoice>65)
        {
            Vector3 DirectionToOpposGoal = new Vector3(TheOppoGoal.transform.position.x, 0.0f, Random.Range(-3.75f, 3.75f)).normalized;
            // Perform a Super Strike
            TheBallObject.SendMessage("GoalieSuperStrike", DirectionToOpposGoal);
        }
        else
        {
            // Direct a Pass to one of the players
            Vector3 DirectionToATeamPlayer = transform.forward;
            if (TeamPlayers.Length == 1)
            {
                // Only One Team Player so Pass to Him
                DirectionToATeamPlayer = (TeamPlayers[0].transform.position - transform.position).normalized;
            }
            else
            {
                // Random Chocie between Both Players
                if (RandomChoice < 33)
                {
                    DirectionToATeamPlayer = (TeamPlayers[0].transform.position - transform.position).normalized;
                    if (CheckIfKickBlocked(TeamPlayers[0].transform.position)) DirectionToATeamPlayer = (TeamPlayers[1].transform.position - transform.position).normalized;
                }
                else
                {
                    DirectionToATeamPlayer = (TeamPlayers[1].transform.position - transform.position).normalized;
                    if (CheckIfKickBlocked(TeamPlayers[1].transform.position)) DirectionToATeamPlayer = (TeamPlayers[0].transform.position - transform.position).normalized;
                }
            }
            
            TheBallObject.SendMessage("ApplyGoaliePassKick", DirectionToATeamPlayer);
        }  // A pass to a Player
    
    }  // PerformAGoallieKickOut
    // =======================================================================================================
    void PerformDeltaMovement(Vector3 TheDeltaMovement)
    {
        // May need a better Grounded Function, Ray cast Down Height Calculation
        TheDeltaMovement.y = 0.0f;    // ** Try to Avoid Sky Walking !
        if (!CurrentlyGrounded)
        {
            TheDeltaMovement.y = 100.0f * gravity;
        }
        TheDeltaMovement = TheDeltaMovement * Time.deltaTime;
        // Now Perform the actual Character Contoller Movement    
        TheCharController.Move(TheDeltaMovement);

    }// PerformDeltaMovement
     // ==========================================================================
    public void StartGoalMiss()
    {
        if (TheGoalKeeperCurrentState != GoalKeeperState.GoalMissing) SetGoalMiss();
    } // StartGoalMiss
    // ========================================================================
    public void StartGoalSave()
    {
        if (TheGoalKeeperCurrentState != GoalKeeperState.GoalSaving) SetGoalSave();

    }  // StartGoalSave
    // ========================================================================
    bool BallIsBehindGoalKeeper()
    {
        bool BallIsBehind = false;

        if(ThePlayerSide== PlayerSide.BlueTeam)
        {
            // Blue Goal Keeper
            if (TheBallObject.transform.position.x > (transform.position.x + 0.75f)) BallIsBehind = true; 
        }
        else
        {
            // Red Goal Keeper
            if (TheBallObject.transform.position.x < (transform.position.x - 0.75f)) BallIsBehind = true;
        }
        return BallIsBehind;
    } // BallIsBehindGoalKeeper
   
    // =======================================================================
    void OnCollisionEnter(Collision theCollision)
    {
        if (theCollision.gameObject.tag == "Floor") CurrentlyGrounded = true;

    }  // OnCollisionEnter

    //consider when character is jumping .. it will exit collision.
    void OnCollisionExit(Collision theCollision)
    {
        if (theCollision.gameObject.name == "Floor") CurrentlyGrounded = false;
    } // OnCollisionExit
    // ===============================================================================================
  

    // ==============================================================================
    public void ReviewCelebration(int ScoringPlayer)
    {
        if (ScoringPlayer == OwnIdentity) SetCelebrating();
    }  // ReviewCelebration

    // ===============================================================================================
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
            CurrentlyHasTheBall = true;
            // For Now set the Gaol Saving Mode
        }
        else
        {
            CurrentlyHasTheBall = false;
        }
    } // ConfirmedBallOwner
    // ======================================================================================================
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
        if (NewTackleBallOwner == OwnIdentity) CurrentlyHasTheBall = true;
        else
        {
            CurrentlyHasTheBall = false;
            // May have Lost the Ball in a Tackle - so need to dealy any follow Actions
            if (TheBallController.PrevTackledOwner == OwnIdentity)
            {
                if((TheGoalKeeperCurrentState != GoalKeeperState.WaitingAtMiddle) && (TheGoalKeeperCurrentState != GoalKeeperState.Celebrating)) SetPlayerWaitingAtPosition(); 
                DecisionCountDown = 50;
            }
        }
    }  // ConfirmedNewTackleOwner
       // ============================================================================================
    bool CheckIfKickBlocked(Vector3 TargetPosition)
    {
        bool KickDirectionBlocked = false;

        RaycastHit KickDirectionHit;
        Vector3 KickDirection = (TargetPosition - TheBallObject.transform.position).normalized;

        Vector3 AboveBallStrikePoint = new Vector3(TheBallObject.transform.position.x, +1.0f, TheBallObject.transform.position.z);

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
    // ============================================================================================

    // =====================================================
    // Player Animation Controls:   { IdleAtPosition, PassingBall, GoalKeeping, GoalSaving, GoalMissing }
    //
    void SetPlayerWaitingAtPosition()
    {

        TheGoalKeeperCurrentState = GoalKeeperState.WaitingAtMiddle; 
        TheSaveCollider.enabled = false;

        ThePlayerAnimator.SetBool("IsGoalKeeping", false);
        ThePlayerAnimator.SetBool("IsSaving", false);
        ThePlayerAnimator.SetBool("IsPassing", false);
        ThePlayerAnimator.SetBool("IsMissing", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);

    } // SetPlayerWaitingAtPosition
    // ==============================================================================================
    void SetGoalKeeping()
    {

        TheGoalKeeperCurrentState = GoalKeeperState.GoalKeeping;
        TheSaveCollider.enabled = false;

        ThePlayerAnimator.SetBool("IsGoalKeeping", true);
        ThePlayerAnimator.SetBool("IsSaving", false);
        ThePlayerAnimator.SetBool("IsPassing", false);
        ThePlayerAnimator.SetBool("IsMissing", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);
    } // SetGoalKeeping

    // =====================================================
    void SetGoalMiss()
    {
        TheGoalKeeperCurrentState = GoalKeeperState.GoalMissing;
        TheSaveCollider.enabled = false;

        ThePlayerAnimator.SetBool("IsGoalKeeping", false);
        ThePlayerAnimator.SetBool("IsSaving", false);
        ThePlayerAnimator.SetBool("IsPassing", false);
        ThePlayerAnimator.SetBool("IsMissing", true);
        ThePlayerAnimator.SetBool("IsCelebrating", false);

    } // SetGoalMiss
    // =====================================================
    void SetGoalSave()
    {
        TheGoalKeeperCurrentState = GoalKeeperState.GoalSaving;       
        TheSaveCollider.enabled = true;

        ThePlayerAnimator.SetBool("IsGoalKeeping", false);
        ThePlayerAnimator.SetBool("IsSaving", true);
        ThePlayerAnimator.SetBool("IsPassing", false);
        ThePlayerAnimator.SetBool("IsMissing", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);
    } // SetGoalMiss

    // ====================================================
    void SetBallPassing()
    {
        TheGoalKeeperCurrentState = GoalKeeperState.PassingBall;
        TheSaveCollider.enabled = false;

        ThePlayerAnimator.SetBool("IsGoalKeeping", false);
        ThePlayerAnimator.SetBool("IsSaving", false);
        ThePlayerAnimator.SetBool("IsPassing", true);
        ThePlayerAnimator.SetBool("IsMissing", false);
        ThePlayerAnimator.SetBool("IsCelebrating", false);
    } // SetGoalMiss

    // =======================================================
    void SetCelebrating()
    {

        TheGoalKeeperCurrentState = GoalKeeperState.Celebrating;       
        TheSaveCollider.enabled = false;

        ThePlayerAnimator.SetBool("IsGoalKeeping", false);
        ThePlayerAnimator.SetBool("IsSaving", false);
        ThePlayerAnimator.SetBool("IsPassing", false);
        ThePlayerAnimator.SetBool("IsMissing", false);
        ThePlayerAnimator.SetBool("IsCelebrating", true);
    } // SetCelebrating
    // ======================================================================================================

}
