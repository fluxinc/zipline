WardenCreate

For generating update zips from a directory with a manifest and relevant scripts/files.

Needs Flux Inc private key on the local key ring, and gpg installed.

Graphical Prototype in Racket, just select a directory from the file picker and it will output the update zip next to the selected directory.

Initial python version:  

> chmod +x WardenCreate.py
> 
> ./WardenCreate.py *directory-that-contains-update-files*

The directory doesn't need to be named anything in particular.  The only thing either script needs is the location of the update files (manifest.yml, scripts, etc).  Everything will be renamed correctly automatically.
