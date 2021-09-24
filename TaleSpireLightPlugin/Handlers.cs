using BepInEx;
using Bounce.Unmanaged;
using Newtonsoft.Json;
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
            foreach (StatMessaging.Change change in changes)
            {
                Debug.Log("Light Plugin: Change '"+change.action+"' for '"+change.cid+"' from '"+change.previous+"' To '"+change.value+"' ("+change.key+")");
                bool process = true;
                if (change.action != StatMessaging.ChangeType.removed)
                {
                    process = !lights[change.value].behaviour.sight || LocalClient.CanControlCreature(change.cid);
                    Debug.Log("Light Plugin: Processing A '" + change.value + "' Light Request (Sight Light: "+ lights[change.value].behaviour.sight+" | Controlled: "+ LocalClient.CanControlCreature(change.cid)+" => "+process+")");
                    if (process) { ProcessRequest(change.cid, change.value); }
                }
                else
                {
                    process = (change.previous!="") ? !lights[change.previous].behaviour.sight || LocalClient.CanControlCreature(change.cid) : true;
                    Debug.Log("Light Plugin: Processing A No Light Request (Sight Light: " + ((change.previous != "") ? lights[change.previous].behaviour.sight.ToString() : "N/A") + " | Controlled: " + LocalClient.CanControlCreature(change.cid)+" => "+process+")");
                    if (process) { ProcessRequest(change.cid, ""); }
                }
            }
        }

        /// <summary>
        /// Handler for Radial Menu selections
        /// </summary>
        /// <param name="cid"></param>
        private void ProcessRequest(CreatureGuid cid, string lightName)
        {
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(cid, out asset);

            if (lightName != "")
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

                Debug.Log("Light Plugin: Adjusting The Light");
                LightSpecs ls = lights[lightName];
                light.name = "Effect:Light:" + ls.name + ":"+cid;
                light.intensity = ls.behaviour.intensityMax;
                ReflectionObjectManipulator.Transfer(light, ls.specs.ToArray());

                Debug.Log("Light Plugin: Securing Light To Base");
                socket.transform.position = asset.BaseLoader.transform.position;
                socket.transform.eulerAngles = asset.BaseLoader.transform.eulerAngles;
                socket.transform.SetParent(asset.BaseLoader.transform);

                Debug.Log("Light Plugin: Adjusting Light Position");
                socket.transform.localPosition = lights[lightName].position;
                socket.transform.localEulerAngles = lights[lightName].rotation;

                if(ls.behaviour.hiddenBase)
                {
                    CreatureManager.SetCreatureExplicitHideState(asset.Creature.CreatureId, true);
                    CreatureManager.SetCreatureName(asset.Creature.CreatureId, "<color=red>");
                }
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
            }
        }

        public string ConvertLegacyConfiguration()
        {            
            string config = FileAccessPlugin.File.Find("LightTypes.json")[0];
            Debug.Log("Light Plugin: Converting Legacy Configuration In " + config);
            string configNew = config.Substring(0, config.Length - 5) + ".kvp";
            Debug.Log("Light Plugin: Writing Updated Configuration In " + configNew);
            List<LegacyLightSpecs> llss = JsonConvert.DeserializeObject<List<LegacyLightSpecs>>(System.IO.File.ReadAllText(config));
            System.IO.File.WriteAllText(configNew, "");
            foreach (LegacyLightSpecs lls in llss)
            {
                System.IO.File.AppendAllText(configNew, "[" + lls.name + " : LightSpecs];\r\n");
                // System.IO.File.AppendAllText(configNew, ".name=" + lls.name + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".menu.iconName=" + lls.iconName + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".menu.menuNode=" + lls.menuNode + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".menu.menuLink=" + lls.menuLink + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".menu.onlyGM=" + lls.onlyGM + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".behaviour.sight=" + lls.sight + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".behaviour.hiddenBase=" + lls.hiddenBase + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".behaviour.flicker=" + lls.flicker + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".behaviour.intensityMin=" + lls.intensityMin + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".behaviour.intensityMax=" + lls.intensity + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".behaviour.deltaMax=" + lls.deltaMax + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".position=" + lls.pos + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".rotation=" + lls.rot + ";\r\n");
                System.IO.File.AppendAllText(configNew, ".specs=[\"type="+(int)lls.lightType+"\", \"color=" + lls.color + "\", \"range=" + lls.range + "\", \"spotAngle=" + lls.spotAngle + "\", \"shadows=2\"];\r\n");
                System.IO.File.AppendAllText(configNew, ";\r\n");
            }
            return configNew;
        }
    }
}
