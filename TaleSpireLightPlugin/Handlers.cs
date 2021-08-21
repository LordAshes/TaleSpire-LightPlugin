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
            Debug.Log("Light Plugin: Requesting A "+lightName+" Light");
            StatMessaging.SetInfo(new CreatureGuid(rid), LightPlugin.Guid, lightName);
        }

        /// <summary>
        /// Handler for Stat Messaging subscribed messages.
        /// </summary>
        /// <param name="changes"></param>
        public void StatMessagingRequest(StatMessaging.Change[] changes)
        {
            foreach (StatMessaging.Change change in changes)
            {
                Debug.Log("Light Plugin: Processing A " + change.value + " Light Request");
                if (change.action != StatMessaging.ChangeType.removed)
                {
                    ProcessRequest(change.cid, change.value);
                }
                else
                {
                    ProcessRequest(change.cid, "");
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

                GameObject socket = GameObject.Find("Effect:Light:" + cid);
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

            }
            else
            {
                Debug.Log("Light Plugin: Extinguishing Light");

                Debug.Log("Socket Find = " + (GameObject.Find("Effect:Light:" + cid) != null));

                GameObject.Destroy(GameObject.Find("Effect:Light:" + cid));
            }
        }
    }
}
