# v0.16.0 (2022-07-13)

Maintenance release - dependency updates only.

# v0.15.0 (2022-05-27)

Maintenance release - dependency updates only.

# v0.14.0 (2022-03-25)

Maintenance release - dependency updates only.

# v0.13.0 (2022-01-16)

EDR is now Sequence. The following has changed:

- The GitLab group has moved to https://gitlab.com/reductech/sequence
- The root namespace is now `Reductech.Sequence`
- The documentation site has moved to https://sequence.sh

Everything else is still the same - automation, simplified.

The project has now been updated to use .NET 6.

## Issues Closed in this Release

### Maintenance

- Rename EDR to Sequence #43
- Update Core to support SCLObject types #39
- Upgrade to use .net 6 #38

# v0.12.0 (2021-11-26)

Maintenance release - dependency updates only.

# v0.11.0 (2021-09-03)

Dependency updates only

# v0.10.0 (2021-07-02)

## Issues Closed in this Release

### Maintenance

- Update Core to latest and remove SCLSettings #22

# v0.9.1 (2021-05-28)

Connector package is no longer runtime dependent.

## Issues Closed in this Release

### New Features

- Update connector packaging method to remove runtime dependency #21

# v0.9.0 (2021-05-14)

## Summary of Changes

### Core SDK

- Connector can now be used as a plugin for EDR

## Issues Closed in this Release

### New Features

- Allow this package to be used as a plugin #16

### Bug Fixes

- Connector package is missing dependencies #20

### Maintenance

- Enable publish to connector registry #18
- Update Core dependecies #17

# v0.8.1 (2021-04-13)

## Summary of Changes

### Connector Updates

- Renamed `RunScript` to `RunScriptAsync`
- Added a new `RunScript` step to enable executing scripts synchronously

## Issues Closed in this Release

### Bug Fixes

- Variable values passed to scripts are not serialized correctly #14
- Script does not execute unless the output is read #12

# v0.8.0 (2021-04-08)

- Update of Core dependencies only

# v0.7.0 (2021-03-26)

- Update of Core dependencies only

# v0.6.0 (2021-03-14)

- Update of Core dependencies only

# v0.5.0 (2021-03-01)

## Summary of Changes

- Structured logging

## Issues Closed in this Release

### New Features

- Update the version of Core to allow structed logging #6

## v0.4.0 (2021-01-29)

First release. Version numbers are aligned across `EDR`.

### New Features

- Allow scripts to accept an EntityStream as input #2
- Allow parameters to be passed to scripts, so that they can be customised dynamically #1





