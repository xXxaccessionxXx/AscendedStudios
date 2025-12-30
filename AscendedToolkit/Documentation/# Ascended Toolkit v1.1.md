# Ascended Toolkit v1.1.0
**Elevate your Unity Workflow**

The Ascended Toolkit is a professional suite of editor utilities designed to accelerate level design, automate repetitive tasks, and provide deep debugging insights.

## 🚀 Getting Started

### Access the Dashboard
* **Menu:** Go to `Ascended > Dashboard` in the top toolbar.
* **Shortcut:** Press **`Alt + A`** to toggle the Hub instantly.

All tools can be launched from the Dashboard, which is organized into three tabs: **Scene**, **Workflow**, and **Analysis**.

---

## 🏗️ Scene Management (Tab 1)

Tools for organizing the Hierarchy and manipulating objects in 3D space.

### 1. Smart Grouper (`Ctrl + G` / `Cmd + G`)
Clean up your Hierarchy instantly.
* **How to use:** Select multiple objects and press `Ctrl + G`.
* **Features:**
    * Creates a parent object at the exact center of selection.
    * **Color Coding:** Automatically paints the Hierarchy row for easier visibility.
    * **Physics Groups:** Can auto-add Rigidbodies and Colliders to the group.
    * **Undo Support:** Fully reversible.

### 2. Raycast Aligner
Stop eyeballing Y-positions. Snap objects to the ground perfectly.
* **How to use:** Select objects (floating or buried) and click **"Drop to Surface"**.
* **Features:**
    * **Pivot Correction:** Intelligently calculates the bottom of the mesh so objects land on their feet, not their pivots.
    * **Randomization:** Use the "Naturalize" section to apply random Rotation (Y-axis) and Scale to make forests/debris look organic.
    * **Layer Masking:** Supports ignoring specific layers (like "Water" or "UI").

### 3. Pivot Doctor
Fix off-center pivots without opening Blender.
* **How to use:** Select an object, click **"Pick New Pivot"**, and click any vertex on the mesh (e.g., a door hinge or corner).
* **Logic:** Creates a permanent parent wrapper at the target location.

### 4. Quick Renamer
Batch rename objects with power.
* **Modes:**
    * **Simple:** Find & Replace text.
    * **Regex:** Use Regular Expressions for complex pattern matching (e.g., remove all numbers `\d`, swap formats).
* **Live Preview:** See the new names before you hit Apply.

---

## ⚡ Workflow Efficiency (Tab 2)

Tools that speed up navigation and asset creation.

### 5. Ascended Hotbar
Your personal bookmarks bar.
* **Scenes:** Add your frequently used scenes (MainMenu, Sandbox) for one-click switching.
* **Assets:** Drag Prefabs/Scripts onto the shelf to keep them handy regardless of which folder you are in.
* **Data:** Favorites are saved locally to your machine, preserving your workflow across projects.

### 6. Snapshot Studio
Turn 3D models into 2D UI Sprites instantly.
* **How to use:** Drag a prefab into the "Subject" slot, adjust the angle, and click **"Save PNG"**.
* **Features:**
    * Transparent backgrounds.
    * Auto-framing camera.
    * Saves directly to `Assets/UI/Icons`.

### 7. Scene Memo (`Ctrl + Right Click`)
Leave 3D sticky notes for yourself or your team.
* **Quick Add:** Hold **Ctrl + Right Click** on any object in the Scene View to place a note.
* **Memo Board:** View a list of all active tasks, find them in the scene, and mark them as "Resolved".
* **Visuals:** Notes appear as floating labels in both Scene and Game views.

### 8. Selection History
Never lose track of what you just clicked.
* **Usage:** Automatically tracks the last 20 objects you selected.
* **Feature:** Click any item in the list to re-select and ping it.

---

## 🔍 Analysis & Repair (Tab 3)

Tools for debugging code and fixing errors.

### 9. Ascended Debugger
A better Console.
* **Translation:** Click any error to see a "Plain English" explanation of what went wrong.
* **Code Preview:** See the exact line of code that crashed *inside* the Editor, without opening Visual Studio.
* **AI Context:** One-click "Copy for AI" button to paste the error + stack trace + code context into ChatGPT/Gemini.

### 10. Save State Manager
God-mode for `PlayerPrefs`.
* **Monitor:** Add keys like "PlayerScore" or "Coins" to a watchlist.
* **Edit:** Change values in real-time while the game is running.
* **Nuke:** "Delete All" button to reset game progress for testing.

### 11. Missing Script Fixer
The "Yellow Warning" killer.
* **Scan:** Finds all objects in the scene/project with broken script references (Missing MonoBehaviours).
* **Fix:** Removes the null components in one click.

---

## ⚙️ Technical Notes

* **Input System:** The toolkit automatically detects if you are using the *New Input System* or the *Legacy Input Manager* and adjusts the `Scene Memo` shortcuts accordingly.
* **Undo System:** All tools register operations with the Unity Undo system (`Ctrl+Z` works everywhere).
* **Icons:** Dashboard icons use Unity's internal library to ensure they look crisp on any screen resolution.

**Created with the Ascended Toolkit.**