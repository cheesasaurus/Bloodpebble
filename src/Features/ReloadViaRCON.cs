
using Bloodpebble.ReloadRequesting;
using ScarletRCON.Shared;
using System.Linq;
using System.Threading.Tasks;

namespace Bloodpebble.Features;


internal class ReloadViaRCON : BaseReloadRequestor
{
    internal static ReloadViaRCON? Instance;

    internal static ReloadViaRCON Initialize()
    {
        Instance = new ReloadViaRCON();
        RconCommandRegistrar.RegisterAll();
        return Instance;
    }

    internal static void Uninitialize()
    {
        RconCommandRegistrar.UnregisterAssembly();
    }

    [RconCommandCategory("Server Administration")]
    public static class RconCommands
    {
        [RconCommand("reloadplugins", "Reloads all plugins in the BloodpebblePlugins folder")]
        public async static Task<string> ReloadAllAsync()
        {
            if (Instance is null)
            {
                return "Error: missing ReloadViaRCON instance";
            }
            var result = await Instance.RequestFullReloadAsync();
            var pluginNames = result.PluginsReloaded.Select(p => p.Metadata.Name);

            switch (result.Status)
            {
                case ReloadResultStatus.Success:
                    return $"Reloaded all plugins: {string.Join(", ", pluginNames)}";
                case ReloadResultStatus.PartialSuccess:
                    return $"Reloaded only some plugins: {string.Join(", ", pluginNames)}";
                default: // default to Faulted
                case ReloadResultStatus.Faulted:
                    return "Error: An exception occurred while attempting to reload. Check logs for details.";
            }
        }

        [RconCommand("reloadplugin", "Reloads a single plugin by its GUID", "reloadplugin <PluginGUID>")]
        public async static Task<string> ReloadOneAsync(string guid)
        {
            if (Instance is null)
            {
                return "Error: missing ReloadViaRCON instance";
            }
            if (string.IsNullOrWhiteSpace(guid))
            {
                return "Error: Plugin GUID not provided.";
            }

            var result = await Instance.RequestPartialReloadAsync([guid]);
            var otherPluginNames = result.PluginsReloaded.Where(p => p.Metadata.GUID != guid).Select(p => p.Metadata.Name);

            switch (result.Status)
            {
                case ReloadResultStatus.Success:
                    if (otherPluginNames.Any())
                    {
                        return $"Reloaded plugin \"{guid}\", along with other plugins: {string.Join(", ", otherPluginNames)}";
                    }
                    return $"Reloaded plugin \"{guid}\".";

                case ReloadResultStatus.PartialSuccess:
                    if (otherPluginNames.Any())
                    {
                        return $"Failed to reload plugin \"{guid}\", but reloaded other plugins: {string.Join(", ", otherPluginNames)}";
                    }
                    return $"Failed to reload plugin \"{guid}\".";

                default:
                case ReloadResultStatus.Faulted:
                    return "Error: An exception occurred while attempting to reload. Check logs for details.";
            }

        }
    }

}
