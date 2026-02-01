# Contributing to DBC-Localizer

Thank you for your interest in contributing to DBC-Localizer!

## Development Setup

### Prerequisites
- .NET 9.0 SDK or higher
- Visual Studio 2024 or Visual Studio Code
- Git

### Clone and Build
```bash
git clone https://github.com/NeonOby/dbc-localizer.git
cd dbc-localizer
cd dbc-localizer
dotnet build
```

### Run Tests
```bash
dotnet run -- --help
dotnet run -- scan-mpq --patch <path> --locale-mpq <path> --defs dbcd-lib/definitions/definitions
```

## Code Style

- Follow C# naming conventions (PascalCase for public members)
- Use meaningful variable names
- Add XML documentation comments for public API
- Keep methods focused and testable
- Use LINQ where appropriate but maintain readability

## Project Organization

- **CommandArgs.cs** - Argument parsing and validation
- ***Handler.cs** - Command-specific logic
- **MergeEngine.cs** - Core business logic
- **Helpers.cs** - Utility functions
- **Models.cs** - Data structures

## Making Changes

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Make your changes
3. Test thoroughly: `dotnet build && dotnet run -- ...`
4. Commit with clear messages: `git commit -m "Add feature: description"`
5. Push to your fork and create a Pull Request

## Reporting Issues

Include:
- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Your environment (.NET version, OS, etc.)
- Relevant file output/logs

## Pull Request Process

1. Update README.md if needed
2. Add comments to complex logic
3. Ensure your code compiles without warnings
4. Test with sample data
5. Provide a clear description of changes

## Areas for Contribution

- Bug fixes
- Performance improvements
- Additional locale support
- Better error messages
- Test coverage
- Documentation

## Questions?

Feel free to open an issue for questions or discussions.

Thank you for contributing!
