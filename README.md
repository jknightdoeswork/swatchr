# Swatchr
![Swatchr Logo](https://i.imgur.com/MUNRmkC.png "Swatchr Logo")

# Overview
Swatchr allows Unity developers to store color palettes inside scriptable objects in their projects. Renderers, particle systems, cameras and shaders can then reference colors as keys into swatches. Swatches can be replaced and updated at runtime, and the changes will propagate. Swatches can be exported to Unity's built in color picker system. Swatchr can be easily extended to custom components by implementing the SwatchrColorApplier class.

# Features
* Color palettes stored as scriptable objects in your project and repository
* Import swatches from .ase & .png files 
* Import swatches from MagicaVoxel's palette export (See Note)
* Export swatches to Unity's built in Color Picker tool
* Components to apply color to Mesh Renderer, Sprite Renderer, Particle System, Light and Camera clear color
* Nice editor UI
* Comes with the AAP-Splendor-128 color palette designed by [@AdigunPolack](https://twitter.com/adigunpolack/status/993524761019015168)

# How To

## Creating a Swatch
Create an empty Swatch by right clicking in the Project Window and going to Swatchr -> Create New Swatch. Click on the .asset file to view it's Editor UI in the Inspector. Add colors to it by clicking the + button. Click on the color next to the selected color to use Unitys color picker to pick a color.

## Importing a .ASE file
Right click in the Project Window and go to Swatchr -> Import ASE File (Browse...). Use the file selector to select the .ase file. A swatch will be created.

## Exporting to Unity's Color Picker
Select the swatch you want to export. Hit the Export To Color Picker Presets button. It will create a file with the .colors extension, and place it in an Editor/ folder. You can move that file, but it has to be in a folder path with Editor/ in it. Now go to the Unity Color Picker and click the dropdown to the top right of the color grid. Select the name of the Swatch you just exported. Now that swatch is available in Unity's default color picker.

## Importing a PNG
You can import a palette from a png by right clicking in your Project Window and going to Swatchr -> Import Swatch From Texture (Browse...). Note that every pixel in the png will become a color in the Swatch. There is no intelligent palette picking.

## Importing MagicaVoxel palettes
MagicaVoxel exports it's palettes with color profiles embedded in the png. Unity does not respect these color profiles. To work around this, export your model using the export .fbx option and then use that png file to import your swatch. Once you have the palette exported from this method, right click in your Project Window and go to Swatchr -> Import Swatch From Texture (Browse...).

## Swapping Swatches
Create a swatch for your project, eg) "MySwatch", and then use that swatch everywhere. Now make a backup of that swatch and create alternative swatches. Now use the Replace button on MySwatch to swap color palettes.

## Selecting Materials
Try the Legacy -> Diffuse material to get a pure, clean, low poly look.

# Technical Details
* Swatch.cs is a scriptable object that contains an array of colors.
* SwatchrColor.cs exposes a color property that uses an integer key and a Swatch to return a color.
* SwatchrColorApplier.cs is an interface for a component that will automatically apply a SwatchrColor to a GameObject when the Swatch changes.
* SwatchrRenderer.cs is one of several implementations of SwatchrColorApplier. This class applies the SwatchrColor to a MeshRenderer or SpriteRenderer.
* Other examples of SwatchrColorApplier are SwatchrLight, SwatchrParticleSystem & SwatchrAmbientLightColor.

# Screenshots
![Swatch Asset](https://i.imgur.com/xxtcCix.gif "Swatch Gif")
![Swatch Asset](https://i.imgur.com/Trtywop.png "Swatch Asset")
![Color Picker](https://i.imgur.com/qCEx68a.png "Color Picker")