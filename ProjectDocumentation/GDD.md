# PhasmophobiAR Game Design Document

## 1. High Concept

PhasmophobiAR is an AR ghost-hunting game for phones and tablets. The player scans a real room, deploys marker-based ghost-hunting equipment, follows spatial clues, identifies the ghost type, and captures the ghost before it disappears.

The main fantasy is that the ghost exists in the player's real environment. Marker cards are not used to spawn ghosts. Instead, marker cards are used to place investigation tools, such as EMF readers, thermometers, spirit boxes, cameras, and traps.

Short pitch:

> PhasmophobiAR is an AR ghost-hunting game where players scan a real room, place physical marker-based tools, follow 3D spatial UI clues, identify ghost evidence, and capture hidden ghosts anchored in the real environment.

## 2. Target Platform

- Mobile phone or tablet.
- Primary engine: Unity.
- Primary AR framework: AR Foundation.
- Target input: touch screen and device movement.
- Optional hardware enhancement: LiDAR-capable iOS devices.
- Required camera use: yes.
- Required physical materials: printed marker cards for investigation tools.

LiDAR can improve room understanding, depth, and occlusion, but it should not be required for the first playable version. The game should work with standard AR tracking, plane detection, feature points, and world anchors.

## 3. Design Pillars

### Real Environment As The Game Space

The player should feel like the room itself is haunted. Ghosts are placed in world space after the room scan and should appear fixed in the real environment.

### Physical Tool Placement

Marker cards represent investigation equipment. Placing a card in the real room places a 3D tool at that location in AR.

### Investigation Before Capture

The player should not simply see and catch a ghost immediately. They should collect evidence, follow clues, use tools, and identify the ghost type before or during capture.

### AR And 3D UI As Gameplay

AR tracking, tool placement, spatial UI, sound cues, and ghost visibility should all affect gameplay. The AR aspect should not be only visual decoration.

## 4. Player Goal

The player's goal in each round is to:

1. Scan the environment.
2. Deploy investigation tools.
3. Locate ghost activity.
4. Collect enough evidence to identify the ghost type.
5. Reveal the ghost.
6. Capture it.
7. Record the result in the ghost journal.

The round is successful if the player captures the ghost and correctly identifies its type.

The round can fail if:

- The ghost disappears before capture.
- The player captures the ghost with too little evidence.
- Tracking becomes too unstable for too long.
- A time limit expires, if a time limit is implemented. (The player goes insane.)

## 5. Core Gameplay Loop

1. The player starts an investigation.
2. The player scans the room with the phone or tablet.
3. The game creates an AR play space and places hidden ghosts using world-space anchors.
4. The player places physical marker cards for tools.
5. The game recognizes each marker and spawns a 3D tool at that location.
6. Tools and scanner modes provide signals, sound cues, and visual hints.
7. The player follows evidence to narrow down the ghost location and type.
8. The ghost partially appears when the player is close or using the correct tool.
9. The player stabilizes the ghost by keeping it in view.
10. The player performs the capture action.
11. The game shows a result screen and updates the ghost journal.

## 6. Round Structure

### Phase 1: Setup

The player starts the investigation and prepares the marker cards.

The game shows:

- Camera feed.
- Basic scanner UI.
- Room scan prompt.
- Tracking quality indicator.

### Phase 2: Room Scan

The player slowly moves the device around the room. The game gathers enough AR tracking data to create an investigation area.

The game can gather:

- Camera movement.
- Device pose.
- Feature points.
- Tracking confidence.
- Detected planes.
- Optional LiDAR depth or mesh data.

The scan completes when:

- Tracking is stable.
- Enough movement has been detected.
- At least one usable surface or world reference exists.
- A minimum scan timer has completed.

### Phase 3: Ghost Spawn

After the scan, the game secretly places one or more ghosts in world space.

Possible spawn rules:

- Near detected walls or corners.
- Slightly above the floor plane.
- Near feature-rich areas with stable tracking.
- Near tables or large detected surfaces.
- Within a reasonable radius of the player.
- Away from the starting camera position so the player has to search.

### Phase 4: Investigation

The player places tool marker cards and scans the room. Each tool gives different evidence or signal feedback.

The player can:

- Move around the room.
- Switch scanner modes.
- Place or reposition tool cards.
- Listen for directional sound cues.
- Check the ghost journal.
- Watch 3D UI readings above placed tools.

### Phase 5: Reveal

The ghost becomes partially visible when the player has enough evidence, is close enough, or uses the correct scanner mode.

Visibility can depend on:

- Distance to ghost.
- Camera angle.
- Scanner mode.
- Correct tool usage.
- Tracking stability.
- Ghost type behavior.

### Phase 6: Capture

The player captures the ghost by performing a short interaction.

Possible capture mechanics:

- Hold the ghost centered for 2-3 seconds.
- Tap weak spots that appear on the ghost.
- Trace a symbol shown in 3D space.
- Activate a marker-based ghost trap.
- Keep the ghost visible while the trap charges.

For the first version, the recommended mechanic is:

- Keep the ghost inside a capture circle for 3 seconds while tracking is stable.

