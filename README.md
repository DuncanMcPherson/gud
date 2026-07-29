# gud

`gud` is a simple, lightweight version control system (VCS) written in .NET. It draws inspiration from Git, providing a distributed model for tracking changes in your source code.

## Features

- **Distributed VCS**: Every developer has a full copy of the repository history.
- **Git-like Architecture**: Uses Blobs, Trees, and Commits to store data.
- **Merges**: Fast-forward when possible; three-way merges with conflict markers when histories diverge.
- **Server Component**: A lightweight Web API for hosting remote repositories.
- **Cross-Platform**: Built on .NET 10, running on Windows and Linux.

## Installation

### Windows

You can build the installer from source using the Wix Toolset:

1. Restore dependencies: `dotnet restore`
2. Build the project: `dotnet build -c Release`
3. Build the MSI: `wix build installer/Product.wxs -d Version=1.0.0 -d PublishDir=./publish/win-x64 -ext WixToolset.UI.wixext -o gud.msi`

Alternatively, download the latest `.msi` from the [Releases](https://github.com/DuncanMcPherson/gud/releases) page.

### Linux (Debian/Ubuntu)

1. Download the latest `.deb` package from the [Releases](https://github.com/DuncanMcPherson/gud/releases) page.
2. Install using dpkg: `sudo dpkg -i gud.deb`

### From Source

Ensure you have the .NET 10 SDK installed.

```bash
git clone https://github.com/DuncanMcPherson/gud.git
cd gud
dotnet build
```

## Quick Start

### Version

```bash
gud --version
gud version
```

### Initialize a Repository

```bash
gud init
```

### Configure Your Identity

```bash
gud config user.name "Your Name"
```

### Create a Commit

`gud` currently commits the entire working directory.

```bash
gud commit -m "My first commit"
```

### Branching

```bash
gud branch my-feature
gud checkout my-feature
gud branch -d my-feature   # delete a branch you are not on
```

Deleting a branch removes its ref under `.gud/refs/heads/`. Commits and objects remain in the object store (garbage collection is not implemented yet).

### Merging

```bash
gud checkout main
gud merge my-feature
```

When histories have diverged without overlapping path edits, `gud` creates a merge commit. Changes on different lines of the same text file are auto-merged; only overlapping line regions get conflict markers. Fix markers and run `gud commit`, or discard the merge with `gud merge --abort`.

### Pull

```bash
gud pull              # fetch default remote + current branch, then merge
gud pull origin -b main
```

Pull fetches the remote tip, then merges it into the current branch. If the remote tip matches the last fetched tracking ref and objects are already local, no object download is performed.


### Viewing History

```bash
gud log
```

## Architecture

`gud` stores data in a `.gud` directory within your repository:
- `objects/`: Content-addressed storage for blobs, trees, and commits.
- `refs/`: Pointers to branch tips (heads) and the current HEAD.
- `config`: Repository-specific configuration.

## Contributing

Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on how to contribute to this project.

## License

`gud` is licensed under the [MIT License](LICENSE).
