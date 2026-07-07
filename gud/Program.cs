using Spectre.Console.Cli;
using gud.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("gud");

    config.AddCommand<LogCommand>("log");
    config.AddCommand<CommitCommand>("commit");
});

return await app.RunAsync(args);