# Contributing to gud

Thank you for your interest in contributing to `gud`! We welcome contributions from everyone.

## Code of Conduct

Please be respectful and professional in all your interactions with the project and its community.

## How Can I Contribute?

### Reporting Bugs

- Search the existing issues to see if the bug has already been reported.
- If not, create a new issue. Include a clear title, a detailed description, and steps to reproduce the issue.

### Suggesting Enhancements

- Search the existing issues to see if the enhancement has already been suggested.
- If not, create a new issue. Describe the proposed enhancement and why it would be useful.

### Pull Requests

1. Fork the repository and create your branch from `master`.
2. If you've added code that should be tested, add tests.
3. Ensure the test suite passes.
4. Make sure your code follows the existing style of the codebase.
5. Submit a pull request.

## Development Setup

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (for semantic-release)

### Building the Project

To build the solution, run:

```bash
dotnet build
```

### Running Tests

To run the tests, use:

```bash
dotnet test
```

### Project Structure

- `gud`: The CLI application.
- `gud.Server`: The server component for hosting remote repositories.
- `gud.Core`: Shared library containing core models and logic.
- `gud.Tests`: Unit and integration tests.

## Versioning

This project uses [Semantic Release](https://github.com/semantic-release/semantic-release) for automated versioning and releases. Please follow the [Conventional Commits](https://www.conventionalcommits.org/) specification for your commit messages.
