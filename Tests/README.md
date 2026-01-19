# CSMOO Testing Guide

This directory contains the unit tests for CSMOO, demonstrating how to use Dependency Injection (DI) and mocking for testing.

## Setup

The test project uses:
- **xUnit** - Testing framework
- **Moq** - Mocking framework for creating test doubles
- **Microsoft.Extensions.DependencyInjection** - DI container (same as main project)

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests for a specific class
dotnet test --filter "FullyQualifiedName~ConfigTests"
```

## Test Structure

### TestHelpers/ServiceProviderHelper.cs

This helper class provides utilities for setting up DI containers in tests:

- `CreateServiceProvider()` - Creates a service provider with real implementations
- `CreateServiceProviderWithMocks()` - Creates a service provider with mocked dependencies

### Example Test Files

- **Configuration/ConfigTests.cs** - Tests for `IConfig` interface
- **Logging/LoggerTests.cs** - Tests for `ILogger` interface
- **Database/DbProviderTests.cs** - Tests for `IDbProvider` interface

## Writing Tests with DI

### Example 1: Testing with Real Dependencies

```csharp
[Fact]
public void MyService_Can_Be_Resolved_From_DI()
{
    // Arrange
    var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
    
    // Act
    var service = serviceProvider.GetRequiredService<IMyService>();
    
    // Assert
    Assert.NotNull(service);
}
```

### Example 2: Testing with Mocked Dependencies

```csharp
[Fact]
public void MyService_Uses_Logger_Correctly()
{
    // Arrange
    var mockLogger = new Mock<ILogger>();
    var serviceProvider = ServiceProviderHelper.CreateServiceProviderWithMocks(
        mockLogger: mockLogger);
    
    var service = serviceProvider.GetRequiredService<IMyService>();
    
    // Act
    service.DoSomething();
    
    // Assert
    mockLogger.Verify(l => l.Info("Something happened"), Times.Once);
}
```

### Example 3: Testing Singleton Behavior

```csharp
[Fact]
public void Service_Is_Singleton_In_DI()
{
    // Arrange
    var serviceProvider = ServiceProviderHelper.CreateServiceProvider();
    
    // Act
    var service1 = serviceProvider.GetRequiredService<IMyService>();
    var service2 = serviceProvider.GetRequiredService<IMyService>();
    
    // Assert
    Assert.Same(service1, service2);
}
```

## Best Practices

1. **Use mocks for external dependencies** - Mock database, file system, network calls
2. **Use real implementations for simple services** - If a service has no external dependencies, use the real implementation
3. **Test one thing at a time** - Each test should verify one specific behavior
4. **Use descriptive test names** - Test names should clearly describe what they're testing
5. **Arrange-Act-Assert pattern** - Structure tests with clear sections

## Adding New Tests

1. Create a test class in the appropriate namespace folder (e.g., `Tests/MyNamespace/MyClassTests.cs`)
2. Use the `[Fact]` attribute for each test method
3. Use `ServiceProviderHelper` to set up DI containers
4. Use `Moq` to create mocks when needed

## Current Test Coverage

- ✅ Config/IConfig interface
- ✅ Logger/ILogger interface  
- ✅ DbProvider/IDbProvider interface
- ✅ DI container setup and resolution
- ✅ Singleton behavior verification
- ✅ Mocking capabilities

## Next Steps

As more services are migrated to use DI, add corresponding tests:
- ObjectManager tests
- PlayerManager tests
- ScriptEngine tests
- CommandProcessor tests
