            
This is a simple base class that defines common functionality. Part modules should derive from it; it's not intended to be used directly in a part config.
            
            
> #### Example
```

            MODULE
            {
                name = BasePartModule
                moduleId = warpEngine
                debugMode = true
            }
            
```

            
        
## Fields

### debugMode
Flag to indicate whether or not the module is in debug mode.
### moduleID
ID of the module. Used to find the proper config node.
## Methods


### getPartConfigNode(System.String)
Retrieves the module's config node from the part config.
> #### Parameters
> **className:** Optional. The name of the part module to search for.

> #### Return value
> A ConfigNode for the part module.

### loadCurve(FloatCurve,System.String,ConfigNode)
Loads the desired FloatCurve from the desired config node.
> #### Parameters
> **curve:** The FloatCurve to load

> **curveNodeName:** The name of the curve to load

> **defaultCurve:** An optional default curve to use in case the curve's node doesn't exist in the part module's config.


