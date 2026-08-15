            
Reads KERBAL_EVA_MODULES after parts load, registers dynamic definitions, and adds only the lightweight controller to each EVA prefab.
        
## Methods


### IsReady
Indicates that the loader has no asynchronous work to finish.
> #### Return value
> Always true.

### StartLoad
Builds the dynamic module registry and installs the KerbalGear controller.

### registerDynamicModule(System.String,ConfigNode)
Registers a dynamic module and removes loader-only values from its runtime config.
> #### Parameters
> **moduleName:** The PartModule class name.

> **sourceConfig:** The KERBAL_EVA_MODULES module node.


