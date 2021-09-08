using BepInEx;
using Bounce.Unmanaged;
using UnityEngine;

namespace LordAshes
{
    public partial class LightPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// Handler for Radial Menu selections
        /// </summary>
        /// <param name="cid"></param>
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
                    process = !lights[change.value].sight || LocalClient.CanControlCreature(change.cid);
                    Debug.Log("Light Plugin: Processing A '" + change.value + "' Light Request (Sight Light: "+ lights[change.value].sight+" | Controlled: "+ LocalClient.CanControlCreature(change.cid)+" => "+process+")");
                    if (process) { ProcessRequest(change.cid, change.value); }
                }
                else
                {
                    process = (change.previous!="") ? !lights[change.previous].sight || LocalClient.CanControlCreature(change.cid) : true;
                    Debug.Log("Light Plugin: Processing A No Light Request (Sight Light: " + ((change.previous != "") ? lights[change.previous].sight.ToString() : "N/A") + " | Controlled: " + LocalClient.CanControlCreature(change.cid)+" => "+process+")");
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
                string[] c = ls.color.Split(',');
                string[] p = ls.pos.Split(',');
                string[] r = ls.rot.Split(',');
                light.name = "Effect:Light:" + ls.name + ":"+cid;
                light.type = ls.lightType;
                light.color = new UnityEngine.Color(float.Parse(c[0]), float.Parse(c[1]), float.Parse(c[2]));
                light.intensity = ls.intensity;
                light.range = ls.range;
                light.spotAngle = ls.spotAngle;
                light.shadows = LightShadows.Soft;

                Debug.Log("Light Plugin: Securing Light To Base");
                socket.transform.position = asset.BaseLoader.transform.position;
                socket.transform.eulerAngles = asset.BaseLoader.transform.eulerAngles;
                socket.transform.SetParent(asset.BaseLoader.transform);

                Debug.Log("Light Plugin: Adjusting Light Position");
                socket.transform.localPosition = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
                socket.transform.localEulerAngles = new Vector3(float.Parse(r[0]), float.Parse(r[1]), float.Parse(r[2]));

                if(ls.hiddenBase)
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
    }
}
