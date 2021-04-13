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
