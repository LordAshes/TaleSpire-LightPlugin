# Light Plugin

This unofficial TaleSpire plugin is for adding custom light sources to your TaleSpire
game. The custom light options are accessible from the mini radial menu under an new
Light menu. Menu includes a None option to extinguish the current light. The plugin
comes with a few sample lights but the plugin is easy to configure with your own light
options and will automaticlaly enumerate those options in the radial menu.

Provides access to ambient, point and spotlight which can be placed at an offset to
the mini and their intensity, range, color and angle (for spot light) can be set.

Added light causes object to cast shadows. Added light produced hard light limits.

![Preview](https://i.imgur.com/ieRjT5J.png)
 
## Change Log

1.5.0: Added support for hierarchical light menus using GUI Menu Plugin

1.4.1: Bug fix for issue which prevented lights from being removed

1.4.0: Added optional properties intensityMin and deltaMax for two flickering light options

1.3.0: Added hiddenBase option

1.2.0: Added GM only light option (via onlyGM property)

1.2.0: Light menu does not show options for mini that the player does not own

1.2.0: Changed sight light distribution so that it can be triggered by player or GM

1.1.0: Added sight light option (light only visible by mini owner and GM)

1.1.0: Removal of light now removes Stats Messaging key (as opposed to making it blank)

1.0.0: Initial release

## Install

Install using R2ModMan.

## Usage

The plugin can be used out-of-the-box as soon as it is installed with the sample lights
but typically the GM will want to configure his/her own light settings appropriate for
the game.

Right click the mini to open the radial menu. Select the (new) Light menu. The sub-menu
will enumarate all of conmfigured lights and includes a None option to turn off the light.
Select one of the options to turn on the corresponding light. If a light is already on,
the light will switch to the selected type. If no light is currently on, the selected
light will be turned on. Select the None sub-menu option to turn off the light.

### Configuring Lights

The number of lights and their effect, provided by the plugin, is completely configurable.
The plugin comes with a JSON (text) file which can be opened and edited to add, modify or
remove lights. The structure for a light is as follows:

	{"name":"Ball(10')",
		"lightType":2,
		"iconName":"ball.png",
		"intensity":0.05,
		"color":"200,200,200",
		"range":3.0,
		"pos":"0,1.5,0",
		"rot":"90,0,0",
		"spotAngle":10.0,
		"flicked":true,
		"intensityMin":0.025,
		"deltaMax":0.05,
		"sight":false,
		"onlyGM":false,
		"hiddenBase":false
	}

Each entry for a light is separated by a comma and the whole thing is enclosed in a set of
square brackets (as per the sample JSON that comes with the plugin).

"name" is the name associated with the light and is the text that is displayed in the Light
radial sub-menu.

"lightType" determine the type of light 0=Spotlight, Directional=1, Point=2, Rectangle=3,
and Disc=4. Rectangle and Disc may not be fully supported at this time. Spotlight creates
a cone from the position point in the direction of the rotation with the angle of the cone
defined by spotAngle. Directional creates a lightbeam from the position in the direction of
the rotation. Point creates light in all direction from the position point.

"iconName" determines the name of the PNG file to be used in the radial menu for this light.

"intensity" determines how stong the light is (normally between 0 to 1).

"color" is a RGB string consisting of the red, green and blue values expressed as a value
from 0 to 255, separated by a comma.

"range" determines how far the light reaches.

"pos" is a string of x, y, z float co-ordinates, separataed by commas, indicating the position
offset from the base of the mini.

"rot" is a string of x, y, z float co-ordinates, separataed by commas, indicating the rotation
offset from the base of the mini.

"spotAngle" is a float indicating the angle of a spot light.

"flicker" is a boolean (true or false) which determines if the light uses either or both of the
plicker effects. Default is false. See "intensityMin" and "deltaMax" for details.

"intensityMin" is a float that determines the minimum intensity that the flickering light will
drop to. When flicker is on the intensity will randomly changes between intensityMin and intensity.
Not used if the flicker setting for the light is set to false. As far as I can tell, this is the
flicker method used by TS. Setting this to the same as intensity will produce a non-changing
intensity.

"deltaMax" is a float which determines how much the light can randomly shift when flickering.
When this value is not zero the light will randomly shift in the x and y direction up to this
value. This setting is ignored if the light's flicker setting is false. Used to make the shadows
shift or "dance". Setting this setting to 0 will provide a non-shifting light.

"sight" indicates if the light is personal light (e.g. darkvision) or regular light which
benefits all party members. If true only mini owner and GM can see the light. If false then
all players can see the light.

"onlyGM" indicates if the light option is only visible to GM. Please note that this does not
mean the light is visible to GM only, it means the option to add it is limited to GM. This way
the GM can define lights like ambient lights or lights for the room and players will not have
those options in their mini light menu. 

"hiddenBase" indicates if the corresponding base should be automatically hidden. Typically, this
is used with onlyGM lights which are not associated with a mini such as ambient lighting, wall
mounted lights, hanging lights and so on. When this setting is set to true, activating the light
will automatically hide the mini (leaving only the light effect) and will also erase the base
mini name so that its name does not clutter the GM view. 

### Configuring Light Menus

There are two optional settings which can be used with light configuration to place them in
hierarchical menus (menus with sub-menus) instead of the flat (one level) Radial UI menus.
These properties are: menuNode and menuLink.

menuNode is used to indicate which menus the light belongs to. Each unique menuNode name is a
different menu. Items (lights) that should appear in the same menu have the same menuNode. The
main menuNode is always called Root. When the user clicks on the Light icon in the mini radial
menu, it will cause the Root menu to be opened. The Root menu can contain a combination of
light selections and menuLinks (which allow access to other menus).

menuLink is used to indicate the menuNode name of the menu that is to be navigated to when this
selection is chosen. For a menuLink entry only the name, iconName, onlyGM, menuNode and menuLink
settings are significant. All of the light settings are irrelevant for a entry with a menuLink 
since these entries are used to make navigation buttons as opposed to light selections.

By editing the R2ModMan configuration for the plugin, you can change the text color of link
entries, text color of selection entries and if the menu is a centre menu (menu appears in the
middle of the screen) or a side menu (menu slides out of the right side of the screen).

Sample config showing a 3 menu system: one main menu offering No Light selection and two
sub-menus (Spotlights and Ambient) and then entries for each of the sub-menus. The spot lightType
link is only available to GM. Notice that it was not necessary to mark each of the Spotlights as
GM only since the link to that menu is only available to GM. Notice the back entries which can
be used to navigate to the parent menu.

```[
	{"name":"None",
		"iconName":"none.png",
		"menuNode":"Root"
	},
	{"name":"Splotlights",
		"iconName":"spot.png",
		"onlyGM":true,
		"menuNode":"Root",
		"menuLink":"Spotlights"
	},
	{"name":"Ambient",
		"iconName":"ball.png",
		"menuNode":"Root",
		"menuLink":"Ambient"
	},
	{"name":"Back",
		"iconName":"none.png",
		"menuNode":"Ambient",
		"menuLink":"Root"
	},
	{"name":"Back",
		"iconName":"none.png",
		"menuNode":"Spotlights",
		"menuLink":"Root"
	},
	{"name":"Ball(10')",
		"lightType":2,
		"iconName":"ball.png",
		"intensity":0.05,
		"color":"200,200,200",
		"range":3.0,
		"pos":"0,1.5,0",
		"rot":"90,0,0",
		"spotAngle":10.0,
		"menuNode": "Ambient"
	},
	{"name":"Torch",
		"lightType":2,
		"iconName":"torch.png",
		"intensity":0.03,
		"color":"255,255,128",
		"range":2.0,
		"pos":"0,0.75,0",
		"rot":"90,0,0",
		"spotAngle":15.0,
		"flicker": true,
		"intensityMin": 0.025,
		"deltaMax": 0.05,
		"menuNode": "Ambient"
	},
	{"name":"Darkvision",
		"lightType":2,
		"iconName":"light.png",
		"intensity":0.03,
		"color":"255,255,128",
		"range":2.0,
		"pos":"0,0.75,0",
		"rot":"90,0,0",
		"spotAngle":15.0,
		"sight":true,
		"menuNode": "Ambient"
	},
	{"name":"Spot (White)",
		"lightType":0,
		"iconName":"spot.png",
		"intensity":0.03,
		"color":"255,255,255",
		"range":3.5,
		"pos":"0,3,0",
		"rot":"90,0,0",
		"spotAngle":45.0,
		"onlyGM":true,
		"menuNode": "Spotlights"
	},
	{"name":"Spot (Red)",
		"lightType":0,
		"iconName":"spot.png",
		"intensity":0.03,
		"color":"255,0,0",
		"range":3.5,
		"pos":"0,3,0",
		"rot":"90,0,0",
		"spotAngle":25.0,
		"onlyGM":true,
		"menuNode": "Spotlights"
	},
	{"name":"Spot (Green)",
		"lightType":0,
		"iconName":"spot.png",
		"intensity":0.03,
		"color":"0,255,0",
		"range":3.5,
		"pos":"0,3,0",
		"rot":"90,0,0",
		"spotAngle":25.0,
		"onlyGM":true,
		"menuNode": "Spotlights"
	},
	{"name":"Spot (Blue)",
		"lightType":0,
		"iconName":"spot.png",
		"intensity":0.03,
		"color":"0,0,255",
		"range":3.5,
		"pos":"0,3,0",
		"rot":"90,0,0",
		"spotAngle":25.0,
		"onlyGM":true,
		"menuNode": "Spotlights"
	}
]```

### Configuring Update Interval

The R2ModMan configuration for the plugin contains an value which determines how often the light
flicker is updated. The higher the number the less frequently the light flicker is updated and
thus the less flickering. The smaller the value the faster the flickering will change. Min 0.

## Limitations

1. Currently the JSON file is a local file which, if modified, need to be distributed to all
   players in order for the light be to correctly rendered on all players' devices.
      
2. Each mini supports only one light at a time. But you can always add a mini with a second
   effect and even use Grab/Drop plugin to grag it around automatically.
   
3. When a mini is not owned, the top level Light menu still shows but has no sub-options.


