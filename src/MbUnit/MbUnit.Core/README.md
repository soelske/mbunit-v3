# MbUnit Test Framework v4.0

Unit testing framework for .NET with rich assertions and data-driven testing, now with .NET 8-windows support.

## ?? Installation

```bash
dotnet add package MbUnit
```

Or via Package Manager Console:
```powershell
Install-Package MbUnit
```

**Note:** MbUnit automatically includes the Gallio platform as a dependency.

## ?? Quick Start

### Basic Test

```csharp
using MbUnit.Framework;

[TestFixture]
public class CalculatorTests
{
    [Test]
    public void Add_TwoNumbers_ReturnsSum()
    {
        // Arrange
        var calculator = new Calculator();

        // Act
        var result = calculator.Add(2, 3);

        // Assert
        Assert.AreEqual(5, result);
    }
}
```

### Data-Driven Tests

```csharp
[TestFixture]
public class MathTests
{
    [Test]
    [Row(1, 2, 3)]
    [Row(5, 5, 10)]
    [Row(-1, 1, 0)]
    [Row(0, 0, 0)]
    public void Add_WithDifferentInputs(int a, int b, int expected)
    {
        var calculator = new Calculator();
        Assert.AreEqual(expected, calculator.Add(a, b));
    }
}
```

### Rich Assertions

```csharp
[Test]
public void TestWithRichAssertions()
{
    var list = new List<int> { 1, 2, 3, 4, 5 };
    
    // Collection assertions
    Assert.Count(5, list);
    Assert.Contains(3, list);
    Assert.IsNotEmpty(list);
    
    // String assertions
    var text = "Hello World";
    Assert.StartsWith("Hello", text);
    Assert.EndsWith("World", text);
    Assert.Contains("lo Wo", text);
    
    // Numeric assertions
    Assert.GreaterThan(10, 5);
    Assert.Between(5, 1, 10);
    Assert.ApproximatelyEqual(3.14159, Math.PI, 0.00001);
}
```

### Exception Testing

```csharp
[Test]
[ExpectedException(typeof(DivideByZeroException))]
public void Divide_ByZero_ThrowsException()
{
    var calculator = new Calculator();
    calculator.Divide(10, 0);
}

[Test]
public void Divide_ByZero_ThrowsException_Alternative()
{
    var calculator = new Calculator();
    
    Assert.Throws<DivideByZeroException>(() => calculator.Divide(10, 0));
}
```

## ? Features

### Core Features
- ? **Rich Assertion Library** - Over 100 assertion methods
- ? **Data-Driven Testing** - `[Row]`, `[Column]`, `[CombinatorialJoin]` attributes
- ? **Parallel Execution** - Run tests in parallel for faster results
- ? **Test Decorators** - Apply behaviors to tests declaratively
- ? **Contract Verifiers** - Verify standard contracts (IEquatable, IComparable, etc.)

### Attribute Features
- ? **`[Test]`** - Mark methods as tests
- ? **`[TestFixture]`** - Group related tests
- ? **`[SetUp]` / `[TearDown]`** - Test initialization and cleanup
- ? **`[TestFixtureSetUp]` / `[TestFixtureTearDown]`** - Fixture-level setup
- ? **`[Ignore]`** - Skip tests temporarily
- ? **`[Category]`** - Organize tests by category
- ? **`[Timeout]`** - Set maximum test execution time

### Advanced Features
- ? **Theory-Based Testing** - Use `[Theory]` for parameterized tests
- ? **Custom Comparers** - Define custom equality and comparison logic
- ? **Test Patterns** - Build reusable test patterns
- ? **.NET 8 Support** - Modern async/await patterns

## ?? Data-Driven Testing Examples

### Multiple Data Sources

```csharp
[Test]
[Row(1, "one")]
[Row(2, "two")]
[Row(3, "three")]
public void TestWithMultipleRows(int number, string text)
{
    Assert.AreEqual(text, NumberToText(number));
}
```

### Combinatorial Testing

```csharp
[Test]
[CombinatorialJoin]
public void TestAllCombinations(
    [Column(1, 2, 3)] int x,
    [Column("a", "b")] string y)
{
    // Tests all 6 combinations: (1,a), (1,b), (2,a), (2,b), (3,a), (3,b)
    Console.WriteLine($"Testing: {x}, {y}");
}
```

### CSV Data

```csharp
[Test]
[CsvData("TestData.csv")]
public void TestFromCsv(string name, int age, string city)
{
    Assert.IsNotNull(name);
    Assert.GreaterThan(age, 0);
}
```

## ??? Framework Support

| Framework | Support |
|-----------|---------|
| .NET Framework 3.5 | ? Full support |
| .NET Framework 4.0 | ? Full support |
| .NET Framework 4.5+ | ? Full support |
| .NET Framework 4.8 | ? Full support |
| .NET 8 | windows support |
| .NET 9+ | ?? Planned |

## ?? Documentation

- [GitHub Repository](https://github.com/soelske/mbunit-v3)
- [Getting Started Guide](https://github.com/soelske/mbunit-v3/wiki/Getting-Started)
- [API Reference](https://github.com/soelske/mbunit-v3/wiki/API-Reference)
- [Assertion Reference](https://github.com/soelske/mbunit-v3/wiki/Assertions)
- [Data-Driven Testing Guide](https://github.com/soelske/mbunit-v3/wiki/Data-Driven-Testing)

## ?? Migration from v3.x

MbUnit v4.0 maintains backward compatibility with most v3.x features:

- ? All assertion methods preserved
- ? All test attributes work the same
- ? Data-driven testing unchanged
- ?? Some obsolete methods removed
- ?? Async test support improved

See the [Migration Guide](https://github.com/soelske/mbunit-v3/wiki/Migration-Guide) for details.

## ?? Comparison with Other Frameworks

| Feature | MbUnit | NUnit | xUnit |
|---------|--------|-------|-------|
| Rich Assertions | ? 100+ | ?? 50+ | ?? 30+ |
| Data-Driven Testing | ? Built-in | ?? Limited | ? Good |
| Parallel Execution | ? Yes | ? Yes | ? Yes |
| Contract Verifiers | ? Yes | ? No | ? No |
| .NET 8 Support | ? Yes | ? Yes | ? Yes |

## ?? Contributing

Contributions are welcome! Please see our [Contributing Guide](https://github.com/soelske/mbunit-v3/blob/master/CONTRIBUTING.md).

## ?? License

Apache License 2.0 - see [LICENSE](https://github.com/soelske/mbunit-v3/blob/master/LICENSE.txt) for details.

## ?? Credits

- Original MbUnit Project (2000-2010)
- Jeff Brown and the Gallio team
- Community contributors
- .NET 8 port by Bart Suelze (2025)

## ?? Support

- **Issues**: [GitHub Issues](https://github.com/soelske/mbunit-v3/issues)
- **Discussions**: [GitHub Discussions](https://github.com/soelske/mbunit-v3/discussions)
- **Stack Overflow**: Tag your questions with `mbunit`

---

**Copyright © 2000-2025 MbUnit Project / Gallio Project**