### Phase 7: Result

After capture, the game shows:

- Ghost type.
- Correct or incorrect identification.
- Evidence found.
- Capture time.
- Tracking stability score.
- Tool usage.
- Journal entry unlocked.

## 7. Player Controls

### Device Movement

The player physically moves the phone or tablet to scan and investigate the room.

### Touch Input

Touch input is used for:

- Starting the scan.
- Switching scanner modes.
- Opening the ghost journal.
- Confirming ghost type.
- Starting capture.
- Interacting with 3D UI prompts.

### Marker Interaction

The player physically places marker cards in the room. The camera detects the marker and spawns the matching tool.

## 8. Marker-Based Tools

Marker cards act as deployable ghost-hunting equipment. When a marker is recognized, the game spawns a 3D tool at the marker's world position.

### EMF Reader

Purpose:

- Detects electromagnetic activity near the ghost.

Gameplay behavior:

- Beeps faster when ghost activity is nearby.
- Shows a floating 3D meter.
- Gives strong readings for certain ghost types.

Evidence:

- EMF spike.

### Thermometer

Purpose:

- Detects cold spots or temperature drops.

Gameplay behavior:

- Shows a temperature reading above the tool.
- Creates cold thermal patches near ghost activity.
- Helps locate slow or hidden ghosts.

Evidence:

- Freezing temperature.

### Spirit Box

Purpose:

- Allows audio-based ghost interaction.

Gameplay behavior:

- Plays distorted ghost responses.
- Can reveal ghost type hints.
- Works better when the player is near the ghost.

Evidence:

- Spirit response.

### Spectral Camera

Purpose:

- Reveals temporary ghost traces.

Gameplay behavior:

- Shows ghost outlines, footprints, fingerprints, or visual distortions.
- Works best when aimed at areas with strong signal.

Evidence:

- Spectral trace.

### Ghost Trap

Purpose:

- Captures the ghost after enough evidence has been collected.

Gameplay behavior:

- Must be placed using a marker card.
- Activates when the ghost is visible or nearby.
- May require the player to keep the ghost in view while the trap charges.

Evidence:

- Not evidence by itself, but part of the capture phase.

## 9. Scanner Modes

The phone or tablet can switch between scanner modes. Each mode changes what information the player sees.

### EMF Mode

Shows signal strength based on the ghost's distance and angle relative to the camera and placed tools.

### Thermal Mode

Adds a thermal-style overlay. Cold or hot patches appear near ghost activity.

### Spectral Mode

Reveals ghost outlines and traces, but only when tracking is stable or enough evidence has been collected.

## 10. Ghost Types

Each ghost type should have simple behavior and clear evidence patterns.

### Wanderer

Behavior:

- Slowly moves around its anchor area.

Evidence tendency:

- EMF spike.
- Spectral trace.

Gameplay identity:

- Easy first ghost type.

### Shy Ghost

Behavior:

- Disappears if stared at for too long.
- Appears more often through indirect tool readings.

Evidence tendency:

- Freezing temperature.
- Spirit response.

Gameplay identity:

- Requires careful observation and tool placement.

### Static Ghost

Behavior:

- Barely moves.
- Creates heavy visual noise and signal distortion.

Evidence tendency:

- EMF spike.
- Spectral trace.

Gameplay identity:

- Easy to locate, harder to capture clearly.

### Mimic

Behavior:

- Creates fake signal peaks.
- Pretends to be other ghost types.

Evidence tendency:

- Spirit response.
- Random false readings.

Gameplay identity:

- Requires multiple tools to identify correctly.

### Fast Ghost

Behavior:

- Appears briefly.
- Moves quickly between positions.

Evidence tendency:

- EMF spike.
- Freezing temperature.

Gameplay identity:

- Harder ghost for later rounds.

## 11. Evidence And Identification

The player identifies the ghost by collecting evidence from tools and scanner modes.

Example evidence types:

- EMF spike.
- Freezing temperature.
- Spirit response.
- Spectral trace.
- Visual distortion.

Example identification rule:

- Each ghost type has two or three evidence traits.
- The player checks discovered evidence in the journal.
- The player selects a suspected ghost type before capture or on the result screen.

For the first version, a simple system is enough:

- Each ghost has two required evidence types.
- The game records evidence when the player gets a strong enough reading.
- The journal shows discovered evidence and possible ghost matches.

## 12. Ghost Journal

The ghost journal is a core feature. It stores discovered ghosts, evidence, and case files.

The journal should include:

- Current investigation evidence.
- Possible ghost types.
- Captured ghost entries.
- Short descriptions of ghost behavior.
- Tool hints.
- Capture results.

The journal gives the player a reason to investigate carefully instead of only chasing the ghost.

## 13. Sound Design

Sound cues are part of the core experience.

Important sound cues:

- EMF beeping that gets faster near ghost activity.
- Low ambience when a ghost is close.
- Directional whispers or noises using stereo panning.
- Distorted spirit box responses.
- Static bursts when tracking becomes unstable.
- Capture sounds when the ghost is being stabilized or trapped.

