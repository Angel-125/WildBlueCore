            
Determines how KerbalGear creates a requested EVA PartModule.
        
## Fields

### Exclusive
Creates one module for the first deterministic provider. This is the default.
### Aggregate
Creates one module shared by all providers. The module must aggregate inventory state.
### PerProvider
Creates a separate module for each stored-part snapshot that requests it.

