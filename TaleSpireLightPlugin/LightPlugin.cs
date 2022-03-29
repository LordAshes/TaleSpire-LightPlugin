using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;

using System.Collections.Generic;
using System;
using System.Reflection;

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
        public const string Version = "1.7.2.0";                        

        // Configuration
        private ConfigEntry<KeyboardShortcut> triggerKey { get; set; }
        private GUIMenuPlugin.MenuStyle menuStyle { get; set; }
        private UnityEngine.Color menuLinkColor { get; set; }
        private UnityEngine.Color menuSelectionColor { get; set; }

        public static Dictionary<string,LightSpecs> lights = new Dictionary<string,LightSpecs>();

        private int flickerSequencer = 0;
        private int flickerSteps = 20;

        private GUIMenuPlugin.GuiMenu menu = new GUIMenuPlugin.GuiMenu();

        private static LightPlugin self = null;

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
            ReflectionObjectManipulator.showDiagnostics = Config.Bind("Settings", "Include diagnostic information in log", false).Value;

            // Create light sub menu
            UnityEngine.Debug.Log("Light Plugin: Deserialize JSON Light File.");
            string configLocation = "";
            if (FileAccessPlugin.File.Exists("LightTypes.json") || !FileAccessPlugin.File.Exists("LightTypes.kvp"))
            {
                configLocation = ConvertLegacyConfiguration();
                UnityEngine.Debug.Log("Light Plugin: Found Legacy Configuration At '"+configLocation+"'");
            }
            else if (FileAccessPlugin.File.Exists("LightTypes.kvp"))
            {
                configLocation = FileAccessPlugin.File.Find("LightTypes.kvp")[0];
                UnityEngine.Debug.Log("Light Plugin: Found Light Configuration At '"+configLocation+"'");
            }
            else
            {
                UnityEngine.Debug.Log("Light Plugin: Missing Light Configuration");
                Environment.Exit(0);
            }

            Dictionary<string, object> objs = new Dictionary<string, object>();
            UnityEngine.Debug.Log("Light Plugin: Building Lights");
            ReflectionObjectManipulator.BuildObjectsFromFile(ref objs, configLocation);
            // Add configured lights
            UnityEngine.Debug.Log("Light Plugin: Creating Light Dictionary");
            foreach (KeyValuePair<string, object> obj in objs)
            {
                // ((LightSpecs)obj.Value).name = obj.Key;
                lights.Add(obj.Key, (LightSpecs)obj.Value);
            }

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
                lights.Add(extinguish.name, extinguish);
                // Create root character light menu
                RadialUI.RadialUIPlugin.AddCustomButtonOnCharacter(LightPlugin.Guid, new MapMenu.ItemArgs()
                {
                    Title = "Light",
                    Icon = FileAccessPlugin.Image.LoadSprite("Light.png"),
                    Action = (s, a) => { Debug.Log("Opening GUI Menu"); menu.Open("Root", LightSelectionHandler); },
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
                lights.Add(extinguish.name, extinguish);
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

            // Subscribe to light events
            StatMessaging.Subscribe(LightPlugin.Guid, StatMessagingRequest);

            // Post on main page
            Utility.PostOnMainPage(this.GetType());
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

        void OnGUI()
        {
            menu.Draw();
        }

        public static void UpdateLight(CreatureGuid cid, LightSpecs ls)
        {
            if (!lights.ContainsKey(ls.name)) { lights.Add(ls.name, ls); } else { lights[ls.name] = ls; }
            self.ProcessLightRequest(cid, ls);
        }

        public static void SaveConfig(string saveFileName)
        {
            FileAccessPlugin.File.WriteAllText(saveFileName, "");
            foreach (LightSpecs ls in lights.Values)
            {
                FileAccessPlugin.File.AppendAllText(saveFileName, "["+ls.name+" : LightSpecs];\r\n");
                foreach(PropertyInfo prop in ls.menu.GetType().GetProperties())
                {
                    FileAccessPlugin.File.AppendAllText(saveFileName, ".menu." + prop.Name + "=" + prop.GetValue(ls.menu) + ";\r\n");
                }
                foreach (PropertyInfo prop in ls.behaviour.GetType().GetProperties())
                {
                    FileAccessPlugin.File.AppendAllText(saveFileName, ".behaviour." + prop.Name + "=" + prop.GetValue(ls.behaviour) + ";\r\n");
                }
                FileAccessPlugin.File.AppendAllText(saveFileName, ".position=" + ls.position.ToString().Replace("(","").Replace(")","").Replace(" ","") + ";\r\n");
                FileAccessPlugin.File.AppendAllText(saveFileName, ".rotation=" + ls.rotation.ToString().Replace("(", "").Replace(")", "").Replace(" ", "") + ";\r\n");
                FileAccessPlugin.File.AppendAllText(saveFileName, ".specs=[");
                for(int s=0; s<ls.specs.Count; s++)
                {
                    if (s != (ls.specs.Count - 1))
                    {
                        FileAccessPlugin.File.AppendAllText(saveFileName, "\"" + ls.specs[s] + "\", ");
                    }
                    else
                    {
                        FileAccessPlugin.File.AppendAllText(saveFileName, "\"" + ls.specs[s] + "\"");
                    }
                }
                FileAccessPlugin.File.AppendAllText(saveFileName, "];\r\n");
                FileAccessPlugin.File.AppendAllText(saveFileName, ";\r\n");
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

        public class LightSpecs
        {
            public string name { get; set; } = "Light";
            public List<string> specs { get; set; } = new List<string>();
            public Vector3 position { get; set; } = Vector3.zero;
            public Vector3 rotation { get; set; } = Vector3.zero;
            public LightBehaviour behaviour { get; set; } = new LightBehaviour();
            public LightMenu menu { get; set; } = new LightMenu();
        }

        public class LegacyLightSpecs
        {
            public string name { get; set; } = "Light";
            public LightType lightType { get; set; } = LightType.Point;
            public string iconName { get; set; } = "Light.png";
            public float intensity { get; set; } = 0.01f;
            public string color { get; set; } = "255,255,128";
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

