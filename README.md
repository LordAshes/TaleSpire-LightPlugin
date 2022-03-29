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

```
1.7.2: Added SaveConfig method to dump lights configuration to KVP file
1.7.1: Bug fix: Exposed method and lights property are made static
1.7.0: Added UpdateLight() method to change light properties locally
1.7.0: Exposed lights dictionary for easier configuration file manipulation
1.6.3: Fixed bug with access to Radial UI menu when no hierarchy is defined
1.6.2: Fixed bug which prvented GUI Menu functionality
1.6.1: Fixed bug that did not allow use of KVP file without JSON
1.6.0: Modified for direct light object access
1.6.0: Modified configuration file format
1.6.0: Includes automatic conversion of legacy configuration to new format
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
```

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
The number of lights and their effects can be configured by modifying the *LightTypes.kvp* file
which replaces the previous *LightTypes.json* file. The kvp is a non-standard file format
but provide full access to the properties of the light object as opposed to a limited number
of properties wrapped by the plugin (as was the case previously). The following outlines the
format of the file and the common light object properties that were set in the previous
version of the plugin:

Each line ends with a semicolor (;). This includes any lines with a comment.

Each light starts with the entry in square brackets which contains the name of the light,
followed by a colon and then the type LightSpecs, such as:

[Torch : LightSpecs];

This is followed by any number of entries for the specific light. The entries for each light
have been grouped in sub-categries for easy categorization. The group are: menu, behaviour
and specs.

.menu.iconName=ball.png;

The iconName property determines the name of the icon file to be used for the light.

.menu.menuNode=;

The menuNode property is optional and determines which menu the light shows up if sub-menus
are being used. If all iconNode properties are empty then all lights will show up in a single
radial menu. If menuNode is set then the light when show only when that particular menu is
being displayed (see below). 

.menu.menuLink=;

The menuLink property is optional and determines what menu is shown if the corresponding menu
item is selected. This property is only used when using multipe menus (i.e. menuNode is set).

.menu.onlyGM=False;

The onlyGM property is used to determine if players can see the corresponding light option or
if it the light menu item is only visible to the GM. Please note this applied to the menu item
and not the light itself. You can either make the link to the menu onlyGM in which case only
the GM will be able to access that menu or you can set individual menu items as onlyGM in which
case only those items will not be visible to players.  

.behaviour.sight=False;

The sight property is used to determine if the light is only visible to the GM and the mini
owner (true) or if it is visisble to all players (false). Typically used to implement various
visions like darkvision.

.behaviour.hiddenBase=False;

The hiddenBase property is typically used with onlyGM lights to produce environment lighting
like fire pits, lanters, chandeliers and other effects without showing the mini to which the
light is attached. Saves the GM the step of having to hide the mini and also erases the name
of the mini so that the name does not show up in the GM view (and clutter the screen in cases
where there are a lot of lights). 

.behaviour.flicker=False;

The flicker property determines if the light is steady or if it flickes. There are two possible
flicker effects. When set to true a light can use either flicker effect or both. When the values
of intensityMin and intensityMax are different and flicker is true, the intensity of the light
will shift to random values between the min and max intensity. When deltaMax is not 0 then light
will also shift in the x and y direction, randomly, up to the deltaMax distance.

.behaviour.intensityMin=0;
.behaviour.intensityMax=0.05;

These properties are used to set the mininum and maximum intensity for a flickering light.
When flicker is false, intensityMax will be used for the light intensity.

.behaviour.deltaMax=0;

This proeprty determines how much the light can shift, randomly, from its specified centre when
flicker is true. Not used when flicker is false.

.position=0,1.5,0;

This property determines the position of the light with respect to the mini to which it is attached.

.rotation=90,0,0;

This property determines the rotation of the light with respect to the mini to which it is attached.
The default value of 90,0,0 is a light that is pointing straight down.

.specs=["type=2", "color=200,200,200", "range=3", "spotAngle=10", "shadows=2"];

