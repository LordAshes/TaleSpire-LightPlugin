using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Concurrent;

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.FileAccessPlugin.Guid)]
    [BepInDependency(LordAshes.StatMessaging.Guid)]
    [BepInDependency(RadialUI.RadialUIPlugin.Guid)]
    [BepInDependency(LordAshes.GUIMenuPlugin.Guid)]
    public partial class LightPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Light Plug-In";                     
        public const string Guid = "org.lordashes.plugins.light";       
        public const string Version = "2.5.0.0";                        

        // Configuration
        private ConfigEntry<KeyboardShortcut> triggerManualApply { get; set; }
        private ConfigEntry<KeyboardShortcut> triggerRereadConfig { get; set; }
        private ConfigEntry<float> subscriptionDelay { get; set; }
        private GUIMenuPlugin.MenuStyle menuStyle { get; set; }
        private UnityEngine.Color menuLinkColor { get; set; }
        private UnityEngine.Color menuSelectionColor { get; set; }

        // Variables

        public static string configLocation = "";

        public static Dictionary<string,LightSpecs> lights = new Dictionary<string,LightSpecs>();
        private GUIMenuPlugin.GuiMenu menu = new GUIMenuPlugin.GuiMenu();

        private static float intensityMultiplier = 300f;

        private int flickerSequencer = 0;
        private int flickerSteps = 20;

        public enum SubscriptionState
        {
            unsubscribed = 0,
            waiting,
            subscribed
        }

        private SubscriptionState subscription = SubscriptionState.unsubscribed;
        
        private static LightPlugin self = null;

        private static ConcurrentQueue<StatMessaging.Change[]> backlog = new ConcurrentQueue<StatMessaging.Change[]>();

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            self = this;

            UnityEngine.Debug.Log("Light Plugin: "+this.GetType().AssemblyQualifiedName+" Is Active.");

            // Obtain Flicker Configuration
            flickerSteps = Config.Bind("Settings", "Updates per flicker update", 20).Value;
            menuStyle = Config.Bind("Settings", "GUI menu style", GUIMenuPlugin.MenuStyle.centre).Value;
            menuLinkColor = Config.Bind("Settings", "GUI menu link color", UnityEngine.Color.black).Value;
            menuSelectionColor = Config.Bind("Settings", "GUI menu selection color", UnityEngine.Color.gray).Value;
            subscriptionDelay = Config.Bind("Settings", "Startup light application delay", 10.0f);

            triggerManualApply = Config.Bind("Hotkeys", "Manual re-apply of lights", new KeyboardShortcut(KeyCode.R, KeyCode.RightControl));
            triggerRereadConfig = Config.Bind("Hotkeys", "Reread lights configuration", new KeyboardShortcut(KeyCode.L, KeyCode.RightControl)); 

            // Read Configuration
            ReadConfiguration(ref lights);

            // Determine if sub-menus are used
            UnityEngine.Debug.Log("Light Plugin: Detecting Usage Of Sub-Menus");
            bool submenus = false;
            foreach (LightSpecs light in lights.Values)
            {
                if (light.menu.menuNode != "") { submenus = true; break; }
            }
            if (submenus)
            {
                UnityEngine.Debug.Log("Light Plugin: Using GUI Menus With Hierarchy.");
                // Add extinguish light option
                UnityEngine.Debug.Log("Light Plugin: Adding Extinguish Light Option");
                LightSpecs extinguish = new LightSpecs() { name = "None", menu = new LightMenu() { iconName = "None.png", menuNode = "Root" } };
                if (lights.ContainsKey(extinguish.name)) { lights.Remove(extinguish.name); }
                lights.Add(extinguish.name, extinguish);
                UnityEngine.Debug.Log("Light Plugin: Adding Light Options");
                // Create root character light menu
                RadialUI.RadialUIPlugin.AddCustomButtonOnCharacter(LightPlugin.Guid, new MapMenu.ItemArgs()
                {
                    Title = "Light",
                    Icon = FileAccessPlugin.Image.LoadSprite("Light.png"),
                    Action = (s, a) => { Debug.Log("Light Plugin: Opening GUI Menu"); menu.Open("Root", LightSelectionHandler); },
                    CloseMenuOnActivate = true
                }, (guid1,guid2)=> { return true; });
                // Create GUI menu for light sub-selections
                foreach (LightSpecs light in lights.Values)
                {
                    if (menu.GetNode(light.menu.menuNode) == null)
                    {
                        Debug.Log("Light Plugin: Making Node '" + light.menu.menuNode + "' For '"+light.name+"'");
                        GUIMenuPlugin.MenuNode node = new GUIMenuPlugin.MenuNode(light.menu.menuNode, new GUIMenuPlugin.IMenuItem[0], menuStyle);
                        menu.AddNode(node);
                    }
                    if (light.menu.menuLink != "")
                    {
                        // Add Menu Link
                        Debug.Log("Light Plugin: Adding Link '" + light.menu.menuLink + "' To Node '" + light.menu.menuNode + "' For '"+ light.name+"'");
                        menu.GetNode(light.menu.menuNode).AddLink(new GUIMenuPlugin.MenuLink(light.menu.menuLink, light.name, menuLinkColor, FileAccessPlugin.Image.LoadTexture(light.menu.iconName), light.menu.onlyGM));
                    }
                    else
                    {
                        // Add Menu Selection
                        Debug.Log("Light Plugin: Adding Selection '" + light.name + "' To Node '" + light.menu.menuNode + "'");
                        menu.GetNode(light.menu.menuNode).AddSelection(new GUIMenuPlugin.MenuSelection(light.name, light.name, menuSelectionColor, FileAccessPlugin.Image.LoadTexture(light.menu.iconName), light.menu.onlyGM));
                        Debug.Log("Light Plugin: Adding To Light Dictionary");
                        // lights.Add(light.name, light);
                    }
                }
            }
            else
            {
                UnityEngine.Debug.Log("Light Plugin: Using Flat Radial Menu.");
                // Add extinguish light option
                UnityEngine.Debug.Log("Light Plugin: Adding Extinguish Light Option");
                LightSpecs extinguish = new LightSpecs() { name = "None", menu = new LightMenu() { iconName = "None.png" } };
                if (lights.ContainsKey(extinguish.name)) { lights.Remove(extinguish.name); }
                lights.Add(extinguish.name, extinguish);
                UnityEngine.Debug.Log("Light Plugin: Adding Light Options");
                // Create root character light menu
                RadialUI.RadialSubmenu.EnsureMainMenuItem(LightPlugin.Guid, RadialUI.RadialSubmenu.MenuType.character, "Light", FileAccessPlugin.Image.LoadSprite("Light.png"));
                // Create sub-menu for light sub-selections
                foreach (LightSpecs light in lights.Values)
                {
                    RadialUI.RadialSubmenu.CreateSubMenuItem(LightPlugin.Guid, light.name, FileAccessPlugin.Image.LoadSprite(light.menu.iconName),
                                                             (cid, s, mmi) => { RadialMenuRequest(cid, RadialUI.RadialUIPlugin.GetLastRadialTargetCreature(), light.name); },
                                                             true,
                                                             () =>
                                                             {
                                                                 bool isGM = LocalClient.IsInGmMode;
                                                                 bool isOwner = LocalClient.CanControlCreature(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()));
                                                                 if (isOwner && (isGM || light.menu.onlyGM == false))
                                                                 {
                                                                     return true;
                                                                 }
                                                                 else
                                                                 {
                                                                     return false;
                                                                 }
                                                             });
                }
            }

            // Subscribe to Light Messages
            StatMessaging.Subscribe(LightPlugin.Guid, StatMessagingRequest);

            // Post on main page
            Utility.PostOnMainPage(this.GetType());
        }

        private IEnumerator DelayLightSubscription(float delay)
        {
            UnityEngine.Debug.Log("Light Plugin: Delaying Light Message Processing");
            subscription = SubscriptionState.waiting;
            yield return new WaitForSeconds(delay);
            if (subscription == SubscriptionState.waiting)
            {
                UnityEngine.Debug.Log("Light Plugin: Start Light Message Processing");
                subscription = SubscriptionState.subscribed;
            }
        }

        private void LightSelectionHandler(string lightName)
        {
            RadialMenuRequest(LocalClient.SelectedCreatureId, RadialUI.RadialUIPlugin.GetLastRadialTargetCreature(), lightName);
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            if (Utility.isBoardLoaded())
            {
                if (subscription == SubscriptionState.unsubscribed)
                {
                    // Delay subscribe to light events
                    subscription = SubscriptionState.waiting;
                    UnityEngine.Debug.Log("Light Plugin: Board Loaded");
                    self.StartCoroutine(DelayLightSubscription(subscriptionDelay.Value));
                }

                if(backlog.Count>0 && subscription == SubscriptionState.subscribed)
                {
                    UnityEngine.Debug.Log("Light Plugin: Backlog="+backlog.Count+". Processing...");
                    StatMessaging.Change[] changes = null;
                    if(backlog.TryDequeue(out changes))
                    {
                        StatMessagingRequest(changes);
                    }
                }

                flickerSequencer++;
                if (flickerSequencer >= flickerSteps) { flickerSequencer = 0; }

                if (flickerSequencer == 0)
                {
                    foreach (CreatureBoardAsset asset in CreaturePresenter.AllCreatureAssets)
                    {
                        // Look for a light
                        Light light = asset.GetComponentInChildren<Light>();
                        if (light != null)
                        {
                            // Check to see if the light is a custom light
                            if (light.name.StartsWith("Effect:Light:"))
                            {
                                string lightName = light.name.Substring("Effect:Light:".Length);
                                lightName = lightName.Substring(0, lightName.IndexOf(":"));
                                // Look up light type
                                if (lights.ContainsKey(lightName))
                                {
                                    // Check if custom light has flicker active
                                    if (lights[lightName].behaviour.flicker)
                                    {
                                        // Process custom light flicker
                                        System.Random rnd = new System.Random();
                                        float randomizer = rnd.Next(0, 100);
                                        float intensity = (randomizer / 100f) * (lights[lightName].behaviour.intensityMax - lights[lightName].behaviour.intensityMin) + lights[lightName].behaviour.intensityMin;
                                        light.intensity = intensity;
                                        float dx = lights[lightName].behaviour.deltaMax * (float)(rnd.Next(0, 100)) / 100f;
                                        float dy = lights[lightName].behaviour.deltaMax * (float)(rnd.Next(0, 100)) / 100f;
                                        light.transform.localPosition = new Vector3(lights[lightName].position.x + dx, lights[lightName].position.y, lights[lightName].position.z + dy);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if(!Utility.isBoardLoaded() && subscription!=SubscriptionState.unsubscribed)
            {
                // Reset subscription state so that re-subscription occurs on board re-load
                UnityEngine.Debug.Log("Light Plugin: Board Unloaded");
                subscription = SubscriptionState.unsubscribed;
            }

            if(Utility.StrictKeyCheck(triggerManualApply.Value))
            {
                UnityEngine.Debug.Log("Light Plugin: Manual Re-Apply");
                StatMessaging.Reset(LightPlugin.Guid);
            }

            if (Utility.StrictKeyCheck(triggerRereadConfig.Value))
            {
                UnityEngine.Debug.Log("Light Plugin: Reread Configuration");
                ReadConfiguration(ref lights);
            }
        }

        void OnGUI()
        {
            menu.Draw();
        }

        public static void ReadConfiguration(ref Dictionary<string, LightSpecs> lights)
        {
            lights.Clear();
            UnityEngine.Debug.Log("Light Plugin: Deserialize JSON Light File.");
            if (FileAccessPlugin.File.Exists("LightSpecs.json"))
            {
                UnityEngine.Debug.Log("Light Plugin: Found Light Specs Configuration");
                configLocation = FileAccessPlugin.File.Find("LightSpecs.json")[0];
                string json = FileAccessPlugin.File.ReadAllText(configLocation);
                List<LightSpecs> lsl = JsonConvert.DeserializeObject<List<LightSpecs>>(json);
                foreach (LightSpecs ls in lsl)
                {
                    lights.Add(ls.name, ls);
                    Debug.Log("Light Plugin:\r\n" + JsonConvert.SerializeObject(ls));
                }
            }
            else if (FileAccessPlugin.File.Exists("LightTypes.json"))
            {
                UnityEngine.Debug.Log("Light Plugin: Found Light Type Configuration");
                configLocation = FileAccessPlugin.File.Find("LightTypes.json")[0];
                string json = FileAccessPlugin.File.ReadAllText(configLocation);
                List<LegacyLightSpecs> llsl = JsonConvert.DeserializeObject<List<LegacyLightSpecs>>(json);
                foreach (LegacyLightSpecs lls in llsl)
                {
                    string[] posParts = lls.pos.Split(',');
                    string[] rotParts = lls.rot.Split(',');
                    LightSpecs ls = new LightSpecs()
                    {
                        behaviour = new LightBehaviour()
                        {
                            deltaMax = lls.deltaMax,
                            flicker = lls.flicker,
                            hiddenBase = lls.hiddenBase,
                            intensityMax = lls.intensity * intensityMultiplier,
                            intensityMin = lls.intensityMin * intensityMultiplier,
                            sight = lls.sight
                        },
                        menu = new LightMenu()
                        {
                            iconName = lls.iconName,
                            menuLink = lls.menuLink,
                            menuNode = lls.menuNode,
                            onlyGM = lls.onlyGM
                        },
                        name = lls.name,
                        position = new F3(Utility.ParseFloat(posParts[0]), Utility.ParseFloat(posParts[1]), Utility.ParseFloat(posParts[2])),
                        rotation = new F3(Utility.ParseFloat(rotParts[0]), Utility.ParseFloat(rotParts[1]), Utility.ParseFloat(rotParts[2])),
                        specs = new LightProperties()
                        {
                            type = lls.lightType,
                            color = lls.color,
                            range = lls.range,
                            spotAngle = lls.spotAngle,
                            shadows = lls.shadowType
                        }
                    };
                    lights.Add(ls.name, ls);
                }
                FileAccessPlugin.File.WriteAllText(FileAccessPlugin.File.Find("LightTypes.json")[0].Replace("LightTypes.json", "LightSpecs.json"), JsonConvert.SerializeObject(lights.Values, Formatting.Indented));
            }
            else
            {
                UnityEngine.Debug.Log("Light Plugin: Missing Light Configuration");
                Environment.Exit(0);
            }
        }

        public static void UpdateLight(CreatureGuid holder, LightSpecs ls)
        {
            if (!lights.ContainsKey(ls.name)) 
            { 
                lights.Add(ls.name, ls);
                RadialUI.RadialSubmenu.CreateSubMenuItem(LightPlugin.Guid, ls.name, FileAccessPlugin.Image.LoadSprite(ls.menu.iconName),
                                         (cid, s, mmi) => { self.RadialMenuRequest(cid, RadialUI.RadialUIPlugin.GetLastRadialTargetCreature(), ls.name); },
                                         true,
                                         () =>
                                         {
                                             bool isGM = LocalClient.IsInGmMode;
                                             bool isOwner = LocalClient.CanControlCreature(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()));
                                             if (isOwner && (isGM || ls.menu.onlyGM == false))
                                             {
                                                 return true;
                                             }
                                             else
                                             {
                                                 return false;
                                             }
                                         });
            }
            else 
            { 
                lights[ls.name] = ls;
            }
            self.ProcessLightRequest(holder, ls);
        }

        public class F3
        {
            public float x { get; set; } = 0f;
            public float y { get; set; } = 0f;
            public float z { get; set; } = 0f;

            public F3() { ; }
            public F3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

            public Vector3 ToVector3()
            {
                return new Vector3(x, y, z);
            }
            public Vector3 ToVector3FromTalespireToUnity()
            {
                return new Vector3(x, z, y);
            }
        }

        public class LightBehaviour
        {
            public bool flicker { get; set; } = false;
            public float intensityMin { get; set; } = 0f;
            public float intensityMax { get; set; } = 0f;
            public float deltaMax { get; set; } = 0f;
            public bool sight { get; set; } = false;
            public bool hiddenBase { get; set; } = false;
        }

        public class LightMenu
        {
            public string iconName { get; set; } = "Light.png";
            public bool onlyGM { get; set; } = false;
            public string menuNode { get; set; } = "";
            public string menuLink { get; set; } = "";
        }

        public class LightProperties
        {
            public LightType type { get; set; } = LightType.Spot;
            public System.Drawing.Color color { get; set; } = System.Drawing.Color.FromArgb(255,255,255,255);
            public float range { get; set; } = 3.5f;
            public float spotAngle { get; set; } = 25;
            public LightShadows shadows { get; set; } = LightShadows.Soft;
        }

        public class LightSpecs
        {
            public string name { get; set; } = "Light";
            public LightProperties specs { get; set; } = new LightProperties();
            public F3 position { get; set; } = null;
            public F3 rotation { get; set; } = null;
            public LightBehaviour behaviour { get; set; } = new LightBehaviour();
            public LightMenu menu { get; set; } = new LightMenu();
        }

        public class LegacyLightSpecs
        {
            public string name { get; set; } = "Light";
            public LightType lightType { get; set; } = LightType.Point;
            public LightShadows shadowType { get; set; } = LightShadows.Soft;
            public string iconName { get; set; } = "Light.png";
            public float intensity { get; set; } = 0.01f;
            public System.Drawing.Color color { get; set; } = System.Drawing.Color.FromArgb(255,255,128);
            public float range { get; set; } = 2.0f;
            public string pos { get; set; } = "0,0.75,0";
            public string rot { get; set; } = "90,0,0";
            public float spotAngle { get; set; } = 15f;
            public bool flicker { get; set; } = false;
            public float intensityMin { get; set; } = 0f;
            public float deltaMax { get; set; } = 0f;
            public bool sight { get; set; } = false;
            public bool hiddenBase { get; set; } = false;
            public bool onlyGM { get; set; } = false;
            public string menuNode { get; set; } = "";
            public string menuLink { get; set; } = "";
        }
    }
}

