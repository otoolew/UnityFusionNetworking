Project PGM Feature Creep
Version 0.0.01
An open project for PGM member to be free and add random content. 

Hack N Plan Link - You need an account and I need to add you to the project if you want to.
https://app.hacknplan.com/p/134172/kanban?categoryId=0&boardId=361439

Authors 
Bill O'Toole        otoolew@gmail.com

Project Notes
The project is various Photon Fusion examples mashed together... so let just mash more into it and prototype our hearts away.

Photon Fusion Notes Below
Link to documentation
https://doc.photonengine.com/en-us/fusion/current/getting-started/fusion-intro
Fusion example used as project base.
https://doc.photonengine.com/en-us/fusion/current/samples/fusion-application-loop

From the Team at Photon 
NOTE: I do not use the set up below. I am in the process of documenting that now.
This example show how an outer loop for a game could be structured to work properly with Fusion when loading scenes and setting up and tearing down connections as well as providing basic matchmaking functionality.

--- DISCLAMER The documentation below is from the various Photon Fusion examples mashed together... I am in the process of revising that now....


More specifically, the example allow players to either create or join sessions with some mock attributes like game mode and map name. It presents a list of sessions to joining users and allow them to configure their avatar before loading the game scene. The example also handles both clients and hosts leaving the session and returning to the intro.

>Scenes
* `MainMenu` - The launch scene is only ever used in builds and holds only an instance of the `GameManager` singleton. Configure this instance for builds to ensure you don't accidentally build with a debug (auto connect) configuration of the App.
* `Lobby` - The intro scene contains the pre-game UI before a connection is established - this is where a topology/client mode and game type is chosen. It also contains the UI for selecting a session to join and for creating a new session. This is where the app will return to if the connection is lost or shut down.
* `2.Staging` - The staging scene is loaded once a network session is established and allow players to configure their avatar and signal to the host that they are ready to play. The app may return here whenever the players need to configure their avatar and indicate that they are ready to play.
* `X.MapY` scenes are actual game maps - each game map instantiates player avatars based on the players configuration from the staging area and tells the host when they're done loading so the game starts at the same time on all clients, even if some are slow to load. The host may move to the next game scene, all clients can disconnect at will.
* `GameOver` - The GameOver scene is essentially just a map where the players don't get an avatar. It could be used to show match results, and just takes players back to the staging area.

>Behaviours
Code in the `GameUI`, `UIComponents` and `Utility` folders are not specific to this example and will not be discussed further.
* `NetworkedGameManager` Prefab launches a networked game. It contains components `GameManager`, `LevelManager`, `SceneTransitionManager`,`FusionNetwork`. 
`GameManager` contains ethods to create and destroy a game session as well as for keeping track of active players. It implements the main Fusion callbacks.
* `NetworkPlayer` Each player gets a NetworkPlayer object when joining a session and this is also parented to the session game object to keep them alive across scene loads. The NetworkPlayer object has no in-game visual representation, it's just an encapsulation of player information that is shared with all clients.
* `Character` The player in-game avatar - controls basic movement of player characters.
* `Level` The level is simply a network object that exists in actual game scenes and is responsible for spawning the players avatar in that scene.
* `LevelManager` This is the Object Provider implementation for Fusion and controls the scene-load sequence from showing a load screen to collecting a list of loaded Network Objects.
* `Session` Once the first player is connected, a single Session object is spawned and parented to the `GameManager` so that it too will not be destroyed on load. The session controls logic for loading maps and can be access via the GameManager (`GameManager.Instance.Session`).
* `NetworkPlayer` Each player gets a NetworkPlayer object when joining a session and this is also parented to the session game object to keep them alive across scene loads. The NetworkPlayer object has no in-game visual representation, it's just an encapsulation of player information that is shared with all clients.
