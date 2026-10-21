## PhasmophobiAR - Game and Interaction Description

Course: 3D User Interfaces [IN2111]

Developers: Jennifer Wagner, Fangxing Liu, Gyuri Lee, Simon Winter

### Game Overview

PhasmophobiAR is a room-scale augmented-reality horror game for Android phones and tablets. The player's physical room becomes the game space, while the mobile device acts as the main ghost-hunting instrument.

At the start of a round, the player scans the room until AR tracking is stable. The game anchors a hidden ghost at a plausible position. The player deploys tools, collects evidence, identifies the ghost in the journal, and keeps it centred in the camera long enough to capture it.

The main gameplay loop is:

1. Scan the room.
2. Place tools and investigate.
3. Collect evidence and identify the ghost.
4. Reveal and capture the ghost.
5. Close the case and view the result.

### Input Device and Tracking

The primary input device is the player's camera-equipped mobile device. AR Foundation combines camera and inertial data to estimate a continuous six-degree-of-freedom pose: three-dimensional position plus yaw, pitch, and roll. Walking changes viewpoint and distance, while aiming changes what can be selected, revealed, or captured.

Printed image-marker cards provide a second physical input. The camera recognizes each marker and places its virtual tool at the measured position and orientation. Moving or rotating a card directly manipulates the tool in 6DoF. Touch starts the investigation, selects scanner modes, navigates the journal, confirms evidence and ghost type, and requests capture.

### Selection and Manipulation

The game combines three spatial selection techniques. Plane detection and feature tracking identify usable room geometry and establish a stable coordinate frame. Optical image tracking identifies each marker's tool type and 6DoF pose. For reveal and capture, camera direction, distance, and viewing-angle tests determine whether the ghost is inside the camera-centred zone. A reticle and progress display communicate the interaction state.

Players place and reposition markers to deploy tools at meaningful locations. Image tracking aligns each virtual tool with its card, so moving the card changes the sensing layout. The implemented EMF reader, thermometer, and spirit-response tool or spirit box each produce different feedback or evidence.

Room geometry is derived from standard AR plane and feature tracking. LiDAR depth and scene occlusion can improve registration on supported devices, but they are optional extensions and are not required by the core interaction concept.

### Movement

Movement uses continuous room-scale 6DoF tracking without a joystick, teleport command, or virtual avatar. The player walks, turns, changes height, and aims the device to investigate, reveal the ghost, and capture it.

This movement matches the level because the scanned physical room is the level. Motion is represented one-to-one in AR, preserving human scale and keeping physical obstacles visible through the camera feed.

### Wayfinding and Spatial Awareness

Wayfinding follows one guidance hierarchy. Directional audio suggests where activity may be occurring. Tool intensity then confirms proximity: the EMF signal strengthens near the ghost, the thermometer reports localized cold, and the spirit-response tool contributes evidence. Finally, anchored spectral traces or a manifestation identify the target area. The game avoids a flat minimap or explicit route.

These cues encourage the player to turn, walk, compare positions, and form a mental map. The camera feed preserves awareness of the room, while world anchors maintain the relationship between ghost, evidence, and tools. Tracking and scan feedback communicate when this coordinate frame is reliable.

Each room has different geometry, furniture, and tracking features, so the environment and marker placement dynamically shape the level. Safe spawn candidates use environmental data and controlled distances to keep the ghost inside the scanned, reachable area.

### Quality of the Interaction Concept

The desired actions and inputs are designed to match each other:

- Searching means physically looking and walking around.
- Approaching means moving closer in the real room.
- Deploying equipment means placing and repositioning a physical marker card.
- Locating activity means interpreting spatial audio, anchored traces, and tool readings.
- Revealing and capturing means aiming at the manifested ghost and holding it steadily inside the camera-centred capture zone for roughly three seconds.

Visual, audio, and tool feedback communicates tracking confidence, signal strength, evidence, reveal state, and capture progress.

The flow is recoverable wherever possible. A failed scan can be retried. Brief marker loss is handled with tracking-state logic and grace periods; stale tools are removed, and the tool returns when its marker is recognized again. Cards remain repositionable. Capture progress tolerates a short loss of alignment before resetting. After a longer interruption, only the capture attempt must be retried, not the scan or evidence phase.

### Playtest Findings and Iteration


Unfortunately, our group was unable to attend the official playtest session. However, we continuously tested the application ourselves throughout development and used the problems we encountered to improve the interaction concept and gameplay flow.

Testing exposed two important AR-specific problems. First, marker recognition became unreliable in weak lighting or at difficult viewing angles. We improved marker designs and sizing, clarified how markers should be presented to the camera, and refined the handling of full, limited, and lost tracking states.

Second, believable ghost placement was inconsistent across unpredictable rooms. The spawn logic was constrained using distance and detected environmental data. The room scan also provides a retry option when no suitable spawn position can be found.

These findings showed that spatial guidance works best when the system communicates confidence clearly. The final design combines in-world readings and directional audio with concise tracking, evidence, and capture feedback.

### Robustness and Known Constraints

Explicit round states control scanning, investigation, identification, capture, and results. Lost-marker cleanup prevents stale tools. Safe placement uses controlled distance and environmental rules, while capture checks distance, viewing angle, and tracking confidence.

As with most mobile AR systems, performance depends on camera visibility, lighting, available physical space, and sufficiently textured surfaces. The interaction concept addresses these constraints with scan feedback, retry options, marker-loss recovery, capture tolerance, and clear player prompts.
