using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheBallControl : MonoBehaviour
{
    // ==================================================================================
    private bool CurrentlyOwned;
    private Rigidbody TheBallRigidBody;
    private SphereCollider TheSphereCollider;
    public GameObject StartPosition;
    private float BallRotationRate = 4.0f;
    private bool GoalJustScored;
    
    public GameObject RedGoalArea;
    public GameObject BlueGoalArea;
    public GameObject TheGameController;
    public GameObject[] AllPlayers;

    public int CurrentOwningPlayer;
    public int PrevTackledOwner; 
    private int LastOwner;
    private int BallStuckCount;
    private float BallStuckThreshold = 0.1f;
    private Vector3 PrevBallPosition;
    private int TackleCountDown; 

    // ==================================================================================
    void Awake()
    {
        TheBallRigidBody = GetComponent<Rigidbody>();
        TheSphereCollider = GetComponent<SphereCollider>();
    } // Awake
    // ==================================================================================
    void Start()
    {
        Reset();

    } // Start
    // ==================================================================================
    public void UpdateDribblePosition(Vector3 DribblePosition)
    {
        if (CurrentlyOwned)
        {
            transform.position = DribblePosition;

            transform.Rotate(new Vector3(0.0f, 0.0f, -BallRotationRate), Space.Self);   // Needs Negative Rotatin if going twoards Red Gaol

        }
    } // UpdateDribblePosition
    // ==================================================================================
    public void UpdateGoalHandlingPosition(Vector3 HandlingPosition)
    {
        if (CurrentlyOwned)
        {
            transform.position = HandlingPosition;
        }
    } // UpdateGoalHandlingPosition
    // ==================================================================================

    public void Reset()
    {

        transform.position = StartPosition.transform.position;
        GoalJustScored = false;
        CurrentOwningPlayer = 0;
        LastOwner = 0;
        PrevTackledOwner = 0; 
        PrevBallPosition = transform.position;

        CurrentlyOwned = false;
        TheBallRigidBody.isKinematic = false;
        TheSphereCollider.enabled = true;
        TheBallRigidBody.velocity = Vector3.zero;  // Clear out any Residual Velocities
        TheBallRigidBody.angularVelocity = Vector3.zero;
        BallStuckCount = 0;

    } // Reset
      // ============================================================================
    void FixedUpdate()
    {
        // Sanity Check - That Ball is still in Motion/ Play
        float DeltaBallChange = Vector3.Distance(transform.position, PrevBallPosition);
        if (TackleCountDown > 0) TackleCountDown--;

        if (DeltaBallChange < BallStuckThreshold)
        {
            BallStuckCount++;
            if (BallStuckCount > 750)            //Note the Blue Main Player Will Need to Keep Busy Moving the Ball if Under Manual Control 
            {
                // Debug.Log(" [ERROR] Ball Has Got Stuck Restarting");
                TheGameController.SendMessage("UpdateNarrativeString", " Ball Stuck: Restarting");
                TheGameController.SendMessage("ResetKickoff");
            }
        }
        else BallStuckCount = 0;
        PrevBallPosition = transform.position;
        // ==============================================================

        // ===========================================================================================
        // Check Ball Over Goals
        if ((transform.position.x < (RedGoalArea.transform.position.x - 0.5f)) && (!GoalJustScored))
        {
            // Ball in Red Goal Area - Blue Player Has Scored
            TheGameController.SendMessage("UpdateNarrativeString", " Blue Have Scored Goal !");
            GoalJustScored = true;

            foreach (GameObject APlayer in AllPlayers) APlayer.SendMessage("ReviewCelebration", LastOwner);

        }
        if ((transform.position.x > (BlueGoalArea.transform.position.x + 0.5f)) && (!GoalJustScored))
        {
            // Ball in Blue Goal Area - Red Player Scored
            TheGameController.SendMessage("UpdateNarrativeString", " Red Have Scored Goal !");
            GoalJustScored = true;
            foreach (GameObject APlayer in AllPlayers) APlayer.SendMessage("ReviewCelebration", LastOwner);
        }

        if (transform.position.y < -10.0f)
        {
            /// Ball Has escaped out of Arena   - Just Give it to the Red Player
            TheGameController.SendMessage("UpdateNarrativeString", " Ball Was Lost");
            Debug.Log(" [ERROR] Ball Was Lost: Re Kick Off");
            TheGameController.SendMessage("ResetKickoff");
        }

        // If Goal Back in Main Pitch Area - The the Ball is outside of the goal Again
        if ((transform.position.x > (RedGoalArea.transform.position.x + 2.0)) && (transform.position.x < (BlueGoalArea.transform.position.x - 2.0))) GoalJustScored = false;

    } // FixedUpdate
      // ==================================================================================
    void OnCollisionEnter(Collision theCollision)
    {
        // 
    } // OnCollisionExit
    // =========================================================================================

    // ========================================================================================
    public void RequestBallOwnership(int OwningPlayer)
    {
        // Only Take Ownership afetr any Tackle/ Kick Count down to give some advantage to previous Owner  (But Goalkeeper alwyas has the advanatge if close to him
        if ((TackleCountDown == 0) || (OwningPlayer==23) || (OwningPlayer==13))
        {
            CurrentlyOwned = true;
            CurrentOwningPlayer = OwningPlayer;
            PrevTackledOwner = 0;
            LastOwner = OwningPlayer;
            TheBallRigidBody.isKinematic = true;
            TheSphereCollider.enabled = false;

            // Broadcast new Ball ownership to All Players
            foreach (GameObject APlayer in AllPlayers) APlayer.SendMessage("ConfirmedBallOwner", CurrentOwningPlayer);
        }

    } // RequestBallOwnership
    // ==================================================================================
    public void RemovePlayerBallOwnership()
    {
        CurrentlyOwned = false;
        CurrentOwningPlayer = 0;
        PrevTackledOwner = 0;
        TheBallRigidBody.isKinematic = false;
        TheSphereCollider.enabled = true;
        foreach (GameObject APlayer in AllPlayers) APlayer.SendMessage("ConfirmedBallOwner", CurrentOwningPlayer);

    } // RemovedPlayerControl
    // ==================================================================================
    public void RequestBallTackle(int RequestingPlayer)
    {
        int RandomChance = Random.Range(0, 100);  
        // Was >60 in Run 4 chnage to >50 in Run 6, to see if takes more chance on attempting a Pass insteasd of 'ploughing on Through 
        if ((RandomChance > 50) && (TackleCountDown == 0))
        {
            
            PrevTackledOwner = CurrentOwningPlayer; 
            CurrentlyOwned = true;
            CurrentOwningPlayer = RequestingPlayer;

            TheBallRigidBody.velocity = Vector3.zero;  // Clear out any Residual Velocities Before Kick
            TheBallRigidBody.angularVelocity = Vector3.zero;

            LastOwner = CurrentOwningPlayer;
            TheBallRigidBody.isKinematic = true;
            TheSphereCollider.enabled = false;

            TackleCountDown = 40;   // Reduce from 50

        }  // Random Choice of new Player
        foreach (GameObject APlayer in AllPlayers) APlayer.SendMessage("ConfirmedNewTackleOwner", CurrentOwningPlayer);

    } // ApplyBallTackle
    // =====================================================================================
    public void ApplyStrikeKick(Vector3 KickDirection)
    {
        
        CurrentlyOwned = false;
        CurrentOwningPlayer = 0;
        PrevTackledOwner = 0; 
        TheBallRigidBody.isKinematic = false;
        // Add some elevation
        KickDirection.y = 0.35f;
        TheBallRigidBody.velocity = Vector3.zero;  // Clear out any Residual Velocities Before Kick
        TheBallRigidBody.angularVelocity = Vector3.zero;

        TheBallRigidBody.AddForce(KickDirection * 12.5f, ForceMode.Impulse);
        TheSphereCollider.enabled = true;

        TackleCountDown = 25;

        foreach (GameObject APlayer in AllPlayers) APlayer.SendMessage("ConfirmedBallOwner", CurrentOwningPlayer);

    } // ApplyStrikeKick
      // ==================================================================================
    public void ApplyPlayerPassKick(Vector3 KickDirection)
    {
        CurrentlyOwned = false;
        CurrentOwningPlayer = 0;
        PrevTackledOwner = 0; 
        TheBallRigidBody.isKinematic = false;
        // Add some elevation
        KickDirection.y = 0.25f;
        TheBallRigidBody.velocity = Vector3.zero;  // Clear out any Residual Velocities Before Kick
        TheBallRigidBody.angularVelocity = Vector3.zero;

        TheBallRigidBody.AddForce(KickDirection * 8.0f, ForceMode.Impulse);
        TheSphereCollider.enabled = true;

        TackleCountDown = 20;

        foreach (GameObject APlayer in AllPlayers) APlayer.SendMessage("ConfirmedBallOwner", CurrentOwningPlayer);

    } // ApplyPassKick
      // ============================================================================
    public void ApplyGoaliePassKick(Vector3 KickDirection)
    {

        CurrentlyOwned = false;
        CurrentOwningPlayer = 0;
        PrevTackledOwner = 0;
        TheBallRigidBody.isKinematic = false;
        // Add some elevation
        KickDirection.y = 0.5f;
        TheBallRigidBody.velocity = Vector3.zero;  // Clear out any Residual Velocities Before Kick
        TheBallRigidBody.angularVelocity = Vector3.zero;

        TheBallRigidBody.AddForce(KickDirection * 9.0f, ForceMode.Impulse);
        TheSphereCollider.enabled = true;

        TackleCountDown = 20;

        foreach (GameObject APlayer in AllPlayers) APlayer.SendMessage("ConfirmedBallOwner", CurrentOwningPlayer);

    } // ApplyPassKick
    // ============================================================================

    public void GoalieSuperStrike(Vector3 KickDirection)
    {
        CurrentlyOwned = false;
        CurrentOwningPlayer = 0;
        PrevTackledOwner = 0;
        TheBallRigidBody.isKinematic = false;
        // Add some elevation
        KickDirection.y = 0.5f;
        TheBallRigidBody.velocity = Vector3.zero;  // Clear out any Residual Velocities Before Kick
        TheBallRigidBody.angularVelocity = Vector3.zero;

        TheBallRigidBody.AddForce(KickDirection * 12.5f, ForceMode.Impulse);
        TheSphereCollider.enabled = true;
        foreach (GameObject APlayer in AllPlayers) APlayer.SendMessage("ConfirmedBallOwner", CurrentOwningPlayer);

    } // ApplyPassKick

    // =========================================================================================
}
