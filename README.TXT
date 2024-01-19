WHAT IS QUADOT ARENA?

QUADOT ARENA is the QUAKE 3 ARENA module of the "Generations" (Behemoth) Project, that aims to have all the Classic ID games (Wolf3d, DOOM, Quake1 and Quake3), playable under the Godot Engine in the spirit of the late Quake2 Generations (https://web.archive.org/web/20020306115539/http://www.planetquake.com/generations/) 

 

WHY? THERE ARE A LOT OF OTHER SOURCEPORT DEVELOPED

QUADOT ARENA is not a source port, It's done in Godot where it's read all the bsp map data, and then process it along with the model, which allows to get the geometric data of the maps, to have a polygonal representation which Godot can work with, then the gameplay elements are added on top of it to have Quake3 Arena "feel" gameplay.

 

HOW DOES IT ACHIEVE QUAKE3 ARENA GAMEPLAY?

At startup it load the default pk3 (doesn't have a pk3 selector yet), the it cache the textures, images, sounds and models, then it start reading the corresponding map information (lump entities, brushes, leaf, surfaces, etc) then it start building and populating the map dynamically, basically all of Quake3 Arena gameplay information/elements were taken from https://quake.fandom.com
 

WHY THE GODOT ENGINE?

Because it's a Free and open-source cross-platform game engine that right now has a huge community support and doesn't do any Runtime Fee (Tax), nor does it change the TOS in an arbitrary way and tries to enforce it retroactively. 

TECHNICAL STUFF:

BEHIND THE CODE

Rendering and Collision

Getting everything loaded for a map had some limitations, we got the same problem that Carmack got when porting Wolf3d for the SNES, the hardware began to struggle. As everything was loaded on a map and having only Frustum Culling (even though Godot offers Occlusion Culling, it must be used for static items, as it need to bake the map in order to get the data to know what to occlude, however as everything is loaded dynamically we cannot baked it) we had a lot of overdrawn getting even more than 8000 draw calls in really populated maps. Quake3 solved this with Binary Space Partition, however as our approach was different as we were working with meshes we wanted to move forward to a more modern approach, here is a good read (http://www.robotrenegade.com/articles/id-tech-3-optimization/vis.html) so basically every surfaces has brushes and leaf which needed to be broken down to convex clusters (BSP) to be culled with a Potentially Visible Set (PVS). We then take all the static geometric data and start intersecting the planes in order to get the vertexes that are going to be use to get a convex hull, as Godot has support for convex polyhedron shapes.
 

External Resources

QUADOT ARENA can load external resources (HD textures, etc), the fastest way is to have everything loaded as resource in Godot and then it's packed and ready for use, however that present a huge limitation when trying to use other textures, graphics, whatsoever, so it was design to load everything externally then search for internal data and finally inside the PK3. 

END GOAL FOR QUADOT ARENA

The rendering code was design for a 4 (8?) player spit screen, so that would be finished as soon as we get ASAP, Online multiplayer will be implemented also to work between platforms (PC, Android, etc)

REMEMBER THIS IS STILL A WORK IN PROGRESS

Feel free to contact me by discord: triggercoder