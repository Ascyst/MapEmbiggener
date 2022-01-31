using HarmonyLib;
using UnityEngine;
using UnboundLib;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using MapEmbiggener.Extensions;

namespace MapEmbiggener.Patches
{
    [HarmonyPatch(typeof(Map),"Start")]
    class MapPatchStart
    {
        private static void Postfix(Map __instance)
        {
            MapEmbiggener.defaultMapSize = __instance.size;

            foreach (SpawnPoint spawnPoint in __instance.GetComponentsInChildren<SpawnPoint>())
            {
                spawnPoint.localStartPos *= MapEmbiggener.setSize;
            }
            __instance.transform.localScale *= MapEmbiggener.setSize;
            __instance.size *= MapEmbiggener.setSize;
            MapEmbiggener.zoomShrink = 1;
            OutOfBoundsUtils.SetOOB(OutOfBoundsUtils.defaultX * MapEmbiggener.settingsSetSize, OutOfBoundsUtils.defaultY * MapEmbiggener.settingsSetSize);
        }
    }
    [HarmonyPatch(typeof(Map), "StartMatch")]
    class MapPatchStartMatch
    {
        private static void Postfix(Map __instance)
        {
            Unbound.Instance.ExecuteAfterFrames(2, () =>
            {
                // if a stickfightmaps object has any particlesystem components, then scale those up
                foreach (ParticleSystem part in __instance.gameObject.GetComponentsInChildren<ParticleSystem>())
                {
                    if (part.gameObject.IsStickFightObject())
                    {
                        part.transform.localScale *= MapEmbiggener.setSize;
                    }
                }
                foreach (Rigidbody2D rig in __instance.allRigs)
                {

                    // if its a MapsExtended object, mess with the position only on the host's side
                    if (rig.gameObject.IsMapsExtObject())
                    {
                        if (rig.gameObject.GetComponent<PhotonView>().IsMine)
                        {
                            rig.gameObject.transform.position = new Vector3(MapEmbiggener.setSize * rig.gameObject.transform.position.x, rig.gameObject.transform.position.y * MapEmbiggener.setSize, rig.gameObject.transform.position.z);
                        }
                    }

                    // rescale physics objects UNLESS they have a movesequence component
                    // also UNLESS they are a crate from stickfight (Boss Sloth's stickfight maps mod)
                    // if they have a movesequence component then scale the points in that component

                    if (rig.gameObject.IsStickFightObject())
                    {

                        if (rig.gameObject.name.Contains("PLatform"))
                        {
                            rig.transform.localScale /= MapEmbiggener.setSize;
                        }

                        rig.mass *= MapEmbiggener.setSize;

                        // increase the strength of chains
                        if (rig.gameObject.name.Contains("Chain"))
                        {
                            if (rig.gameObject.GetComponent<ForceMultiplier>() != null) { rig.gameObject.GetComponent<ForceMultiplier>().multiplier *= MapEmbiggener.setSize; }
                            foreach (DistanceJoint2D joint in rig.gameObject.GetComponents<DistanceJoint2D>())
                            {
                                joint.distance *= MapEmbiggener.setSize;
                            }
                        }
                        continue;
                    }

                    if (rig.gameObject.GetComponentInChildren<MoveSequence>(true) == null)
                    {
                        rig.mass *= MapEmbiggener.setSize;

                        // if its a maps extended object, then only change its size on the host client
                        if (rig.gameObject.IsMapsExtObject() && !rig.gameObject.GetComponent<PhotonView>().IsMine)
                        {
                            continue;
                        }

                        rig.transform.localScale *= MapEmbiggener.setSize;
                    }
                    else
                    {
                        List<Vector2> newPos = new List<Vector2>() { };
                        MoveSequence move = rig.gameObject.GetComponentInChildren<MoveSequence>(true);
                        foreach (Vector2 pos in move.positions)
                        {
                            newPos.Add(pos * MapEmbiggener.setSize);
                        }
                        move.positions = newPos.ToArray();
                        move.SetFieldValue("startPos", MapEmbiggener.setSize * (Vector2)move.GetFieldValue("startPos"));
                    }
                }

                GameObject Rendering = GameObject.Find("/Game/Visual/Rendering ");

                if (Rendering != null)
                {
                    foreach (Transform transform in Rendering.GetComponentsInChildren<Transform>(true))
                    {
                        transform.localScale = Vector3.one * Mathf.Clamp(MapEmbiggener.setSize, 0.1f, 2f);
                    }
                }

                Unbound.Instance.StartCoroutine(GameModes(__instance));
            });
        }
        private static float timerStart;
        private static float rotTimerStart;
        private static IEnumerator GameModes(Map instance)
        {
            if (instance == null) { yield break; }
            MapPatchStartMatch.timerStart = Time.time;
            MapPatchStartMatch.rotTimerStart = Time.time;
            while (instance != null && instance.enabled)
            {
                if (instance != null && (float)instance.GetFieldValue("counter") > 2f && (MapEmbiggener.chaosMode || (MapEmbiggener.suddenDeathMode && CountPlayersAlive() <= 2)))
                {
                    if (instance != null && Time.time > MapPatchStartMatch.timerStart + MapEmbiggener.shrinkDelay)
                    {
                        MapPatchStartMatch.timerStart = Time.time;
                        MapEmbiggener.zoomShrink *= MapEmbiggener.shrinkRate;
                        instance.size = MapEmbiggener.defaultMapSize * MapEmbiggener.settingsSetSize *
                                         MapEmbiggener.zoomShrink;
                        OutOfBoundsUtils.SetOOB(
                            OutOfBoundsUtils.defaultX * MapEmbiggener.settingsSetSize * MapEmbiggener.zoomShrink + 0.15f,
                            OutOfBoundsUtils.defaultY * MapEmbiggener.settingsSetSize * MapEmbiggener.zoomShrink + 0.15f);

                    }
                }
                if (instance != null && (float)instance.GetFieldValue("counter") > 2f && MapEmbiggener.chaosMode)
                {
                    if (instance != null && Time.time > MapPatchStartMatch.rotTimerStart + MapEmbiggener.rotationDelay)
                    {
                        MapPatchStartMatch.rotTimerStart = Time.time;
                        Vector3 currentRot = Interface.GetCameraRot().eulerAngles;
                        var angle = currentRot.z + MapEmbiggener.rotationDirection * MapEmbiggener.rotationRate;
                        Interface.MoveCamera(angle: angle);
                        OutOfBoundsUtils.SetAngle(angle);
                    }
                }
                yield return null;
            }
            yield break;
        }
        private static int CountPlayersAlive()
        {
            return PlayerManager.instance.players.Count(p => !p.data.dead);
        }
    }
}
