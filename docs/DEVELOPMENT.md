# HR Management - Development Guide

## 1. Project Architecture

The solution follows a layered architecture:

- `HrManagement.Domain`
  - Core business entities and domain rules.
  - Must not depend on Desktop or Infrastructure.

- `HrManagement.Application`
  - Application use cases, interfaces, and business workflows.
  - May depend on Domain.

- `HrManagement.Infrastructure`
  - Infrastructure implementations such as persistence and external services.
  - Implements interfaces defined by the Application layer.

- `HrManagement.Desktop`
  - WPF presentation layer.
  - Uses MVVM.
  - Contains Views, ViewModels, navigation, themes, and desktop-specific services.

- `HrManagement.Tests`
  - Automated tests for the solution.

## 2. UI Architecture

The desktop application uses:

- WPF
- MVVM
- CommunityToolkit.Mvvm
- Dependency Injection
- Navigation through `INavigationService`

Views should not contain business logic.

ViewModels should not directly manipulate UI controls.

## 3. Dependency Injection

Services and ViewModels should be registered through the application DI container.

Avoid creating application services manually with `new` inside Views or ViewModels when the service can be injected.

## 4. Navigation

Application navigation is centralized.

Navigation items use `NavigationItem`.

Navigation between pages is performed through `INavigationService`.

Avoid creating separate navigation commands for every sidebar item.

## 5. Theme System

Theme resources are stored under:

`HrManagement.Desktop/Themes`

Theme presets are stored under:

`HrManagement.Desktop/Themes/Presets`

UI colors should use resource keys instead of hard-coded colors whenever practical.

Runtime theme changes are handled through `IThemeService`.

Customer-specific branding should be implemented through configuration/theme resources rather than duplicating the entire application.

## 6. NuGet Packages

Package versions are managed centrally in:

`Directory.Packages.props`

Do not add package versions directly to individual `.csproj` files.

Adding or upgrading a package should be treated as a separate intentional change and verified with Build and Tests.

## 7. Git Workflow

Development work should be performed on feature branches.

Example:

`feature/login-and-dashboard`

Before committing:

1. Build the solution.
2. Run all tests.
3. Check `git status`.
4. Ensure no temporary/generated files are included.

Commit messages should be short and descriptive.

Examples:

`feat: add runtime theme system`

`fix: correct theme resource replacement`

`test: add authentication service tests`

`chore: standardize project configuration`

## 8. Testing

Before a development milestone is considered complete:

- Solution must build successfully.
- Existing automated tests must pass.
- New business logic should receive tests where practical.
- Important WPF interactions should also be manually verified when automated UI testing is not appropriate.

## 9. Customization Strategy

The product is designed to support different customer requirements.

Prefer:

- Configurable modules
- Feature flags
- Theme presets
- Branding configuration
- Dependency injection
- Replaceable service implementations

Avoid creating a separate copy of the entire source code for every customer unless there is a strong technical or contractual reason.

## 10. AI Features

AI capabilities may be introduced as an optional module in the future.

Potential use cases include:

- HR data search
- Employee profile summaries
- Leave information queries
- Email drafting
- Software usage assistance

AI features must not be tightly coupled to the core HR domain.

The core HR application must remain functional without the AI module.