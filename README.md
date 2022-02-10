# Unity ML Agent: DuffBall team play

A review of Unity ML Agents to train Agents in a simple Team play environment.  The intention is to review ML Agents playing a soccor like game, but with Tactical level Observaitons and Actions, rather than the typically base Moevement/ roations actions.  Hopefully the Training will be faster and more robust.  

Please see the results of the Trained Duff Ball Agents playing reasonably well at << You Tube Reference >>

The results are as expected. Faster and More robuste Training to achieve reasonbaly good player performance. However the Agents did not learn any need to perform collaborative play. Goal Rewards could be achieved without any collaboration due to the environmental features.  

![ScreenShot](MainScene.PNG)

### Duff Ball Overview 
The Environment is two teams playing a soccer like game. A Red and a Blue Team. There are three players, per side, however the Goalies are non trainable characters, and merely act in their own goal areas to proect their own goal, as game environmental characters.  The principle players are the two mid field players on each side, which tarverse the pitch, and have the full range of Tactical Actions available to them.  

There are two experiments: 
- Experiment A: ML Agents Blue Team is trained against a pre programmed Red Player, with some simple tactical Logic. The Training is advanced through Curriculum Learning
- Experiment B: Both the Red and Blue Side are ML Agents, and are trained as Self Play against each other.    

### Agent Set Up
The intention is to review team learning at a Tactical level, rather than a low level movement/rotation/basic action level.

So the Tactical Actions Are: {None, ChaseBallPlayer, DribbleForward, DribbleWide, Goto_Defence, Goto_Attack, StrikeBall, Pass_Ball} 
These are Masked, such that if the ML Agent Does Not have the Ball, it can only recommend: { None, ChaseBallPlayer, Goto_Defence, Goto_Attack}  and when it has the Ball the masked Actions are then : { None, DribbleForward, DribbleWide, StrikeBall, Pass_Ball} 

The Observation Space is limited to:
- float: NormalisedDistanceForward   - Distance up the Pitch
- bool: Player Has the Ball
- bool: BallTeamOwnership
- bool: PlayerIsWithin Striking Range
- bool: An Oppostion Player Is Close
- bool: The Player is Closest To the Ball
- bool: Team Mate Is Forward 

This Observation and Action Space was identified as being sufficient for a logical agent to play a basic, competitive game of Duffball. The programmed Red Player Agent, in Experiment A, used a Tactical Decision Table, based upon these Observations, to Propose its own Tactical Actions. (See PlayerControl.cs: ReviewTacticalActions() method)

There are no local Raycast sensors etc. 

As an ML Group training, the Rewards were allocated into BlueTeam Griup rewards. See GameControl.cs, where the Team Groups are set up and Registered via RegisterAgnet() calls. 
In Experiment A, with only the Blue Team being Trained, only the Blue Team is registwered. In Experiment B, fopr Self Play, both Team are Regsitsered, into two Groups,  with each Agent having their corresponding team id.

The Group Rewards are then:
- AddGroupReward(1 - 0.5f*(float)DecisionCounter / MaxNumberOfDecisions);     For a Goal by Own side, so a penalty is imposed for length game (Decsion Sequences)
- AddGroupReward(-1);      When athe Opposing team scores a Goal

The Games are coordinated by the GameControl.cs, which Resets all the players after a Goal has been scored ResetKickOff(), assigns Rewards to the ML Agent Groups, and reviews/ advances the Curriclum Learning. 

The rest of the environment is a lot of player statement management and animation control stuff, with the players transisting through their own states machines. 

Both the Goalies are managed by ther own GoalKeeperControl.cs scripts. 

The game Arena, Blue, Red Team, Ball and Game Objects are all captured within a GameEnvironment Unity Prefab. So any changes to the Agent should be done within this Prefab. This Prefab was then replicacted into 8x GameEnvironment Game Objects within the Training Scene, to speed up Training. 

The DuffBallMLConfig.yaml configuration is set up to use the Unity ML POCA Multi-Agent POsthumous Credit Assignment (MA-POCA)  algorithm, which I believe is based around PPO. 

### Experiment A : Curriculum Learning
See the original Scripts used for Curriculum Learning under the ExpACurriclum Folder.   

The 2x Blue Team players are set up as ML Agents. See the MLPlayer.cs script. 
The Red Players are left with the programemd logic See PlayerControl.cs: ReviewTacticalActions() 

The Training of the Blue Agents is then advanced through Curriculum Learning, with the Red Players having very small Speeds, and the Blue Players starting closer to the Ball at the Lower Game levels.  The Game Level is advanced up, within the GameControl.cs when the BluePlayer Scores are 125% greater than the Red Player Scores.  As the Game level increases The Red players are given higher speeds, approaching those of the Blue Players, and the player distribution after Kick Offs.  The Game Level can advance to level 12, where the teams have same performance.   

### Experiment B : Self Play Learning
Both the Red and the Blue team are assigned as being ML Agents, and so both teams have MLPlayer.cs scripts assigend to them. There are no programemd players, no use of PlayerControl.cs (Excepting the Goalies) 

The Game Manager (GameControl.cs script) assigns Group rewards to both the Blue and Red Teams, as a function of which side scored the goal. (This follows very simuilar to the Unity ML Soccer Twos example and configuration)

The Selfplay configuration is added to the DuffBallMLConfig.yaml configuration file: 
self_play:
      save_steps: 20000
      team_change: 100000
      swap_steps: 10000
      window: 10
      play_against_latest_model_ratio: 0.5
      initial_elo: 1200.0

## Conclusions

   

Happy for Any Discusssion, Comments and Improvements.

## Download and Use ##

I have captured and exported a Unity Package the DuffBall Scene, Scripts, Models etc. I am not so familiar with Unity Package export/ imports, so hopefully this is the most convinient way to import into your Unity Projects.  This can be downladed and imported into Unity, or possibly via the Unity Git import directly by reference to the .json file from the Unity Package Manager.  You will also need to import the Unity ML Agents package to run this(Note this was developed and Tested using Release 19)


## Acknowledgements ## 

- Unity ML Agents at:  https://github.com/Unity-Technologies/ml-agents
- Jason Weimann: Unity and Game Development: https://www.youtube.com/c/Unity3dCollege
- Immersive Limit: Unity Machine Learning Agents: https://www.youtube.com/c/ImmersiveLimit


