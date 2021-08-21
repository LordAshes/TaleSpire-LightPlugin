using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;

using System.Collections.Generic;

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
        public const string Version = "1.0.0.0";                        

        // Configuration
        private ConfigEntry<KeyboardShortcut> triggerKey { get; set; }

        private Dictionary<string,LightSpecs> lights = new Dictionary<string,LightSpecs>();

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
            RadialUI.RadialSubmenu.CreateSubMenuItem(LightPlugin.Guid, "None", FileAccessPlugin.Image.LoadSprite("light.png"), (cid, s, mmi) => { RadialMenuRequest(cid, RadialUI.RadialUIPlugin.GetLastRadialTargetCreature(), ""); }, true, null);
            foreach (LightSpecs light in JsonConvert.DeserializeObject<LightSpecs[]>(FileAccessPlugin.File.ReadAllText("LightTypes.json")))
            {
                RadialUI.RadialSubmenu.CreateSubMenuItem(LightPlugin.Guid, light.name, FileAccessPlugin.Image.LoadSprite(light.iconName), (cid,s,mmi)=> { RadialMenuRequest(cid, RadialUI.RadialUIPlugin.GetLastRadialTargetCreature(),light.name); }, true, null);
                lights.Add(light.name, light);
            }

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
        }

        public class LightSpecs
        {
            public string name { get; set; }
            public LightType lightType { get; set; }
            public string iconName { get; set; }
            public float intensity { get; set; }
            public string color { get; set; }
            public float range { get; set; }
            public string pos { get; set; }
            public string rot { get; set; }
            public float spotAngle { get; set; }
        }
    }
}
