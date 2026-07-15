# gud

`gud` is a simple, lightweight version control system (VCS) written in .NET. It draws inspiration from Git, providing a distributed model for tracking changes in your source code.

## Features

- **Distributed VCS**: Every developer has a full copy of the repository history.
- **Git-like Architecture**: Uses Blobs, Trees, and Commits to store data.
- **Fast-Forward Merges**: Support for simple branch management and pushes.
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
```

### Viewing History

```bash
gud log
```

## Hosting a Server

The `gud.Server` project provides a simple way to host repositories remotely.

1. Configure the `ReposRoot` in `appsettings.json` or via environment variables.
2. Run the server: `dotnet run --project gud.Server`

Repositories are secured using an API Key middleware.

## Architecture

`gud` stores data in a `.gud` directory within your repository:
- `objects/`: Content-addressed storage for blobs, trees, and commits.
- `refs/`: Pointers to branch tips (heads) and the current HEAD.
- `config`: Repository-specific configuration.

## Contributing

Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on how to contribute to this project.

## License

`gud` is licensed under the [MIT License](LICENSE).
