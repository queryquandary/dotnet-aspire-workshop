# Integration Testing with .NET Aspire

## Introduction

In this module, we will cover integration testing using `Aspire.Hosting.Testing` with `MSTest`. Integration testing is crucial for ensuring that different parts of your application work together as expected. We will create a separate test project to test both the API and the web application. Additionally, we will explain the use of Playwright for end-to-end testing.

## Difference Between Unit Testing and Integration Testing

Unit testing focuses on testing individual components or units of code in isolation. It ensures that each unit functions correctly on its own. In contrast, integration testing verifies that different components of the application work together as expected. It tests the interactions between various parts of the system, such as APIs, databases, and web applications.

In the context of distributed applications with .NET Aspire, integration testing is essential to ensure that the different services and components communicate and function correctly together.

## Creating the Integration Test Project

1. Create a new test project named `IntegrationTests` in the `complete` folder.
2. Add references to the `Aspire.Hosting.Testing` and `MSTest` packages in the `IntegrationTests.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Testing" Version="9.1.0" />
    <PackageReference Include="MSTest.TestAdapter" Version="2.2.8" />
    <PackageReference Include="MSTest.TestFramework" Version="2.2.8" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Api\Api.csproj" />
    <ProjectReference Include="..\MyWeatherHub\MyWeatherHub.csproj" />
  </ItemGroup>

</Project>
```

3. Create a test class for integration tests in the `IntegrationTests.cs` file:

```csharp
using Aspire.Hosting.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http.Json;

namespace IntegrationTests
{
    [TestClass]
    public class IntegrationTests
    {
        private static AspireTestHost _host;
        private static HttpClient _client;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _host = new AspireTestHost();
            _client = _host.CreateClient();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _client.Dispose();
            _host.Dispose();
        }

        [TestMethod]
        public async Task TestApiGetZones()
        {
            var response = await _client.GetAsync("/zones");
            response.EnsureSuccessStatusCode();

            var zones = await response.Content.ReadFromJsonAsync<Zone[]>();
            Assert.IsNotNull(zones);
            Assert.IsTrue(zones.Length > 0);
        }

        [TestMethod]
        public async Task TestWebAppHomePage()
        {
            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(content.Contains("MyWeatherHub"));
        }
    }

    public record Zone(string Key, string Name, string State);
}
```

## Running the Integration Tests

1. Open a terminal and navigate to the `complete` folder.
2. Run the integration tests using the `dotnet test` command:

```bash
dotnet test IntegrationTests/IntegrationTests.csproj
```

The tests will run and verify that the API and web application are functioning correctly together.

## Playwright for End-to-End Testing

Playwright is a powerful tool for end-to-end testing. It allows you to automate browser interactions and verify that your application works as expected from the user's perspective. Playwright supports multiple browsers, including Chromium, Firefox, and WebKit.

### Use Case

Playwright can be used to perform end-to-end testing of your web application. It can simulate user interactions, such as clicking buttons, filling out forms, and navigating between pages. This ensures that your application behaves correctly in real-world scenarios.

### High-Level Concepts

- **Browser Automation**: Playwright can launch and control browsers to perform automated tests.
- **Cross-Browser Testing**: Playwright supports testing across different browsers to ensure compatibility.
- **Headless Mode**: Playwright can run tests in headless mode, which means the browser runs in the background without a graphical user interface.
- **Assertions**: Playwright provides built-in assertions to verify that elements are present, visible, and have the expected properties.

For more information on Playwright, refer to the [official documentation](https://playwright.dev/dotnet/).

## Conclusion

In this module, we covered integration testing using `Aspire.Hosting.Testing` with `MSTest`. We created a separate test project to test both the API and the web application. Additionally, we explained the use of Playwright for end-to-end testing. Integration testing is essential for ensuring that different parts of your application work together as expected, and Playwright provides a powerful tool for end-to-end testing.

**Next**: [Module #10: Advanced Topics](10-advanced-topics.md)
