WardenCreate

For generating update zips from a directory with a manifest and relevant scripts/files.

Needs Flux Inc private key on the local key ring.

Prototype in Racket, just select a directory from the file picker and it will output the update zip.

ISSUES:

Directory you pick cannot be named "payload".  Will need to work on using temp files instead.