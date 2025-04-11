            
This is a specialized class that creates a two-dimensional grid of meshes from a collection of meshes provided by the model. While it is possible to duplicate multiple copies of a single transform, research shows that the part's radial attachment system gets messed up when you do that. So for now, we have a grid that is limited by the total number of meshes in the model.
            
            
> #### Example
```

            MODULE
            {
                name = WBIModulePartGridVariants
                totalRows = 6
                totalColumns = 6
                elementTransformName = yardFrameAngled37-30
                elementLength = 3.75
                elementWidth = 3.75
                elementHeight = 0.1875
            }
            
```

            
        
## Fields

### elementTransformName
Base name of the meshes found in the part's model object. All model transforms start with this prefix. Individual elements in the mesh should have " (n)" appended to them. NOTE: Be sure to have a total number of elements equal to totalRows * totalColumns and be sure to label them from (0) to (totalElements - 1) Example: yardFrameFlat37 (0), yardFrameFlat37 (1) ... yardFrameFlat37 (35) Note that there is a space between the prefix and the element id.
### elementLength
Length of a single element, in meters.
### elementWidth
Width of a single element, in meters.
### elementHeight
Height of a single element, in meters.
### totalRows
Total number of rows that are possible in the grid.
### totalColumns
Total number of columns that are possible in the grid.
### rowIndex
Current selected row variant.
### columnIndex
Current selected column variant.
## Methods


### copyOriginalNodes(System.Collections.Generic.List{AttachNode})
Called when the part was copied in the editor.
> #### Parameters
> **copyNodes:** The list of AttachNode objects to copy into our originalNodes field.


