# Gallio Test Automation Platform v4.0

Modern test automation platform for .NET, now with .NET 8 support!

## ?? Installation

```bash
dotnet add package Gallio
```

Or via Package Manager Console:
```powershell
Install-Package Gallio
```

## ?? Quick Start

### Initialize the Runtime

```csharp
using Gallio.Runtime;
using Gallio.Runtime.Logging;

// Initialize the Gallio runtime
var setup = new RuntimeSetup();
RuntimeBootstrap.Initialize(setup, new ConsoleLogger());

// Access runtime services
var testRunner = RuntimeAccessor.ServiceLocator.Resolve<ITestRunnerManager>();
```

### Run Tests Programmatically

```csharp
using Gallio.Runner;
using Gallio.Model;

var testPackage = new TestPackage();
testPackage.AddFile("MyTests.dll");

var testRunner = TestRunnerUtils.CreateTestRunner();
testRunner.Initialize(testPackage, null, null);

var result = testRunner.Run(null, null, null, null);
```

## ? Features

- ? **Multi-Framework Support** - .NET Framework 3.5+, .NET 4.8, and .NET 8
- ? **Extensible Plugin Architecture** - Build custom test frameworks and extensions
- ? **Advanced Test Reporting** - HTML, XML, and custom report formats
- ? **Test Isolation Options** - Local, AppDomain, and Process isolation
- ? **Cross-Platform Compatible** - Runs on Windows, Linux, and macOS (.NET 8)
- ? **Modern RPC Layer** - Async/await support for .NET 8

## ??? Architecture

Gallio provides the foundation for test automation:

- **Test Framework Infrastructure** - Host and run tests from any framework
- **Plugin System** - Extensible architecture for custom components
- **Test Runners** - Execute tests locally or in isolation
- **Report Generation** - Create comprehensive test reports
- **Service Locator** - Dependency injection and service resolution

## ?? Extensions

Gallio supports various extensions:

- **MbUnit** - Full-featured unit testing framework
- **NUnit** - NUnit test framework adapter
- **xUnit** - xUnit.net test framework adapter
- **AutoCAD** - Run tests in AutoCAD environment
- **Custom Frameworks** - Build your own test framework

## ?? Documentation

- [GitHub Repository](https://github.com/soelske/mbunit-v3)
- [Wiki Documentation](https://github.com/soelske/mbunit-v3/wiki)
- [API Reference](https://github.com/soelske/mbunit-v3/wiki/API-Reference)
- [Migration Guide]( https://github.com/soelske/mbunit-v3/wiki/Migration-Guide)

## ?? Migration from v3.x

Gallio v4.0 includes several breaking changes:

- .NET 8 support replaces .NET Remoting with custom RPC layer
- Async/await patterns throughout the API
- Updated package structure (Gallio.Core for .NET 8)

See the [Migration Guide](https://github.com/soelske/mbunit-v3/wiki/Migration-Guide) for details.

## ?? Contributing

Contributions are welcome! Please see our [Contributing Guide](https://github.com/soelske/mbunit-v3/blob/master/CONTRIBUTING.md).

## ?? License

Apache License 2.0 - see [LICENSE](https://github.com/soelske/mbunit-v3/blob/master/LICENSE.txt) for details.

## ?? Credits

- Original Gallio Project (2005-2010)
- Community contributors and maintainers
- .NET 8 port by Bart Suelze (2025)

## ?? Support

- **Issues**: [GitHub Issues](https://github.com/soelske/mbunit-v3/issues)
- **Discussions**: [GitHub Discussions](https://github.com/soelske/mbunit-v3/discussions)

---

**Copyright © 2005-2025 Gallio Project**
