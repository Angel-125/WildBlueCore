            
Adds the always-present KerbalGear controller to EVA prefabs and registers the remaining KERBAL_EVA_MODULES entries for on-demand creation by that controller.
        
## Methods


### TryGetModuleDefinition(System.String,WildBlueCore.KerbalGear.KerbalGearModuleDefinition@)
Finds the configuration used to create a requested EVA module at runtime.
> #### Parameters
> **moduleName:** The requested PartModule class name.

> **definition:** The registered definition, when found.

> #### Return value
> True when KERBAL_EVA_MODULES defines the requested module.

### EVAModulesLoader.IsReady
Indicates that the loader has no asynchronous work to finish.
> #### Return value
> Always true.

### EVAModulesLoader.StartLoad
Builds the dynamic module registry and installs the KerbalGear controller.

### EVAModulesLoader.registerDynamicModule(System.String,ConfigNode)
Registers a dynamic module and removes loader-only values from its runtime config.
> #### Parameters
> **moduleName:** The PartModule class name.

> **sourceConfig:** The KERBAL_EVA_MODULES module node.


### Awake
Inserts the KerbalGear loader immediately after KSP's PartLoader.

