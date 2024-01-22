## Into the Radius Save Editor

This is a console based save editor for Into the Radius for PCVR. It allows players to update their credit balance and security level. It is compatible with Into the Radius 2.x zlib compressed save files.

*Usage*

Run the app from a commandline with adminstrator privileges and use the -s flag to specify the path to your save file.

./itr-save-edit.exe -s "c:\Users\[username]\Documents\My Games\IntoTheRadius\v2.0\saves\IntoTheRadius.1.1.save"

Save files are saved with the format, 

IntoTheRadius.[profile].[slot 1-4].save

Autosave slots have the format,

IntoTheRadius.[profile].[slot 11-13].save

By default the editor will provide 1,000,000 credits and security level 5. To specify credit amount and level use the command line switches -c and -l respectively.

./itr-save-edit.exe -c 5000 -l 4 -s "c:\Users\[username]\Documents\My Games\IntoTheRadius\v2.0\saves\IntoTheRadius.1.1.save"

http://paypal.me/substatica
