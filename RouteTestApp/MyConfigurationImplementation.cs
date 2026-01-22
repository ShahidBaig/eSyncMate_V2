using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

internal class MyConfigurationImplementation : IConfiguration
{
    private readonly IConfiguration _configuration;

    public MyConfigurationImplementation()
    {
        // Build configuration from appsettings.json - use exe directory, not current directory
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

        Console.WriteLine($"[DEBUG] MyConfigurationImplementation - Exe Directory: {exeDirectory}");
        Console.WriteLine($"[DEBUG] MyConfigurationImplementation - Current Directory: {Directory.GetCurrentDirectory()}");

        var builder = new ConfigurationBuilder()
            .SetBasePath(exeDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        _configuration = builder.Build();

        // Debug: Check if UseExternalProcess is loaded
        var useExternal = _configuration.GetValue<bool>("RouteEngine:UseExternalProcess");
        Console.WriteLine($"[DEBUG] MyConfigurationImplementation - UseExternalProcess: {useExternal}");
    }

    public string this[string key]
    {
        get => _configuration[key];
        set => throw new NotSupportedException("Setting configuration values is not supported.");
    }

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return _configuration.GetChildren();
    }

    public IChangeToken GetReloadToken()
    {
        return _configuration.GetReloadToken();
    }

    public IConfigurationSection GetSection(string key)
    {
        return _configuration.GetSection(key);
    }
}
