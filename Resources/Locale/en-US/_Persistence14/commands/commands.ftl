cmd-ssd-desc = Mark yourself as SSD

cmd-persistencesave-desc = Saves server data to a persistence file to be loaded later.
cmd-persistencesave-usage = persistencesave <mapId> [filePath - default: game.map (CCVar)]
cmd-persistencesave-no-path = filePath was not specified and CCVar {$cvar} is not set. Manually set the filePath param in order to save the map.

cmd-persistencesavechar-desc = Saves entity data to a persistence file to be loaded later.
cmd-persistencesavechar-usage = persistencesavechar <mobId> [filePath - default: game.map (CCVar)

cmd-persistencesavegrid-desc = Saves grid data to a persistence file to be loaded later.
cmd-persistencesavegrid-usage = persistencesavegrid <gridId> [filePath - default: game.map (CCVar)]

cmd-persistenceload-desc = Loads a given post-init map save-file onto the first unused map ID.
cmd-persistenceload-usage = persistenceload <filePath>

cmd-persistenceloadchar-desc = Loads a given character save-file at the position of the player executing the command.
cmd-persistenceloadchar-usage = persistenceloadchar <filePath>

cmd-persistenceloadgrid-desc = Loads a given post-init grid save-file on a map at the given coordinates & rotation. Optionally set DeserializationOptions.StoreYamlUids enabled with 6th parameter.
cmd-persistenceloadgrid-usage = persistencesave <mapId> <filepath> <x-pos> <y-pos> <rot> [true/false - default: false]

cmd-hint-savechar-id = <Mob EntityID>

