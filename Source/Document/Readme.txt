Isles v0.1 Readme File
----------------------
Jan. 2008. (c) Nightin Games.


Welcome to Isles. This file contains information to help you install, play and troubleshoot Isles. For source code and bug traces, check out the project hosted at http://www.codeplex.com/isles.


-----------------
Table Of Contents
-----------------

1. System Requirements
2. Installing
3. Playing
4. Known Issues
5. Contact


----------------------
1. System Requirements
----------------------

Processor	- 1.4 gigahertz (GHz) or higher processor
Memory		- 128 megabytes (MB)
Hard disk	- 100 MB free hard disk space
Operating System	- Windows XP/Vista
Vidiocard	- 64 MB video card with DirectX(TM) and Shader Model 1.4 support
Soundcard	- DirectX 9.0c compliant sound card
DirectX		- DirectX 9.0c
.NET Framework	- Microsoft .NET Framework 2.0 Redistribution
XNA Game Studio	- Microsoft XNA Game Studio 2.0 Redistribution


-------------
2. Installing
-------------

You can choose to install Isles from both source code and XNA Creators Club Game Package (.ccgame).
For details on how to access the Isles repository, check out http://www.codeplex.com/Isles.
For details on how to install from a .ccgame package, check out http://msdn2.microsoft.com/en-us/library/bb464158.aspx.


----------
3. Playing
----------

- User Interface: Games states will be shown on the top right corner of your screen. A scroll panel on the bottom left corner shows the icons of all the actions you can perform. E.g., spells, buildings.

- Navigation: Move your hand to the borders of the screen to scroll the map (Hotkey: WSAD). Wheel your middle button to adjust view distance (Hotkey: Page Up/Page Down). Press & hold your middle button to adjust view angle. You can simulate the middle button by pressing & holding your left and right button simultaneously (Hotkey: Arrow keys).

- Building: Click the Build icon (Hotkey: B) to enter build menu, select the building you wish to build, and place it somewhere on the landscape by clicking on the target location. You can adjust the orientation of the building by pressing & holding your mouse while placing it. You can only place the building on a flat terrain. Buildings turn red when the location is invalid.

- Drag & Drop: Right click & hold an object (E.g., trees, rocks) until you gain enough power to pick it up. Then click on the ground anywhere you want to place it. Placing the object onto your buildings (Townhall or storage) adds to the total amount of wood or gold.

- Farming: Build a windmill to store food. Build several farmlands to grow crops. Harvest the crops by right clicking & holding the farmland.

- Spells: Select a spell from the scroll panel (E.g., Fireball). The way a spell being casted differs. In most cases, simply cast the spell by left clicking on the target.

- Creatures: Select a creature by left clicking it, then right click on the landscape to move your creature to the target position.


---------------
4. Known Issues
---------------

This is still a preliminary version of Isles, most of the gameplay and graphics features are not implemented, and will be affected by future changes. Still, there are some known issues that might surprise you.

- The landscape is a square viewed from far away, you can even see the borders. The ocean doesn't look much like an ocean, but rather a plane of fog. And sometimes the edge between the water and terrain jitters: There's currently no solution to this until we have refined our landscape rendering pipeline next month.

- The camera and hand shakes when the game frame rate is low or inconsistent: We have tracked this bug and hopefully it will be fixed in our next release.

- The game frame rate dropped sharply after building several farmlands: That's the problem with our billboard system, and we will be optimizing it.

- Why can't I destroy a building after throwing dozens of fireballs onto it? Why are crops ripe and ready to harvest immediately they are seeded? Currently, game entities do not have a so-called health property, so fireball is just a demonstration, a lot of the game logic is not finished yet :(


----------
5. Contact
----------

<HUANG Yufei, zt.nightin@gmail.com>
<MENG Biping, mbp24645860@yahoo.com.cn>
