# 🔌 NOCAPI Plugin Template

This project serves as a **template** for creating modular plugins that extend a host REST API. Each plugin is a self-contained .NET class library that can be dynamically loaded by the host application. This approach allows for scalable, maintainable, and decoupled API development.


## 📦 Project Structure

Each plugin consists of:

- `plugin.json`: Metadata describing the plugin.
- `config.json`: Plugin-specific configuration.
- `*.cs` files: Controllers or other logic.

You will need to install the plugin config wrapper from Gitea to access the functions inside this.

---

## 📝 Example Files

### `plugin.json`

```json
{
  "name": "NOCAPI.Modules.Users",
  "version": "1.0.0",
  "description": "Provides a sample for demonstration purposes.",
  "author": "Ciaran Reddington",
  "assembly": "NOCAPI.Modules.Users"
}
```

### `config.json`

```json
{
  "SomeSettingKey": "Hello from Users plugin!",
  "EnableFeatureX": true
}
```

### `UsersController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var setting = PluginConfigWrapper.Get("SomeSettingKey");
        return Ok($"Plugin loaded! Setting: {setting}");
    }
}
```
---

## 🛠️ How to Use This Template

### 1. Create a New Plugin

- Create a new repo, selecting this repo as the template.
- Rename the namespace and files to match your plugin name.
- Update `plugin.json` and `config.json` accordingly.

### 2. Implement Your API Logic

- Add controllers and services as needed.
- Use `PluginConfigWrapper.Get("key")` to access values from `config.json`.

### 3. Build the Plugin

Ensure your `.csproj` is configured like so:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Library</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore" Version="2.3.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Core" Version="2.3.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="9.0.8" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.8" />
    <PackageReference Include="System.Formats.Asn1" Version="9.0.8" />
  </ItemGroup>
  <ItemGroup>
    <None Update="plugin.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="config.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

### 4. Deploy the Plugin

- Compile the plugin.
- Place the output (DLL + `plugin.json` + `config.json` and any other required files) into a subfolder named after the plugin inside the host API's `/Plugins` directory.
- Restart the host API to load the new plugin.

### 5. Source Control

It is important to add the files here to source control and push changes so others can work on the project if required.
Please make sure to add config.json to your gitignore so important things like keys are not included in any pushes. The config file is kept in this repo for demonstration purposes only.

---

## 🔐 Authentication

Authentication is **key-based** and managed by the host application. To access plugin endpoints:

- You must include a valid **Bearer token** in the `Authorization` header.
- Tokens are issued by the host API and typically require set up beforehand.

Example header:

```
Authorization: Bearer <your-token>
```

---


## 🧠 Plugin Configuration Access

Use the `PluginConfigWrapper` to access plugin-specific settings:

```csharp
var value = PluginConfigWrapper.Get("SomeSettingKey");
```

This reads from the plugin's `config.json` located in its `/Plugins/{PluginName}` folder.


---


## 👨‍💻 Author

**Ciaran Reddington**  
Senior NOC Analyst
# Zdx-Grafana
