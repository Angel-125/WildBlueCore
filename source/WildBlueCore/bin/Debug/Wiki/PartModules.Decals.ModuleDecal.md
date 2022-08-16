            
This part module lets you change the decal using the stock flag selector. It does so independently of the mission flag.
        
## Fields

### decalURL
URL to the image that's displayed by the decal.
### isVisible
Flag to indicate whether or not the decal is visible
### alwaysVisible
Override flag to ensure that the decal is always visible.
### allowFieldEdit
Flag to allow users to change the flag while out in the field.
### updateSymmetry
Flag to indicate if the decal updates symmetry parts
### toggleDecalName
GUI name for button that toggles decal visibility
### selectDecalName
GUI name for button that selects the decal.
### decalTransforms
List of transforms that will be changed by the decal. Separate names by semicolon
## Methods


### ToggleDecal
Toggles visibility of the decal.

### SelectDecal
Changes the decal

### onFlagSelected(FlagBrowser.FlagEntry)
Private event handler to respond to flag selection.
> #### Parameters
> **selected:** The selected texture


### ChangeDecal
Changes the decal on all named transforms.

