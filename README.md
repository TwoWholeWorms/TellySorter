# TellySorter

TellySorter is a doobry for organising that mess of TV episode files which results fromliving digitally for too
long. :)

You feed it a list of folders to search and it goes through and identifies all the files using the TV DB API
(http://thetvdb.com) and renames those files according to rules you've defined.  Once an episode has been
identified, it will then move that file (if necessary) to its correct location, again based on another set of
rules you've defined.

TellySorter supports multiple target directories, so you set a default target folder to organise everything into,
and then you can specify separate target folders for individual shows.

I may also add film support and a GUI at a later date.

You can preview changes without actually making them by adding the --simulate flag to your command.

## Getting started

Step le un is to go and net yourself an API key from http://thetvdb.com/?tab=apiregister then run this command:

    mono TellySorter.exe set ApiKey <your_api_key_here>

This will store your API key in the database and allow the rest of the app to work.

Now, add a list of directories you want it to search like this:

    mono TellySorter.exe source add ~/TV
    mono TellySorter.exe source add ~/Media
    mono TellySorter.exe source add /Volumes/nas/TV
    mono TellySorter.exe source add C:\Media\TV

Finally, set a default target path where you want everything to end up using one of the following:

    mono TellySorter.exe set DefaultTargetPath C:\SortedMedia\TV
    mono TellySorter.exe set DefaultTargetPath /Volumes/me/Media/TV
    mono TellySorter.exe set DefaultTargetPath ~/Media/TV

Now, run a simulated process with this command:

    mono TellySorter.exe process --simulate

Hopefully, it should run through all your files, match them against the TV DB database and spit out details of how
it would rename the files (if necessary) and tell you where any matching failures occurred. If a show /is/ unable
to be automatically matched, it will give you a list of potential matches and all you need to do is run this
command to fix it:

    mono TellySorter.exe set ShowId <series_id> <tvdb_show_id>

Where <series_id> is the id given to you in the example command, and <tvdb_show_id> is the correct TV DB show id
for that series. If the actual show isn't in the list given, you can find it online at http://thetvdb.com. If it
doesn't exist, create it! The TVDB is user-editable, so if something's missing or wrong, you can simply add it. :)

Finally, once all the errors are fixed, run the process for reals like this:

    mono TellySorter.exe process

Et voilà, all your files should now be stored in the correct place with the correct series, season, and episode
names. :)

## Advanced usage

TellySorter has a rudimentary rules system built in to it. At the moment, this is basically limited to telling it
to put specific shows into different target paths.

To set a show-specific target path, run this command:

    mono TellySorter.exe rule target <tvdb_show_id> <path>

Where <tvdb_show_id> is the id of the series on the TV DB, and <path> is the base folder where you would like the
files to end up. This folder should not contain the series name or season numbers, as these will be created
automatically and added to the path during processing. :)

## To-do list

GUI. So much GUI. >.<

Also, switch out TagLib for MediaInfo because TagLib doesn't properly match things.

## Acknowledgements

Props to Jer Vannevel for [TVDBSharp](https://github.com/Vannevelj/TVDBSharp), and to
[the TV DB](http://www.thetvdb.com/) themselves for providing such a brilliant API and user-editable database for
identifying things.
