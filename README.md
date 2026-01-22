# Morrowind Map Generator

A tool to (relatively) quickly generate a full interactive web map for Morrowind using your load order and textures. The web map includes markers for locations, doors, and fast travel points, all exported from the game files.

## Instructions

When running the tool, the first step is to select your configuration based on whether you use OpenMW or Morrowind.
- OpenMW, you will need to select your `openmw.cfg`, it will detect the default location, so you may not need to change this unless you have a custom setup for OpenMW.
- Morrowind, you will need to select your `Morrowind.ini` which is in the game folder. If you are using Mod Organizer, you will need to run the map generator through Mod Organizer for it to work correctly.

You should also select an output folder where your final files will be generated.

There are multiple steps to the map generator, and you can control them to prevent re-doing the same tasks.

### Generate Tiles
If enabled, OpenMW Map Generator will be downloaded and run to generate map tiles for your mod list (this should work regardless of whether you use OpenMW or not).
If disabled, the tool will look for a `tiles` folder next to the map generator executable and use tiles it finds there. You can use tiles generated from Morrowind with the `CreateMaps` console command as well, but you will need to handle that yourself.
 
### Generate Markers
If enabled, tes3conv will be downloaded and run to convert your entire load order, then all cell names, door locations, and fast travel routes will be extracted and saved to a markers.json in the output folder.
If disabled, the tool will look for an existing markers.json in the output folder.

### Generate Big Map
If enabled, the generated tiles will be stitched together into one large map image. These maps are generally very large and not very practical, but the option is here.

### Generate Web Map
If enabled, the generated tiles will be converted to a zoom pyramid and an interactive web map using leaflet.js will be generated for browsing the map.

### 512px Mode
If enabled, web map tiles will all be 512px instead of the default 256px, this can be very helpful if you upload the map anywhere that might rate limit you, as it significantly reduces the number of web requests made when browsing the map (as well as the number of generated files).

### Layers
If you have a folder of tiles with coordinate names `(x,y,).png` that map to Morrowind grid coordinates, you can add them as a layer here, and if the web map is generated, these files will be converted to a zoom pyramid as well, and added as toggleable layers.

You can specify the order and names of layers, as well as their default enabled state. 

## Generated Files

Once you're done setting up, just click Generate Map, and wait for it to complete. In your output you will have;
- map.png, if you generated a big map, which will be a very large full-size stitched together map of your game.
- map.html, if you generated the web map, which will open the interactive map in your browser.
- tiles, a folder of tiles for the web map, if you generated one.
- markers.json, a record of the markers exported, will be used if you don't generate markers next time.

Next to the map generator executable, there will be a `tiles` folder containing the map tiles that were generated, and a `tools` folder containing the downloaded tools. There will also be a `settings.json` file containing the settings used to generate the map.