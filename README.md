# TellySorter

TellySorter is a doobry for organising that mess of TV episode files which results from running a DVR for far too long. :)

You feed it a list of folders to search and it goes through and identifies all the files using the TV DB API and renames
those files according to rules you've defined.  Once an episode has been identified, it will then move that file (if
necessary) to its correct location, again based on another set of rules you've defined.

TellySorter supports multiple target directories, eg you can set a default target folder to organise everything into, and
then specify a separate target folder on a per-show basis.

I may also add film support and a GUI at a later date.

You can preview changes without actually making them by adding the --simulate flag to your command.
