# S.T.A.L.K.E.R. 2 Pak cfg Merge Tool

S.T.A.L.K.E.R. 2 Pak Cfg Merge Tool is a utility for merging configuration mod packs for the game *S.T.A.L.K.E.R. 2: Heart of Chornobyl*.  
This tool automatically detects conflicting `.cfg` files in mod packs and creates a single `.pak` archive with all the merged `.cfg` files inside.

## Usage
Open up run.bat file with any text editor.
Add the path to your game in the `run.bat` file and execute it.  
Alternatively you can run the tool directly from the command line or a terminal:
```bat
Stalker2PakCfgMergeTool.exe "path_to_the_game"
```

## Step by step:
* Change "replace me :)" to your game folder root path in double quotes. There is an example in run.bat file.
* Save and close it.
* Make sure that your game is not running.
* If you are using Vertex Mod Manager, make sure all of the mods you are using are deployed.
* Double clik on the run.bat file to run it.
* Wait for "Open up the summary.html file in your browser to see the merge results? [y/n]" message to appear.
* Press 'y' key on your keyboard if you want to view changes to the original file or press any other key to close the console.
* _merged_cfg_modpack mod will be created, name is based on the number of mods in your folder and current date.
* Delete old merged_cfg_modpack if you have run the tool previously already.
* Run this tool again if you have added/removed/updated any of your mods.

## Do not delete or disable the original mod paks
merged_cfg_modpack will contain only the configuration files that had conflicts with other mods. Wihtout the original files merged mods migth not work anymore.
