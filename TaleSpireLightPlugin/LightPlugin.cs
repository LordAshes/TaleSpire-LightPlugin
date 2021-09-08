using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;

using System.Collections.Generic;
using System;

/// <summary>
/// 
/// Notes:
/// 
/// 1. Try to keep this main page simple with just the Unity/TS methods like Awake() and Update() methods
/// 2. Place your processing code in class specific files or a general class file like the sample Handler.cs file
/// 3. You can make your additional class file part of the main plugin class by making them "public partial class TemplatePlugin : BaseUnityPlugin"
///    (See the sample Handler.cs and/or Unity.cs files for an example)
/// 
/// </summary>

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.FileAccessPlugin.Guid)]
    [BepInDependency(LordAshes.StatMessaging.Guid)]
    [BepInDependency(RadialUI.RadialUIPlugin.Guid)]
    public partial class LightPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Light Plug-In";                     
        public const string Guid = "org.lordashes.plugins.light";       
        public const string Version = "1.4.1.0";                        

        // Configuration
        private ConfigEntry<KeyboardShortcut> triggerKey { get; set; }

        private Dictionary<string,LightSpecs> lights = new Dictionary<string,LightSpecs>();

        private int flickerSequencer = 0;
        private int flickerSteps = 20;

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            // Not required but good idea to log this state for troubleshooting purpose
            UnityEngine.Debug.Log("Light Plugin: Lord Ashes Template Plugin Is Active.");

            // Create root character light menu
            RadialUI.RadialSubmenu.EnsureMainMenuItem(LightPlugin.Guid, RadialUI.RadialSubmenu.MenuType.character, "Light", FileAccessPlugin.Image.LoadSprite("Light.png"));

            // Create light sub menu
            List<LightSpecs> lightAvailable = JsonConvert.DeserializeObject<List<LightSpecs>>(FileAccessPlugin.File.ReadAllText("LightTypes.json"));
            lightAvailable.Insert(0, new LightSpecs() { name = "None", iconName = "None.png" });
            foreach (LightSpecs light in lightAvailable)
            {
                RadialUI.RadialSubmenu.CreateSubMenuItem(LightPlugin.Guid, light.name, FileAccessPlugin.Image.LoadSprite(light.iconName), 
                                                         (cid,s,mmi)=> { RadialMenuRequest(cid, RadialUI.RadialUIPlugin.GetLastRadialTargetCreature(), light.name); },
                                                         true, 
                                                         ()=> 
                                                         {
                                                             bool isGM = LocalClient.IsInGmMode;
                                                             bool isOwner = LocalClient.CanControlCreature(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()));
                                                             // Debug.Log("isGM:" + isGM + ", isOwner:" + isOwner + ", requiredGM:" + light.onlyGM);
                                                             if (isOwner && (isGM || light.onlyGM==false))
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

            // Obtain Flicker Configuration
            flickerSteps = Config.Bind("Settings", "Updates per flicker update", 20).Value;

            // Subscribe to light events
            StatMessaging.Subscribe(LightPlugin.Guid, StatMessagingRequest);

            // Post on main page
            Utility.PostOnMainPage(this.GetType());
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
        }
    }
}

