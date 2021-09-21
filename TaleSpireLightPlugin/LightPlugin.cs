using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;

using System.Collections.Generic;
using System;

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
        public const string Version = "1.5.0.0";                        

        // Configuration
        private ConfigEntry<KeyboardShortcut> triggerKey { get; set; }
        private GUIMenuPlugin.MenuStyle menuStyle { get; set; }
        private UnityEngine.Color menuLinkColor { get; set; }
        private UnityEngine.Color menuSelectionColor { get; set; }

        private Dictionary<string,LightSpecs> lights = new Dictionary<string,LightSpecs>();

        private int flickerSequencer = 0;
        private int flickerSteps = 20;

        private GUIMenuPlugin.GuiMenu menu = new GUIMenuPlugin.GuiMenu();

        void ABV()
        {
            GUIMenuPlugin.GuiMenu menu = new GUIMenuPlugin.GuiMenu(new GUIMenuPlugin.MenuNode[]
            {
                new GUIMenuPlugin.MenuNode("Root", new GUIMenuPlugin.IMenuItem[]
                {
                    new GUIMenuPlugin.MenuLink("Menu1","Menu1",UnityEngine.Color.red,FileAccessPlugin.Image.LoadTexture("Menu1.png"),false),
                    new GUIMenuPlugin.MenuLink("Menu2","Menu2",UnityEngine.Color.red,FileAccessPlugin.Image.LoadTexture("Menu2.png"),false)
                }, GUIMenuPlugin.MenuStyle.centre),
                new GUIMenuPlugin.MenuNode("Menu1", new GUIMenuPlugin.IMenuItem[]
                {
                    new GUIMenuPlugin.MenuLink("Root","Back",UnityEngine.Color.red,FileAccessPlugin.Image.LoadTexture("Back.png"),false),
                    new GUIMenuPlugin.MenuSelection("Item1","Item1",UnityEngine.Color.red,FileAccessPlugin.Image.LoadTexture("Item1.png"),false),
                    new GUIMenuPlugin.MenuSelection("Item2","Item2",UnityEngine.Color.red,FileAccessPlugin.Image.LoadTexture("Item2.png"),false)
                }, GUIMenuPlugin.MenuStyle.centre),
                new GUIMenuPlugin.MenuNode("Menu2", new GUIMenuPlugin.IMenuItem[]
                {
                    new GUIMenuPlugin.MenuLink("Root","Back",UnityEngine.Color.red,FileAccessPlugin.Image.LoadTexture("Back.png"),false),
                    new GUIMenuPlugin.MenuSelection("Item3","Item3",UnityEngine.Color.red,FileAccessPlugin.Image.LoadTexture("Item3.png"),false),
                    new GUIMenuPlugin.MenuSelection("Item4","Item4",UnityEngine.Color.red,FileAccessPlugin.Image.LoadTexture("Item4.png"),false)
                }, GUIMenuPlugin.MenuStyle.centre)
            });
        }

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            // Not required but good idea to log this state for troubleshooting purpose
            UnityEngine.Debug.Log("Light Plugin: Lord Ashes Light Plugin Is Active.");

            // Obtain Flicker Configuration
            flickerSteps = Config.Bind("Settings", "Updates per flicker update", 20).Value;
            menuStyle = Config.Bind("Settings", "GUI menu style", GUIMenuPlugin.MenuStyle.centre).Value;
            menuLinkColor = Config.Bind("Settings", "GUI menu link color", UnityEngine.Color.black).Value;
            menuSelectionColor = Config.Bind("Settings", "GUI menu selection color", UnityEngine.Color.gray).Value;

            // Create light sub menu
            UnityEngine.Debug.Log("Light Plugin: Deserialize JSON Light File.");
            List<LightSpecs> lightAvailable = JsonConvert.DeserializeObject<List<LightSpecs>>(FileAccessPlugin.File.ReadAllText("LightTypes.json"));
            bool submenus = false;
            foreach (LightSpecs light in lightAvailable)
            {
                if (light.menuNode != "") { submenus = true; break; }
            }
            if (submenus)
            {
                UnityEngine.Debug.Log("Light Plugin: Using GUI Menus With Hierarchy.");
                // Create root character light menu
                RadialUI.RadialUIPlugin.AddOnCharacter(LightPlugin.Guid, new MapMenu.ItemArgs()
                {
                    Title = "Light",
                    Icon = FileAccessPlugin.Image.LoadSprite("Light.png"),
                    Action = (s, a) => { Debug.Log("Opening GUI Menu"); menu.Open("Root", LightSelectionHandler); },
                    CloseMenuOnActivate = true
                });
                // Create GUI menu for light sub-selections
                foreach (LightSpecs light in lightAvailable)
                {
                    if (menu.GetNode(light.menuNode) == null)
                    {
                        Debug.Log("Making Node '" + light.menuNode + "'");
                        GUIMenuPlugin.MenuNode node = new GUIMenuPlugin.MenuNode(light.menuNode, new GUIMenuPlugin.IMenuItem[0], menuStyle);
                        menu.AddNode(node);
                    }
                    if (light.menuLink != "")
                    {
                        // Add Menu Link
                        Debug.Log("Adding Link '" + light.menuLink + "' To Node '" + light.menuNode + "'");
                        menu.GetNode(light.menuNode).AddLink(new GUIMenuPlugin.MenuLink(light.menuLink, light.name, menuLinkColor, FileAccessPlugin.Image.LoadTexture(light.iconName),light.onlyGM));
                    }
                    else
                    {
                        // Add Menu Selection
                        Debug.Log("Adding Selection '" + light.name + "' To Node '" + light.menuNode + "'");
                        menu.GetNode(light.menuNode).AddSelection(new GUIMenuPlugin.MenuSelection(light.name, light.name, menuSelectionColor, FileAccessPlugin.Image.LoadTexture(light.iconName),light.onlyGM));
                        lights.Add(light.name, light);
                    }
                }
            }
            else
            {
                UnityEngine.Debug.Log("Light Plugin: Using Flat Radial Menu.");
                // Create root character light menu
                RadialUI.RadialSubmenu.EnsureMainMenuItem(LightPlugin.Guid, RadialUI.RadialSubmenu.MenuType.character, "Light", FileAccessPlugin.Image.LoadSprite("Light.png"));
                lightAvailable.Insert(0, new LightSpecs() { name = "None", iconName = "None.png" });
                // Create sub-menu for light sub-selections
                foreach (LightSpecs light in lightAvailable)
                {
                    RadialUI.RadialSubmenu.CreateSubMenuItem(LightPlugin.Guid, light.name, FileAccessPlugin.Image.LoadSprite(light.iconName),
                                                             (cid, s, mmi) => { RadialMenuRequest(cid, RadialUI.RadialUIPlugin.GetLastRadialTargetCreature(), light.name); },
                                                             true,
                                                             () =>
                                                             {
                                                                 bool isGM = LocalClient.IsInGmMode;
                                                                 bool isOwner = LocalClient.CanControlCreature(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()));
                                                                 if (isOwner && (isGM || light.onlyGM == false))
                                                                 {
                                                                     return true;
                                                                 }
                                                                 else
                                                                 {
                                                                     return false;
                                                                 }
                                                             });
                    lights.Add(light.name, light);
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
                                if (lights[lightName].flicker)
                                {
                                    // Process custom light flicker
                                    System.Random rnd = new System.Random();
                                    float randomizer = rnd.Next(0, 100);
                                    float intensity = (randomizer / 100f) * (lights[lightName].intensity - lights[lightName].intensityMin) + lights[lightName].intensityMin;
                                    light.intensity = intensity;
                                    float dx = lights[lightName].deltaMax * (float)(rnd.Next(0, 100)) / 100f;
                                    float dy = lights[lightName].deltaMax * (float)(rnd.Next(0, 100)) / 100f;
                                    string[] pos = lights[lightName].pos.Split(',');
                                    light.transform.localPosition = new Vector3(float.Parse(pos[0]) + dx, float.Parse(pos[1]), float.Parse(pos[2]) + dy);
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

        [Serializable()]
        public class LightSpecs
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

