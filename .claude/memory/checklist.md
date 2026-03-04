🌟 [Required] Global Instructions

Before entering prompts for each step, assign the following roles to Claude Code.

[ ] Separate Manual Setup Guide: For any code output requiring Inspector configuration, layer setup, or material assignment, strictly instruct the AI to record step-by-step instructions in a separate manual_setting.md file.

[ ] Compliance with Latest Rendering or Equivalent Standards: Prioritize using the latest APIs available in the environment (e.g., Render Graph, AsyncGPUReadback) or stable, equivalent modern technologies.

🧱 Phase 1: Architecture & Core Logic

Step 0: Interfaces & Events (Foundation)

[x] Define IDamageable, ITameable, ITeamable, and ITargetable interfaces.

[x] Implement static actions/events for unit death and successful taming in GlobalEvents.

[x] Verify the inheritance structure of the MonsterUnit class.

Step 1: Core Taming Logic (Faction & Reset)

[x] Implement probability-based taming logic within the TakeDamage method.

[x] On success: Immediately clear targeting info and handle Faction transition (to Player).

[x] Implement team registry updates within the CombatSystem.

Step 2: Squad Capacity Management (Flock FIFO)

[x] Implement squad size limits (e.g., 20 units) in FlockManager.

[x] Implement First-In-First-Out (FIFO) unit release and transition to FactionId.Neutral.

[x] Ensure AI State Machine reset for released units.

🗺️ Phase 2: Minimap & Fog of War (URP Tech)

Step 3: Minimap Foundation (Coordinate System)

[x] Design the MapData ScriptableObject (World Center, World Size).

[x] Implement WorldToMinimapUV conversion utility (including Mathf.Clamp01).

Step 4: Fog of War (Rendering Optimization)

[x] Implement ScriptableRenderPass or CommandBuffer compliant with the latest Render Graph or equivalent URP rendering workflows.

[x] Avoid direct RenderTexture.active manipulation; implement an efficient brush stamp logic.

[x] Optimize execution based on camera movement distance (e.g., > 0.5 units).

Step 5: Minimap UI Shader (URP HLSL)

[x] Write a pure HLSL Unlit Shader (not Shader Graph) or equivalent code.

[x] Implement smoothstep for soft fog edges.

[x] Ensure UI hierarchy instructions are stored in manual_setting.md.

Step 6: Minimap Unit Tracking (Entity Tracking)

[x] Implement a system to differentiate icons by faction (e.g., Blue/Red).

[x] Implement icon visibility logic based on Fog of War revealed/hidden data.

🎨 Phase 3: Feedback & System Integration

Step 7: Procedural Feedback (MaterialPropertyBlock)

[ ] Implement "Squash & Stretch" and "Hit Flash" using MaterialPropertyBlock for performance.

[ ] Implement a Slerp-based absorption coroutine for successful taming.

Step 8 & 9: Sound & Respawn

[ ] Integrate SoundManager for Attack, Hit, and Taming Success events.

[ ] Implement an automatic monster respawn system based on distance outside the camera's view.

💾 Phase 4: Data & Optimization

Step 10: JSON Data Persistence (Async Optimization)

[ ] Use AsyncGPUReadback or equivalent asynchronous methods to retrieve RenderTexture data.

[ ] Implement Base64 encoding for fog data and JSON serialization.

[ ] Implement a unit restoration system using ScriptableObject IDs.

Step 11: Creature Encyclopedia (Tamed Only)

[ ] Add Unique ID and Representative Color fields to MonsterData.

[ ] Implement discovery logic by subscribing to taming success events.

[ ] Store UI setup instructions (Scroll View, Grid Layout) in manual_setting.md.

Step 12: Technical Spec & Optimization Audit

[ ] Audit all distance calculations to ensure use of sqrMagnitude.

[ ] Generate a Markdown report analyzing draw calls and memory efficiency in the current URP environment.

✅ Technical Audit (Final Quality Check)

[ ] Logical Integrity: Does the generated code integrate smoothly into the environment with minimal friction and consistent logical flow?

[ ] Manual Setup Separation: Are all manual editor tasks (hyperlinks, layers, materials) documented clearly in manual_setting.md?

[ ] Performance Optimization: Are asynchronous processing and caching applied to high-frequency updates and GPU data reading?

[ ] Rendering Standard Compliance: Does the rendering logic follow the latest targeted standards or stable, modern equivalents?