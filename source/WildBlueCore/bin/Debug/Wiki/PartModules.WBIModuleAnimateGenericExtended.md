            
This part module derives from ModuleAnimatedGeneric. It adds the ability to play sound effects while animating. It also adds several options to check before playing the animation. First, it can check the active vessel for a necessary skill and skill level. Second, it can check for and consume required resources. Those resources can be pulled from the part's vessel and/or from a remote vessel. Third, it can check for and consume parts stored in an inventory. Those parts can be pulled form the parts vessel and/or from a remote vessel. Finally, you can provide a list of part modules that will be enabled after tne animation completes, and disabled when when not complete.
        
## Fields

### debugMode
Debug flag
### startEventRequiresSkillCheck
Flag indicating whether or not to require a skill check before playing the start event animation. Default is false.
### endEventRequiresSkillCheck
Flag indicating whether or not to require a skill check before playing the end event animation. Default is false.
### skillToCheck
The skill required to unpack the part.
### minimumSkillLevel
The minimum skill level required to unpack the box. Default is 0.
### startEventRequiresResources
Flag to indicate whether or not to make a resource requirements check before playing the start event animation. Default is false.
### endEventGivesResources
Flag to indicate whether or not to make a resource requirements check before playing the end event animation. Default is false.
### canUseRemoteResources
Flag to indicate whether or not when checking resources, resources can come from other vessels. Default is true.
### startSoundURL
URL for the start sound played when the animation starts.
### startSoundPitch
Pitch level for the start sound.
### startSoundVolume
Volume level for the start sound.
### loopSoundURL
URL for the loop sound, played while the animation is playing.
### loopSoundPitch
Pitch level for the loop sound.
### loopSoundVolume
Volume level for the loop sound.
### stopSoundURL
URL four the stop sound, played when the animation is completed.
### stopSoundPitch
Pitch level for the stop sound.
### stopSoundVolume
Volume level for the stop sound.
## Methods


### OnStartFinished(PartModule.StartState)
This gets called after all part modules in the part have been started. Overriding this gives us the opportunity to disable all the part modules that we manage regardless of their load order.
> #### Parameters
> **state:** The StartState upon finishing the start process.


### Toggle
Toggles the animation.

### ToggleAction(KSPActionParam)
Action to toggle the animation.
> #### Parameters
> **param:** 


### SetProgress(System.Single)
Sets the Progress level. Call this instead of setting deployPercent directly.
> #### Parameters
> **toValue:** A float between 0 and 100.


### PlayStartSound
Plays the start sound.

### PlayEndSound
Plays the end sound.

