- Make `Rewind.History` a message that can be sent by the server for Pawn sync'ing. Feed the sync into rollback, so the game uses it as the target frame and resimulates
- Figure out how to lower CPU and GC usage of the Input system and Cinemachine
  - Use codegen instead of UnityEvents for input
- Replace the Motor's Rigidbody with CharacterController