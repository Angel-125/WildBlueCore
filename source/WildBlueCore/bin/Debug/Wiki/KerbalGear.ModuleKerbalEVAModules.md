            
Special thanks to Vali for figuring out this issue! :) The Vintage, Standard, and Future suits are all defined in separate part modules that are combined when KSP starts. The problem is that when Module Manager is used to add part modules to the kerbal, you'll get duplicates. One solution is to disable or outright remove the duplicate part module, but we have several part modules to manage. So to get around that problem, the ModuleKerbalEVAModules adds a custom LoadingSystem that adds any part modules defined by a KERBAL_EVA_MODULES node to the kerbals. Simply define a KERBAL_EVA_MODULES config node with one or more standard MODULE config nodes, and they'll be added to the kerbals.
            
            
> #### Example
```

            KERBAL_EVA_MODULES
            {
                MODULE
                {
                    name = ModuleWearablesController
                    debugMode = false
                }
                
                MODULE
                {
                    name = ModuleEVAOverrides
                }
            }
            
```

            
        

