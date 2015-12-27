# TellySorter

TellySorter is a doobry for organising that mess of TV episode files which results from running a DVR for far too long. :)

You feed it a list of folders to search and it goes through and identifies all the files using the TV DB API
(http://thetvdb.com) and renames those files according to rules you've defined.  Once an episode has been identified, it
will then move that file (if necessary) to its correct location, again based on another set of rules you've defined.

TellySorter supports multiple target directories, so you set a default target folder to organise everything into, and then
you can specify separate target folders for individual shows.

I may also add film support and a GUI at a later date.

You can preview changes without actually making them by adding the --simulate flag to your command.

## Getting started

Step le un is to go and net yourself an API key from http://thetvdb.com/?tab=apiregister then run this command:

    mono TellySorter.exe set ApiKey <your_api_key_here>

This will store your API key in the database and allow the rest of the app to work.