Sound should support spatial investigation. The player should sometimes be able to follow audio clues even before the ghost is visible.

## 14. 3D UI Aspect

Important UI elements should exist as 3D objects in AR space, not only as flat overlays.

Examples:

- Floating EMF meters above placed tools.
- 3D signal waves pointing toward ghost activity.
- Thermal particles or colored fog in the room.
- Evidence icons hovering near active tools.
- Capture circles attached to the ghost.
- Weak spots or symbols shown in world space.
- A 3D journal or case file presentation after capture.
- Debug coordinate axes for marker pose, camera pose, and world anchors.

Flat screen UI can still be used for menus, mode switching, and journal navigation, but the main investigation feedback should feel spatial.

## 15. AR Aspect

The AR aspect is central to PhasmophobiAR. The ghost and tools should feel registered to the real room.

Important AR features:

- The player scans the real room before the investigation starts.
- Ghosts are spawned into the scanned environment using world-space anchors.
- Marker cards are tracked and used to place 3D investigation tools.
- Tools remain fixed at their real-world positions after placement.
- The player physically walks around to search from different angles.
- Ghost visibility depends on distance, viewing angle, scanner mode, and tracking stability.
- 3D UI elements are placed in the environment.
- Sound cues and spatial UI guide the player toward hidden ghost activity.
- Tracking uncertainty becomes part of the gameplay through flickering, noise, and unstable ghost visibility.

This makes AR part of the mechanics, not only the presentation.

## 16. Technical Scope

First implementation:

- Unity project.
- AR Foundation.
- AR plane detection.
- AR feature-point tracking.
- AR world anchors for ghost positions.
- AR tracked images for marker-based tools.
- Basic 3D ghost model, transparent mesh, or billboard sprite.
- Simple 3D tool models.
- Distance-based signal strength.
- Camera-angle-based visibility.
- Thermal overlay using particles, transparent materials, or screen tint.
- Capture mechanic based on keeping the ghost centered.
- Ghost journal UI.
- Sound cues for tools and ghost proximity.

Optional technical enhancements:

- LiDAR depth support.
- Real occlusion.
- Room mesh visualization.
- Multiplayer.
- More advanced ghost AI.
- Persistent saved ghost journal.
- Replay/debug view with camera path and marker axes.

## 17. MVP Scope

The minimum playable version should include:

- One room scan phase.
- One hidden ghost spawned in world space.
- At least two marker-based tools.
- At least two scanner modes.
- At least three evidence types.
- At least three ghost types.
- Basic ghost journal.
- Basic sound cues.
- One capture mechanic.
- Result screen.

Recommended MVP tools:

- EMF Reader.
- Thermometer.
- Ghost Trap.

Recommended MVP scanner modes:

- EMF Mode.
- Spectral Mode.

Recommended MVP ghosts:

- Wanderer.
- Shy Ghost.
- Static Ghost.

## 18. Stretch Features

- Multiplayer mode with multiple investigators.
- One player hides ghost activity while another investigates.
- Ghosts reacting more deeply to placed tools.
- Occlusion so ghosts can hide behind real objects.
- Bad calibration mode to demonstrate AR drift.
- Replay/debug view showing marker axes, ghost path, and camera trajectory.
- Advanced ghost AI using anchor zones and tool detection ranges.
- More tool types.
- More ghost types.
- Persistent campaign or case progression.

## 19. Technical Risks

### AR Tracking Instability

If tracking is poor, ghosts and tools may drift. The game should show tracking confidence and use instability as part of the ghost effect where possible.

### Marker Detection Reliability

Marker cards must be visually clear and easy for the camera to detect. The game should handle lost markers gracefully.

### Ghost Placement

Ghosts must spawn in reachable and visible locations. The game should avoid placing ghosts too far away, behind the player, inside walls, or outside the scanned area.

### Device Performance

Thermal overlays, particles, ghost shaders, and AR tracking can be expensive on mobile. Effects should be simple in the first version.

### Scope Creep

Multiplayer, advanced occlusion, and complex ghost AI are exciting but should stay as stretch goals until the core loop works.

## 20. Demo Plan

1. Show the physical marker cards.
2. Start PhasmophobiAR on a phone or tablet.
3. Scan the room until tracking is stable.
4. The game secretly spawns a ghost in the scanned environment.
5. Place an EMF reader marker on a table.
6. A 3D EMF reader appears at that real-world position.
7. Move around the room and follow sound cues and tool readings.
8. Place a thermometer or trap marker.
9. Switch scanner modes.
10. Reveal the ghost in AR.
11. Keep the ghost centered to capture it.
12. Show the result screen.
13. Open the ghost journal entry.

## 21. Final Summary

PhasmophobiAR is an AR ghost-hunting game where the player scans a real room, deploys marker-based tools, follows sound and 3D UI clues, identifies a ghost through evidence, and captures it in AR. Ghosts are placed in world space after the room scan, while marker cards are used only for physical investigation equipment.

The most important design goal is to make the ghost feel like it exists in the real room and make the player's tools feel like real devices placed into that same space.
