using System.Collections.Generic;
using ExitGames.Client.Photon;
using GorillaBody;
using GorillaExtensions;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

namespace GorillaBody.Patches;

public static class Patches
{
    private static readonly List<int> TargetActorCache = new(10);
    private static readonly int[] ElbowPackedData = new int[9]; 
    private static readonly Quaternion BoneAlignOffset = Quaternion.Euler(0f, -90f, 0f);

    [HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
    internal class VRRigPostTickPatch
    {
        private static void Postfix(VRRig __instance)
        {
            if (__instance.isOfflineVRRig)
                HandleLocalRig(__instance);
            else
                HandleRemoteRig(__instance);
        }

        private static void HandleLocalRig(VRRig rig)
        {
            var plugin = BodyTrackingClass.Instance;
            if (plugin is not { enabled: true })
                return;

            if (plugin.chestFollow == null)
                return;

            if (BodyTrackingClass.DisableMod is { Value: true })
                return;

            if (!plugin.trackersInitialized || !plugin.trackerSetUp)
                return;

            rig.transform.rotation = plugin.chestFollow.transform.rotation;

            if (plugin.IsSpineEnabled)
                ApplyLocalSpine(rig, plugin);

            if (plugin.IsHeadLeanEnabled)
                ApplyLocalHeadLean(rig, plugin);

            rig.head.MapMine(rig.scaleFactor, rig.playerOffsetTransform);
            rig.rightHand.MapMine(rig.scaleFactor, rig.playerOffsetTransform);
            rig.leftHand.MapMine(rig.scaleFactor, rig.playerOffsetTransform);

            ApplyLocalElbowTracking(rig, plugin);
            ApplyLocalFingerTracking(rig, plugin);
        }

        private static void ApplyLocalSpine(VRRig rig, BodyTrackingClass plugin)
        {
            ref var spine = ref plugin.GetSpineResult();

            var rigTransform = rig.transform;
            var lowerSpine = rigTransform.Find("rig/body/spine");
            var upperSpine = rigTransform.Find("rig/body/spine/chest");

            if (lowerSpine != null)
                lowerSpine.rotation = spine.LowerSpineRotation;

            if (upperSpine != null)
                upperSpine.rotation = spine.UpperSpineRotation;
        }

        private static void ApplyLocalHeadLean(VRRig rig, BodyTrackingClass plugin)
        {
            ref var spine = ref plugin.GetSpineResult();

            if (Mathf.Abs(spine.HeadLeanAngle) < 0.5f) return;

            var headBone = rig.transform.Find("rig/body/head");
            if (headBone == null) return;

            var leanRotation = Quaternion.AngleAxis(spine.HeadLeanAngle, spine.HeadLeanAxis);
            headBone.rotation = leanRotation * headBone.rotation;
        }

        private static void HandleRemoteRig(VRRig rig)
        {
            if (!BodyTrackingClass.TryGetRemoteElbowData(rig, out var elbowInfo))
                return;

            if (!elbowInfo.HasData)
                return;

            float t = Time.deltaTime * 12f;

            ApplyRemoteArm(rig, elbowInfo.TargetLeftUpper, elbowInfo.TargetLeftForearm, true, t);
            ApplyRemoteArm(rig, elbowInfo.TargetRightUpper, elbowInfo.TargetRightForearm, false, t);
            ApplyRemoteSpine(rig, elbowInfo.TargetUpperSpine, elbowInfo.TargetLowerSpine, t);
            ApplyRemoteHeadLean(rig, elbowInfo.TargetHeadLean, t);
            ApplyRemoteFingers(rig, elbowInfo.FingerCurlsLeft, elbowInfo.FingerCurlsRight, t);
        }

        private static void ApplyLocalElbowTracking(VRRig rig, BodyTrackingClass plugin)
        {
            var gorillaIK = rig.GetComponent<GorillaIK>();
            if (gorillaIK == null || gorillaIK.enabled)
                return;

            if (plugin.HasLeftElbow)
            {
                ref var result = ref plugin.GetLeftElbowResult();
                ApplyArmResult(rig, result, true);
            }

            if (plugin.HasRightElbow)
            {
                ref var result = ref plugin.GetRightElbowResult();
                ApplyArmResult(rig, result, false);
            }
        }
        
        private static void ApplyLocalFingerTracking(VRRig rig, BodyTrackingClass plugin)
        {
            // Placeholder for local finger application if bones are accessible
        }

        private static void ApplyArmResult(VRRig rig, ElbowResult result, bool isLeft)
        {
            var rigTransform = rig.transform;

            var upperArm = rigTransform.Find(isLeft
                ? "rig/body/shoulder.L/upper_arm.L"
                : "rig/body/shoulder.R/upper_arm.R");

            if (upperArm == null) return;

            var forearm = upperArm.Find(isLeft ? "forearm.L" : "forearm.R");
            if (forearm == null) return;

            upperArm.rotation = result.UpperArmRotation * BoneAlignOffset;
            forearm.rotation = result.ForearmRotation * BoneAlignOffset;
        }

        private static void ApplyRemoteArm(VRRig rig, Quaternion targetUpper, Quaternion targetFore, bool isLeft, float t)
        {
            var rigTransform = rig.transform;

            var upperArm = rigTransform.Find(isLeft
                ? "rig/body/shoulder.L/upper_arm.L"
                : "rig/body/shoulder.R/upper_arm.R");

            if (upperArm == null) return;

            var forearm = upperArm.Find(isLeft ? "forearm.L" : "forearm.R");
            if (forearm == null) return;

            upperArm.rotation = Quaternion.Slerp(upperArm.rotation, targetUpper * BoneAlignOffset, t);
            forearm.rotation = Quaternion.Slerp(forearm.rotation, targetFore * BoneAlignOffset, t);
        }

        private static void ApplyRemoteSpine(VRRig rig, Quaternion targetUpper, Quaternion targetLower, float t)
        {
            var rigTransform = rig.transform;

            var lowerSpine = rigTransform.Find("rig/body/spine");
            var upperSpine = rigTransform.Find("rig/body/spine/chest");

            if (lowerSpine != null)
                lowerSpine.rotation = Quaternion.Slerp(lowerSpine.rotation, targetLower, t);

            if (upperSpine != null)
                upperSpine.rotation = Quaternion.Slerp(upperSpine.rotation, targetUpper, t);
        }

        private static void ApplyRemoteHeadLean(VRRig rig, Quaternion targetHeadLean, float t)
        {
            var headBone = rig.transform.Find("rig/body/head");
            if (headBone != null)
            {
                headBone.rotation = Quaternion.Slerp(headBone.rotation, targetHeadLean * headBone.rotation, t);
            }
        }
        
        private static void ApplyRemoteFingers(VRRig rig, float[] leftCurls, float[] rightCurls, float t)
        {
            // Placeholder
        }
    }

    [HarmonyPatch(typeof(VRRig), nameof(VRRig.SerializeWriteShared))]
    internal class VRRigSerializeWriteSharedPatches
    {
        private static void Prefix(VRRig __instance, out Quaternion __state)
        {
            __state = Quaternion.identity;
            
            if (!__instance.isOfflineVRRig) return;
            
            var plugin = BodyTrackingClass.Instance;
            if (plugin == null || BodyTrackingClass.DisableMod is { Value: true }) return;

            // **FIX HERE:** Check for ModSided, as it's the only mode that hides rotation from vanilla
            if (plugin.CurrentVisibilityMode == VisibilityMode.ModSided)
            {
                __state = __instance.transform.rotation;
                
                var headYaw = __instance.head.rigTarget.eulerAngles.y;
                __instance.transform.rotation = Quaternion.Euler(0, headYaw, 0);
            }
        }

        private static void Postfix(VRRig __instance, Quaternion __state)
        {
            if (!__instance.isOfflineVRRig)
                return;

            var plugin = BodyTrackingClass.Instance;
            if (plugin is not { trackerSetUp: true, trackersInitialized: true })
                return;

            if (BodyTrackingClass.DisableMod is { Value: true })
                return;

            // **FIX HERE:** Check for ModSided, as it's the only mode that needs its rotation restored
            if (plugin.CurrentVisibilityMode == VisibilityMode.ModSided && __state != Quaternion.identity)
            {
                __instance.transform.rotation = __state;
            }

            if (!PhotonNetwork.InRoom)
                return;

            TargetActorCache.Clear();
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal) continue;

                if (player.CustomProperties is { } props &&
                    props.ContainsKey(BodyTrackingClass.Prop))
                {
                    TargetActorCache.Add(player.ActorNumber);
                }
            }

            if (TargetActorCache.Count == 0)
                return;

            var targetActors = TargetActorCache.ToArray();

            // Send standard body rotation only if not ModSided
            if (plugin.CurrentVisibilityMode != VisibilityMode.ModSided)
            {
                var packedRotation = BitPackUtils.PackQuaternionForNetwork(VRRig.LocalRig.transform.rotation);
                PhotonNetwork.RaiseEvent(
                    BodyTrackingClass.BodyEventCode,
                    packedRotation,
                    new RaiseEventOptions { TargetActors = targetActors },
                    SendOptions.SendUnreliable
                );
            }

            if (plugin.HasLeftElbow || plugin.HasRightElbow || plugin.IsSpineEnabled)
            {
                ref var leftResult = ref plugin.GetLeftElbowResult();
                ref var rightResult = ref plugin.GetRightElbowResult();
                ref var spineResult = ref plugin.GetSpineResult();

                ElbowPackedData[0] = plugin.HasLeftElbow
                    ? BitPackUtils.PackQuaternionForNetwork(leftResult.UpperArmRotation)
                    : 0;
                ElbowPackedData[1] = plugin.HasLeftElbow
                    ? BitPackUtils.PackQuaternionForNetwork(leftResult.ForearmRotation)
                    : 0;
                ElbowPackedData[2] = plugin.HasRightElbow
                    ? BitPackUtils.PackQuaternionForNetwork(rightResult.UpperArmRotation)
                    : 0;
                ElbowPackedData[3] = plugin.HasRightElbow
                    ? BitPackUtils.PackQuaternionForNetwork(rightResult.ForearmRotation)
                    : 0;
                ElbowPackedData[4] = plugin.IsSpineEnabled
                    ? BitPackUtils.PackQuaternionForNetwork(spineResult.UpperSpineRotation)
                    : 0;
                ElbowPackedData[5] = plugin.IsSpineEnabled
                    ? BitPackUtils.PackQuaternionForNetwork(spineResult.LowerSpineRotation)
                    : 0;
                ElbowPackedData[6] = plugin.IsHeadLeanEnabled
                    ? BitPackUtils.PackQuaternionForNetwork(
                        Quaternion.AngleAxis(spineResult.HeadLeanAngle, spineResult.HeadLeanAxis))
                    : 0;
                
                ElbowPackedData[7] = Compression.PackFingers(plugin.GetLeftFingers());
                ElbowPackedData[8] = Compression.PackFingers(plugin.GetRightFingers());

                PhotonNetwork.RaiseEvent(
                    BodyTrackingClass.ElbowEventCode,
                    ElbowPackedData,
                    new RaiseEventOptions { TargetActors = targetActors },
                    SendOptions.SendUnreliable
                );
            }
        }
    }

    [HarmonyPatch(typeof(VRRig), nameof(VRRig.SerializeReadShared))]
    internal class VRRigSerializeReadSharedPatches
    {
        private static bool Prefix(VRRig __instance, InputStruct data)
        {
            if (!__instance.creator.GetPlayerRef().CustomProperties
                    .ContainsKey(BodyTrackingClass.Prop)) return true;

            __instance.head.syncRotation.SetValueSafe(BitPackUtils.UnpackQuaternionFromNetwork(data.headRotation));
            BitPackUtils.UnpackHandPosRotFromNetwork(data.rightHandLong, out __instance.tempVec, out __instance.tempQuat);
            __instance.rightHand.syncPos = __instance.tempVec;
            __instance.rightHand.syncRotation.SetValueSafe(in __instance.tempQuat);
            BitPackUtils.UnpackHandPosRotFromNetwork(data.leftHandLong, out __instance.tempVec, out __instance.tempQuat);
            __instance.leftHand.syncPos = __instance.tempVec;
            __instance.leftHand.syncRotation.SetValueSafe(in __instance.tempQuat);
            __instance.syncPos = BitPackUtils.UnpackWorldPosFromNetwork(data.position);
            __instance.handSync = data.handPosition;
            int packedFields = data.packedFields;
            __instance.remoteUseReplacementVoice = (packedFields & 512) != 0;
            __instance.SpeakingLoudness = (float)(packedFields >> 24 & byte.MaxValue) / byte.MaxValue;
            __instance.UpdateReplacementVoice();
            __instance.UnpackCompetitiveData(data.packedCompetitiveData);
            __instance.taggedById = data.taggedById;
            int num1 = (packedFields & 1024) != 0 ? 1 : 0;
            __instance.grabbedRopeIsPhotonView = (packedFields & 2048) != 0;
            if (num1 != 0)
            {
                __instance.grabbedRopeIndex = data.grabbedRopeIndex;
                __instance.grabbedRopeBoneIndex = data.ropeBoneIndex;
                __instance.grabbedRopeIsLeft = data.ropeGrabIsLeft;
                __instance.grabbedRopeIsBody = data.ropeGrabIsBody;
                __instance.grabbedRopeOffset.SetValueSafe(in data.ropeGrabOffset);
            }
            else
                __instance.grabbedRopeIndex = -1;
            if (num1 == 0 & (packedFields & 32768) != 0)
            {
                __instance.mountedMovingSurfaceId = data.grabbedRopeIndex;
                __instance.mountedMovingSurfaceIsLeft = data.ropeGrabIsLeft;
                __instance.mountedMovingSurfaceIsBody = data.ropeGrabIsBody;
                __instance.mountedMonkeBlockOffset.SetValueSafe(in data.ropeGrabOffset);
                __instance.movingSurfaceIsMonkeBlock = data.movingSurfaceIsMonkeBlock;
            }
            else
                __instance.mountedMovingSurfaceId = -1;
            int num2 = (packedFields & 8192) != 0 ? 1 : 0;
            bool isHeldLeftHanded = (packedFields & 16384) != 0;
            if (num2 != 0)
            {
                BitPackUtils.UnpackHandPosRotFromNetwork(data.hoverboardPosRot, out var localPos, out var q);
                Color boardColor = BitPackUtils.UnpackColorFromNetwork(data.hoverboardColor);
                if (q.IsValid())
                    __instance.hoverboardVisual.SetIsHeld(isHeldLeftHanded, localPos.ClampMagnitudeSafe(1f), q, boardColor);
            }
            else if (__instance.hoverboardVisual.gameObject.activeSelf)
                __instance.hoverboardVisual.SetNotHeld();
            if ((packedFields & 65536) != 0)
            {
                bool isLeftHand = (packedFields & 131072) != 0;
                BitPackUtils.UnpackHandPosRotFromNetwork(data.propHuntPosRot, out var localPos, out var handRot);
                __instance.propHuntHandFollower.SetProp(isLeftHand, localPos, handRot);
            }
            if (__instance.grabbedRopeIsPhotonView)
                __instance.localGrabOverrideBlend = -1f;
            Vector3 position = __instance.transform.position;
            __instance.leftHandLink.Read(__instance.leftHand.syncPos, __instance.syncRotation, position, data.isGroundedHand, data.isGroundedButt, (packedFields & 262144) != 0, (packedFields & 1048576) != 0, data.leftHandGrabbedActorNumber, data.leftGrabbedHandIsLeft);
            __instance.rightHandLink.Read(__instance.rightHand.syncPos, __instance.syncRotation, position, data.isGroundedHand, data.isGroundedButt, (packedFields & 524288) != 0, (packedFields & 2097152) != 0, data.rightHandGrabbedActorNumber, data.rightGrabbedHandIsLeft);
            __instance.LastTouchedGroundAtNetworkTime = data.lastTouchedGroundAtTime;
            __instance.LastHandTouchedGroundAtNetworkTime = data.lastHandTouchedGroundAtTime;
            __instance.UpdateRopeData();
            __instance.UpdateMovingMonkeBlockData();
            __instance.AddVelocityToQueue(__instance.syncPos, data.serverTimeStamp);

            return false;
        }
    }

    [HarmonyPatch(typeof(GorillaIKMgr), nameof(GorillaIK.OnEnable))]
    internal static class PatchGorillaIKMgrOnEnable
    {
        private static bool Prefix(GorillaIK __instance)
        {
            var rig = __instance.GetComponent<VRRig>();
            if (!rig) return true;

            if (rig.isOfflineVRRig) return true;

            var props = rig.OwningNetPlayer.GetPlayerRef().CustomProperties;
            if (!props.ContainsKey(BodyTrackingClass.Prop)) return true;
            return !(bool)props[BodyTrackingClass.Prop];
        }
    }
}
