ZiplineCreate

For generating update zips from a directory with a manifest and relevant scripts/files.

Needs whatever private key matches the public key of the installed Zipline on the local key ring, and gpg installed.

Graphical Prototype in Racket, just select a directory from the file picker and it will output the update zip next to the selected directory.

### Python version  

> chmod +x ZiplineCreate.py


``` 
./ZiplineCreate.py -d, --directory (directory-that-contains-update-files) 
                   -r, --repeatable (if repeatable update) 
                   -b, --backend (future backend use)
```
The directory doesn't need to be named anything in particular.  The only thing either script needs is the location of the update files (manifest.yml, scripts, etc).  Everything will be renamed correctly automatically.
