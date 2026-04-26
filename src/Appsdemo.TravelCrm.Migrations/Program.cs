using Appsdemo.TravelCrm.Migrations;

if (args.Length < 2)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  Migrations master \"Host=...;Database=appsdemo_master;Username=...;Password=...\"");
    Console.WriteLine("  Migrations tenant \"Host=...;Database=appsdemo_acme;Username=...;Password=...\"");
    return 1;
}

var mode = args[0].ToLowerInvariant();
var connectionString = args[1];

var result = mode switch
{
    "master" => MigrationRunner.RunMaster(connectionString),
    "tenant" => MigrationRunner.RunTenant(connectionString),
    _ => null
};

if (result is null)
{
    Console.WriteLine($"Unknown mode '{mode}'. Use 'master' or 'tenant'.");
    return 1;
}

if (!result.Success)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Migration failed: {result.Error}");
    Console.ResetColor();
    return 2;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Migration succeeded. Applied {result.AppliedScripts.Length} script(s).");
Console.ResetColor();
return 0;
