using BepInEx;
using Bounce.Unmanaged;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LordAshes
{
    public partial class LightPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// Handler for Radial Menu selections
        /// </summary>
        /// <param name="cid"></param>
        /// <param name="rid"></param>
        /// <param name="lightName"></param>
        public void RadialMenuRequest(CreatureGuid cid, NGuid rid, string lightName)
        {
            if (lightName == "None")
            {
                Debug.Log("Light Plugin: Requesting No Light");
                StatMessaging.ClearInfo(new CreatureGuid(rid), LightPlugin.Guid);
            }
            else if (lights.ContainsKey(lightName))
            {
                StatMessaging.SetInfo(new CreatureGuid(rid), LightPlugin.Guid, lightName);
            }
            else
            {
                Debug.Log("Light Plugin: Don't Seem To Have A '" + lightName + "' Light");
            }
        }

        /// <summary>
        /// Handler for Stat Messaging subscribed messages.
        /// </summary>
        /// <param name="changes"></param>
        public void StatMessagingRequest(StatMessaging.Change[] changes)
        {
            Debug.Log("Light Plugin: Changes Received");
            if (subscription != SubscriptionState.subscribed)
            {
                Debug.Log("Light Plugin: Addings Changes To Backlog");
                backlog.Enqueue(changes);
            }
            else
            {
                foreach (StatMessaging.Change change in changes)
                {
                    Debug.Log("Light Plugin: Change '" + change.action + "' for '" + change.cid + "' from '" + change.previous + "' To '" + change.value + "' (" + change.key + ")");
                    bool process = true;
                    if (change.action != StatMessaging.ChangeType.removed)
                    {
                        process = !lights[change.value].behaviour.sight || LocalClient.CanControlCreature(change.cid);
                        Debug.Log("Light Plugin: Processing A '" + change.value + "' Light Request (Sight Light: " + lights[change.value].behaviour.sight + " | Controlled: " + LocalClient.CanControlCreature(change.cid) + " => " + process + ")");
                        if (process) { ProcessRequest(change.cid, change.value); }
                    }
                    else
                    {
                        process = (change.previous != "") ? !lights[change.previous].behaviour.sight || LocalClient.CanControlCreature(change.cid) : true;
                        Debug.Log("Light Plugin: Processing A No Light Request (Sight Light: " + ((change.previous != "") ? lights[change.previous].behaviour.sight.ToString() : "N/A") + " | Controlled: " + LocalClient.CanControlCreature(change.cid) + " => " + process + ")");
                        if (process) { ProcessRequest(change.cid, ""); }
                    }
                }
            }
        }

        /// <summary>
        /// Handler for Radial Menu selections
        /// </summary>
        /// <param name="cid"></param>
        private void ProcessRequest(CreatureGuid cid, string lightName)
        {
            if (lightName != "")
            {
                ProcessLightRequest(cid, lights[lightName]);
            }
            else
            {
                ProcessLightRequest(cid, null);
            }
        }

        private void ProcessLightRequest(CreatureGuid cid, LightSpecs ls)
        {
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(cid, out asset);

            if (ls != null)
            {
                GameObject socket = null;
                foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
                {
                    if (go.name.StartsWith("Effect:Light:") && go.name.EndsWith(cid.ToString()))
                    {
                        socket = go;
                        break;
                    }
                }

                if (socket == null)
                {
                    Debug.Log("Light Plugin: Creating New Light Socket");
                    socket = new GameObject("Effect:Light:" + cid);
                    socket.name = "Effect:Light:" + cid;
                }

                Light light = socket.GetComponent<Light>();
                if (light == null)
                {
                    Debug.Log("Light Plugin: Creating New Light");
                    light = socket.AddComponent<Light>();
                    light.name = "Effect:Light:" + cid;
                }

                Debug.Log("Light Plugin: Applying:\r\n" + JsonConvert.SerializeObject(ls));

                Debug.Log("Light Plugin: Adjusting The Light");
                light.name = "Effect:Light:" + ls.name + ":"+cid;
                light.intensity = ls.behaviour.intensityMax;

                light.type = ls.specs.type;
                light.color = new UnityEngine.Color(ls.specs.color.R/255.0f, ls.specs.color.G/255.0f, ls.specs.color.B/255.0f, ls.specs.color.A/255.0f);
                light.range = ls.specs.range;
                light.spotAngle = ls.specs.spotAngle;
                light.shadows = ls.specs.shadows;

                Debug.Log("Light Plugin: Color = " + Convert.ToString(light.color));

                socket.transform.position = Utility.GetBaseLoader(asset.CreatureId).transform.position;
                // socket.transform.eulerAngles = Utility.GetBaseLoader(asset.CreatureId).transform.eulerAngles;
                socket.transform.SetParent(Utility.GetBaseLoader(asset.CreatureId).transform);
                Debug.Log("Light Plugin: Secured Light To Base (Pos: "+Convert.ToString(socket.transform.position)+", Rot: "+Convert.ToString(socket.transform.eulerAngles)+")");

                Debug.Log("Light Plugin: Light Offset (Pos: " + JsonConvert.SerializeObject(ls.position) + ", Rot: " + JsonConvert.SerializeObject(ls.rotation) + ")");

                socket.transform.localPosition = ls.position.ToVector3FromTalespireToUnity();
                socket.transform.localEulerAngles = Quaternion.Euler(ls.rotation.x-90f, ls.rotation.y+180f, ls.rotation.z+180f).eulerAngles;
                Debug.Log("Light Plugin: Adjusting Light Position (Pos: " + Convert.ToString(socket.transform.localPosition) + ", Rot: " + Convert.ToString(socket.transform.localEulerAngles) + ")");

                if (ls.behaviour.hiddenBase)
                {
                    CreatureManager.SetCreatureExplicitHideState(asset.CreatureId, true);
                    CreatureManager.SetCreatureName(asset.CreatureId, "<color=red>");
                }
                Debug.Log("Light Plugin: Light Ready");
            }
            else
            {
                Debug.Log("Light Plugin: Extinguishing Light");

                GameObject socket = null;
                foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
                {
                    if (go.name.StartsWith("Effect:Light:") && go.name.EndsWith(cid.ToString()))
                    {
                        socket = go;
                        break;
                    }
                }

                if (socket != null) { GameObject.Destroy(socket); }

                Debug.Log("Light Plugin: Light Removed");
            }
        }
    }
}
