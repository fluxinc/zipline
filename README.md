![Zipline Logo](zipline-logo.png)

# Zipline

_When the internet isn't there;  Zipline is._

Zipline is a agnostic script and executable runner for devices that cannot experience the world wide web.  Currently, this operates through a windows service which watches for new USB devices.  The root directory of the new device is scanned for any zip files matching the zipline pattern, and if found, those files are processed as updates.

## Installing

Zipline comes with Inno setup files predefined, so all you need to do is compile a build through VStudio, and then run the Inno compiler to generate a binary.  This will automatically install Zipline as a Windows service.

You will also need to go into Utility/VerifyDetachedSignature.cs and replace the public key with whatever one you want to use.

## Contributing

Please stick to semantic commits, if you are going to use a different style while on your personal branch, then please squash it down to a semantic version for any PRs.

## Features

- Private/Public key signing of updates for integrity and security.
- Hashes of updates that have been run are logged so they aren't run again.
- Repeatable updates can be generated that always run.
- Creation scripts (GUI and CLI) for easy update zip creation.
- Provides return logs to the USB.
- Ability to return arbitrary files too.
- Agnostic scripts or executables can be run, it is also easy to customize run commands on particular files
- Synthesized status updates for headless operation.
- Basic error handling to continue processing if one script/exe fails

## TODO

Please see our wiki and/or issues!

License
GNU General Public License v3.0 or later

See [LICENSE](LICENSE) to see the full text.
