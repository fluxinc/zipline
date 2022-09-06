# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.3] - 2022-09-06
### Added
- Configuration file that can set returnDirectory, ignoreHashLog, enableVoice, virtualUSB, and virtualRepeat options.
- virtualUSB option can configure a folder to act as a USB drive.  Any files dropped into the folder are checked to see if they are zipline updates and ran.

### Fixed
- Fix voice library removed for Release configuration.

## [1.0.2] - 2022-07-15
### Added
- Automatically set recovery options for service to restart on failure.

### Changed
- Renamed project & code references from Warden to Zipline.

## [1.0.1] - 2022-06-21
### Added
- Allow for repeatable updates