The specs property is an list of Unity Light properties and their values. Any Unity Light property
whose value can be JSON serialzied and JOSN deserialized can be added into the specs list with its
JSON value. The above settings are the settings that were accessible in the previous version of the
plugin with shadow being an additional property (previously hard coded to 2). It should be noted that
while the value of specs is a JSON value, it is a array of strings, and not object properties, with
each key value pair being a string in the array. 

The type property determine the type of light. 0=Spotlight, Directional=1, Point=2, Rectangle=3,
and Disc=4. Rectangle and Disc may not be fully supported at this time. Spotlight creates a cone from
the position point in the direction of the rotation with the angle of the cone defined by spotAngle.
Directional creates a lightbeam from the position in the direction of the rotation. Point creates light
in all direction from the position point.

The color property determines the color of the light in R,G,B format. Each R, G and B value ranges from
0 to 255. The R, G and B values are separated by commas.

The range property indicates the maximum range of the light.

The spotAngle property determines the angle of the cone of the spot light.

The shadows determines the type of shadows produced. 0=None, 1=Hard, and 2=Soft.

As indicated above the new plugin interpreter allows any property of the Light object to be specified
as long as the value can be JSON serialized and JSON deserialized. Additional key value pairs can be
added as string entries to the array to set additional properties. See Unity Light object for more
details on what additional properties can be set.
  
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

### Configuring Update Interval

The R2ModMan configuration for the plugin contains an value which determines how often the light
flicker is updated. The higher the number the less frequently the light flicker is updated and
thus the less flickering. The smaller the value the faster the flickering will change. Min 0.

### Use As A Dependecny Plugin 

Plugins which want to modify a light's properties can use the ``UpdateLight(cid, ls)`` method
which updates a light to the specified LightSpecs. Such changes are not automatically propagated
to other clients allowing a light to be adjusted on one device and then the final results to be
sent to other clients.

Cid is the CreatureGuid of the mini that contains the light. If a light is not present one is
created. If a light is present the light is modified.

Ls is the LightSpecs specification of the light (see above).

The modified lights configruation can be saved to a KVP file using ``SaveConfig(filename)``.

## Limitations

1. Currently the KVP file is a local file which, if modified, need to be distributed to all
   players in order for the light be to correctly rendered on all players' devices.
      
2. Each mini supports only one light at a time. But you can always add a mini with a second
   effect and even use Grab/Drop plugin to grag it around automatically.
   
3. When a mini is not owned, the top level Light menu still shows but has no sub-options.

## With Great Powers Comes Great Responsibility

The new configruatin interpreter is capable of creating new objects on the fly and settings
property values for any of its properties. This makes the interpreter very powerful because
it means that if the Unity Light object is updated with new properties the Light Plugin will
be able to make use of them without any update to the Light Plugin. For the Light Plugin this
is not a big deal because the Light object does not have too many properties and thus the
previous plugin version was able to wrap most of them and make them accessible. However, for
more complex objects (like the Particle System) this is decrease the amount of work greatly
becuase the plugin no longer needs to wrap each proeprty to expose it.

The downside of this is that it does, theoretically, introduce a security risk. Since the
configuration can be used to create new objects and set any proeprty, it could be used to
inject malicious code to make the plugin do something is should not.

However, in most cases this is not a concern because the configruation files being run is
a local file and thus only a risk if you are running a configuration file created by someone
else. Since you are already trusting Lord Ashes by running his plugin, we can rule out the
concern that Lord Ashes will create a malicious configruation file so it really comes down
to configruation files provided by other players. On top of that, it is really easy to see
if a configuraton files is malicious. Open it up. If the configuration looks similar to the
sample configruation with properties having a single values (or a list in the case of specs)
then the file is okay. If, instead, any propety has what appears to be bunch of code slipped
into it then it is probably a malicious attempt to do something odd.

In 99% of cases this is not a concern but I wanted to mention it since technically it is
possible. I am not even sure you could accomplish with this exploit (if anything useful).
So just double check the configruation if you are getting it from a 3rd party.


