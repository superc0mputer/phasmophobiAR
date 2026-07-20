# PhasmophobiAR — Game and Interaction Description

**Course:** 3D User Interfaces [IN2111]  
**Developers:** Jennifer Wagner, Fangxing Liu, Gyuri Lee, Simon Winter

## Game Overview

PhasmophobiAR is a room-scale augmented-reality horror game for Android phones and tablets. The player's physical room becomes the game space, while the mobile device acts as the main ghost-hunting instrument.

At the start of a round, the player scans the room until AR tracking is stable. The game then places a hidden ghost at a plausible, anchored position in the real environment. The player deploys investigation tools, collects evidence, identifies the ghost type in the journal, and keeps the revealed ghost centered in the camera long enough to capture it.

The main gameplay loop is:

1. Scan the room.
2. Place tools and investigate.
3. Collect evidence and identify the ghost.
4. Reveal and capture the ghost.
5. Close the case and view the result.

## Input Device and Tracking

The primary input device is the player's camera-equipped mobile device. AR Foundation combines the camera image with inertial sensing to estimate a continuous six-degree-of-freedom pose: three-dimensional position plus yaw, pitch, and roll. Walking changes the player's viewpoint and distance to the ghost, while rotating and aiming the device changes what is selected, revealed, or captured.

Printed image-marker cards provide a second form of physical input. The camera recognizes each marker and places the corresponding virtual investigation tool at its measured real-world position and orientation. Moving a card therefore manipulates the associated tool directly. Touch input is used only for interface actions such as selecting scanner modes, navigating the journal, and confirming a ghost type.

## Selection and Manipulation

Camera direction and spatial tests determine whether the ghost is inside the reveal and capture zone. A reticle and live progress feedback communicate the current selection and capture state.

Players physically place and reposition printed markers to deploy tools at meaningful locations in the room. Image tracking continuously aligns the virtual tool with its card. The implemented tools include an EMF reader, thermometer, and spirit box, each of which provides different evidence or proximity feedback.

Room scanning, plane detection, feature points, and optional depth information are also used as environmental input. They help constrain ghost placement and anchor spatial effects in the physical room.

## Movement

Movement uses continuous room-scale 6-DoF tracking. There is no joystick, teleport command, or virtual avatar locomotion. The player must physically walk, turn, and aim the device to search the room, approach evidence, inspect tools, reveal the ghost, and complete the capture.

This movement concept directly matches the level design because the scanned physical room is the level. The player's real movement and device pose are represented immediately in the AR view.

## Wayfinding and Spatial Awareness

Wayfinding uses a consistent combination of anchored visual evidence, tool readings, and spatial audio. The EMF reader increases its signal near the ghost, the thermometer reports localized cold, and the spirit box provides evidence when used appropriately. Spatial audio communicates direction even when the ghost is outside the camera view.

These cues encourage the player to turn, walk, compare positions, and build an understanding of the ghost's location instead of following a flat minimap. The ghost, tracked tools, audio, and environmental effects share the same real-world coordinate frame, reinforcing spatial awareness throughout the investigation.

Each physical room has different geometry and tracking features, so the level is dynamically shaped by the player's environment. Controlled spawn distances and surface alignment keep the ghost reachable without removing the uncertainty required for investigation and horror.

## Quality of the Interaction Concept

The desired actions and inputs are designed to match each other:

- Searching means physically looking and walking around.
- Approaching means moving closer in the real room.
- Deploying equipment means placing a marker card on a real surface.
- Revealing and capturing means aiming the camera and holding the ghost steadily in view.

Immediate visual, audio, and tool feedback communicates tracking quality, signal strength, collected evidence, reveal state, and capture progress.

The interaction flow is recoverable wherever possible. A failed room scan can be retried. Temporary marker loss is handled through tracking-state logic and timeouts instead of leaving a tool permanently frozen. Capture progress tolerates a short interruption before resetting, allowing small tracking or aiming errors without forcing the entire investigation to restart.

## Playtest Findings and Iteration

Unfortunately, our group was unable to attend the official playtest session. However, we continuously tested the application ourselves throughout development and used the problems we encountered to improve the interaction concept and gameplay flow.

Testing during development exposed two important AR-specific problems. First, marker recognition became unreliable in weak lighting or at difficult viewing angles. We improved marker designs and sizing, clarified how markers should be presented to the camera, and refined the handling of different tracking states.

Second, believable ghost placement was inconsistent across unpredictable rooms. The spawn logic was constrained using distance and detected environmental data. The room scan also provides a retry option when no suitable spawn position can be found.

These findings showed that spatial guidance works best when the system also communicates its confidence clearly. The final design therefore combines in-world tool readings and spatial audio with concise on-screen tracking, evidence, and capture feedback.

## Robustness and Known Constraints

The application uses explicit round states to control the progression from room scanning through investigation, identification, capture, and results. Lost-marker cleanup prevents stale or unreachable tool objects. Ghost placement uses controlled distance and alignment rules, while capture checks distance, viewing angle, and tracking confidence.

As with most mobile AR systems, performance still depends on camera visibility, lighting, available physical space, and sufficiently textured surfaces. The interaction concept addresses these constraints with scan feedback, retry options, tracking-loss handling, and clear player prompts.
