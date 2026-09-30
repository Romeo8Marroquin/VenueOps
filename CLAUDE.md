# VenueOps — repository guidance

.NET MAUI 10 app for managing venues, events, bookings, and staff through an operations
dashboard. Targets Android, iOS, Mac Catalyst, and Windows.

**This repository is the frontend only.** It talks to a backend REST API that is not part
of this repository and must never be changed from here. The API contract lives in
`VenueOps/Models/**` (request and response shapes, JSON property names). Treat those names
as the backend's contract: `Models/ModelSerializationTests.cs` pins them. Some are
spelled differently on purpose because the backend spells them that way, for example
`expectedAtendees` on create and detail but `expectedAttendees` on update. Do not "fix" a
wire name unless the story explicitly says the backend changed.

---

## Structure

| Folder | Contents | Conventions |
| --- | --- | --- |
| `VenueOps/Models/` | API DTOs, by feature (`Events/`, `Dashboard/`, `Common/`) | Plain classes with `[JsonPropertyName]`; no logic beyond simple computed properties |
| `VenueOps/Services/` | One interface + one implementation per concern | HTTP services take `IHttpClientFactory` and use the named client `"VenueOpsApi"`; a non-success status returns `null`, auth failures throw `AuthException` |
| `VenueOps/ViewModels/` | One view model per view, plus `BaseViewModel` and `Common/PaginatedSection<T>` | CommunityToolkit.Mvvm: `[ObservableProperty]` on `partial` properties, `[RelayCommand]` for commands; dependencies injected through the constructor |
| `VenueOps/Views/` | XAML pages and popups, by feature | Bind to the view model; no logic in code-behind beyond wiring |
| `VenueOps/Converters/` | `IValueConverter`s for XAML | Pure functions of the input value |
| `VenueOps/Config/` | `ApiConfig` | The base URL comes from the gitignored `Config/ApiConfig.Local.cs` (template: `ApiConfig.Local.cs.example`). Never commit a real URL. |
| `VenueOps/MauiProgram.cs` | Dependency injection | Services singleton, view models and views transient. Register every new service, view model, and view here. |

**Navigation and dialogs go through `INavigationService` and `IDialogService`.** New code in
view models must not call `Shell.Current`, `Application.Current`, or `Launcher.Default`
directly. Those need a running app and cannot be unit tested. A few older view model methods
still do; that is known debt, not a pattern to copy.

---

## Tests

`VenueOps.Tests` (xUnit + Moq) **references the app project**. The app also builds for plain
`net10.0` for this purpose, and `InternalsVisibleTo` exposes internal types. There is no
list of linked files to maintain: new app code is testable as soon as it exists.

- Mirror the app's folders: `Tests/ViewModels/`, `Tests/Services/`, `Tests/Models/`,
  `Tests/Converters/`.
- Mock service interfaces with Moq. Test HTTP services through
  `TestDoubles/StubHttpMessageHandler` and `TestDoubles/FakeApi`, never the network.
- Culture-sensitive assertions (dates, numbers) set `CultureInfo.CurrentCulture` inside a
  `try`/`finally`.
- **Any new or changed logic in Models, Services, ViewModels, or Converters comes with unit
  tests** covering its branches, including error paths. Views, Platforms, App, AppShell, and
  MauiProgram are outside unit-test scope, and so are the Dialog and Navigation service
  wrappers.
- Never delete, skip, or weaken an existing test to make a change pass.
- Test data is fictional: invented names, `example.invalid` URLs.

Coverage scope and exclusions are declared, with a reason for each, in
`VenueOps.Tests/coverlet.runsettings`, which is applied automatically.

---

## Commands

```
dotnet build VenueOps/VenueOps.csproj -f net10.0-windows10.0.19041.0   # app, Windows (~1 min cold)
dotnet test  VenueOps.Tests/VenueOps.Tests.csproj                      # unit tests
dotnet test  VenueOps.Tests/VenueOps.Tests.csproj --collect:"XPlat Code Coverage"   # + coverage
```

Both builds must finish with 0 warnings and 0 errors, and the tests must be green.

**Windows path length.** Clone and build from a short path (for example `C:\src\VenueOps`).
Very deep paths make the Windows build fail in `MakePri` with "path not found" errors.

---

## Git

- Base branch: `dev`. Pull requests target `dev`.
- Branch names: `VNO-<nn>-<Short-Description>`, one branch per story.
- Never commit `Config/ApiConfig.Local.cs`, `.env` files, credentials, or tokens.
- The repository is public: everything in it, including issues and test data, is fictional.
