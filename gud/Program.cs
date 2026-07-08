using Spectre.Console.Cli;
using gud.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("gud");

    config.AddCommand<LogCommand>("log")
        .WithDescription("Shows the commit logs");
    config.AddCommand<CommitCommand>("commit")
        .WithDescription("Creates a new commit");
    config.AddCommand<InitCommand>("init")
        .WithDescription("Initializes a new repository");
    config.AddCommand<ConfigCommand>("config")
        .WithDescription("Manages configuration settings");
    config.AddCommand<BranchCommand>("branch")
        .WithDescription("Manages branches");
});

return await app.RunAsync(args);