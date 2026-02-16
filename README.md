# GorillaBody

GorillaBody is a [BepInEx](https://github.com/BepInEx/BepInEx) mod for *Gorilla Tag* that enables full-body tracking using SteamVR trackers. This mod enhances immersion by tracking your chest and elbows, providing more realistic avatar movements that are visible to other players who also have the mod installed.

## Features

*   **Chest Tracking:** Utilizes a tracker on your chest to control the orientation of your gorilla's body, allowing for natural leaning and turning.
*   **Elbow Tracking:** Implements a custom IK solver using elbow trackers to accurately animate your gorilla's arms, replacing the game's default IK for a more lifelike appearance.
*   **Network Synchronization:** Your body and arm movements are synchronized over the network, allowing other players with GorillaBody to see your tracked motions.
*   **In-Game Configuration:** An easy-to-use in-game menu allows you to assign trackers, adjust offsets, and fine-tune smoothing settings.
*   **Auto-Assignment:** Automatically assign roles to your trackers based on their relative positions to your headset.

## Requirements

*   A VR setup compatible with SteamVR.
*   A legitimate copy of *Gorilla Tag* on Steam.
*   [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases) installed for *Gorilla Tag*.
*   At least one, but preferably three, SteamVR-compatible trackers (e.g., Vive Trackers, Tundra Trackers).

## Installation

1.  Ensure you have BepInEx installed for *Gorilla Tag*.
2.  Download the `GorillaBody.dll` from the [latest release](https://github.com/TheRDShmackleguy/GorillaBody/releases).
3.  Move the downloaded `GorillaBody.dll` file into your `<Gorilla Tag Game Folder>/BepInEx/plugins/` directory.
4.  Launch the game with your SteamVR trackers powered on.

## Configuration

GorillaBody provides a comprehensive in-game menu for setup and customization.

1.  To open the menu, press the **'B' key** on your keyboard three times in quick succession.
2.  The menu will appear, allowing you to manage your trackers and settings.

### Tracker Assignment

*   **Auto-Assign Trackers:** The simplest way to get started. This button will attempt to assign the Chest, Left Elbow, and Right Elbow roles based on tracker positions relative to your HMD.
*   **Manual Assignment:** A list of detected trackers (by serial number) is displayed. You can manually assign roles:
    *   `SET AS CHEST`
    *   `SET AS LEFT ELBOW`
    *   `SET AS RIGHT ELBOW`
*   **Headset Mode:** You can assign your HMD as the chest tracker, which can be useful for fangames or setups without a dedicated chest tracker.

### Settings

*   **Tracker Rotation:** If your chest tracker is not oriented correctly, use the rotation buttons (`ROTATE TRACKER UP/DOWN/LEFT/RIGHT`) to align your virtual body with your physical one. Each click rotates the tracker offset by 90 degrees.
*   **Smoothing:** Adjust the sliders for `CHEST SMOOTHING` and `ELBOW SMOOTHING` to find your preferred balance between responsiveness and visual smoothness. Higher values increase smoothing but also latency.
*   **Enable Elbow Tracking:** Toggles the custom elbow IK on or off. Requires at least one elbow tracker to be assigned.
*   **Disable Tracking:** This toggle will disable all local tracking functionality. You will still be able to see the body movements of other players using the mod.

## How It Works

The mod leverages the OpenVR API to read position and rotation data from SteamVR trackers. This data is then used to drive avatar movement through a combination of techniques:

*   **Harmony Patches:** The mod injects its logic into the game by patching core methods like `VRRig.PostTick`, `VRRig.SerializeWriteShared`, and `VRRig.SerializeReadShared`. This allows it to override default behaviors for rig rotation and arm IK.
*   **Custom Inverse Kinematics (IK):** A sophisticated two-bone IK solver (`ElbowIK`) is used for arm tracking. It calculates the rotation of the upper arm and forearm bones based on the positions of the shoulder (inferred), hand (from the controller), and the elbow tracker. This provides a much more accurate representation of the player's real arm position than the game's default IK.
*   **Networking:** GorillaBody uses Photon custom events to broadcast packed rotation data to other players in the lobby. Remote players running the mod will receive this data and apply the rotations to the corresponding player's avatar, enabling everyone to see each other's full-body movements.

## Building from Source

If you wish to build the mod yourself, follow these steps:

1.  Clone the repository.
2.  Open `GorillaBody.csproj` in an IDE like Visual Studio or JetBrains Rider.
3.  Modify the `<GamePath>` property in `Directory.Build.props` to point to your *Gorilla Tag* game directory.
    ```xml
    <GamePath>C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag</GamePath>
    ```
4.  Build the solution. The project is configured to automatically place the compiled `GorillaBody.dll` into your `BepInEx/plugins/GorillaBody` folder.
