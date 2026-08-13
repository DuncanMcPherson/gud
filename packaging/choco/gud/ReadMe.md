# gud (Chocolatey Package)

Chocolatey packaging for [gud](https://github.com/DuncanMcPherson/gud), a
git-inspired version control system.

## What this package does

Downloads the official `gud_<version>.msi` installer from the project's
GitHub Releases and installs it silently via `msiexec /quiet /norestart`.
No installer files are embedded in the package — see `tools/VERIFICATION.txt`
for how to independently verify the downloaded binary.

## Updating this package for a new release

1. Update `<version>` in `gud.nuspec`.
2. Update `$url` and `checksum` in `tools/chocolateyinstall.ps1` to point
   at the new release's MSI and its SHA256 hash.
3. Update the version references in `tools/VERIFICATION.txt`.
4. Run `choco pack` from this directory.
5. Test locally with `choco install gud -s . -y` before pushing.
6. `choco push gud.<version>.nupkg --source https://push.chocolatey.org/`

## Notes

- `tools/chocolateyuninstall.ps1` and `tools/chocolateybeforemodify.ps1`
  are currently no-ops — Chocolatey's AutoUninstaller handles removal via
  the MSI's registered uninstall string. Revisit both once `gud` writes
  a global config file outside MSI-tracked state.
- Architecture: x86 only (matches the current MSI build).