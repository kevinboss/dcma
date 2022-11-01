﻿using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using port;
using port.Commands.Commit;
using port.Commands.Export;
using port.Commands.Import;
using port.Commands.List;
using port.Commands.Orphan;
using port.Commands.Prune;
using port.Commands.Pull;
using port.Commands.Remove;
using port.Commands.Reset;
using port.Commands.Run;
using port.Commands.Stop;
using port.Config;
using port.Infrastructure;
using Spectre.Console.Cli;

var registrations = new ServiceCollection();
registrations.AddSingleton<IAllImagesQuery, AllImagesQuery>();
registrations.AddSingleton<IGetDigestsByIdQuery, GetDigestsByIdQuery>();
registrations.AddSingleton<IImageIdentifierPrompt, ImageIdentifierPrompt>();
registrations.AddSingleton<IContainerNamePrompt, ContainerNamePrompt>();
registrations.AddSingleton<ICreateImageCommand, CreateImageCommand>();
registrations.AddSingleton<ICreateImageFromContainerCommand, CreateImageFromContainerCommand>();
registrations.AddSingleton<IGetImageQuery, GetImageQuery>();
registrations.AddSingleton<IGetImageIdQuery, GetImageIdQuery>();
registrations.AddSingleton<IDoesImageExistQuery, DoesImageExistQuery>();
registrations.AddSingleton<IGetContainersQuery, GetContainersQuery>();
registrations.AddSingleton<IGetRunningContainersQuery, GetRunningContainersQuery>();
registrations.AddSingleton<ICreateContainerCommand, CreateContainerCommand>();
registrations.AddSingleton<IRunContainerCommand, RunContainerCommand>();
registrations.AddSingleton<IStopContainerCommand, StopContainerCommand>();
registrations.AddSingleton<IStopAndRemoveContainerCommand, StopAndRemoveContainerCommand>();
registrations.AddSingleton<IRemoveImageCommand, RemoveImageCommand>();
registrations.AddSingleton<ICreateImageCliCommand, CreateImageCliCommand>();
registrations.AddSingleton<IImageIdentifierAndTagEvaluator, ImageIdentifierAndTagEvaluator>();
registrations.AddSingleton<IExportImageCommand, ExportImageCommand>();
registrations.AddSingleton<IProgressSubscriber, ProgressSubscriber>();
registrations.AddSingleton<IOrphanImageCommand, OrphanImageCommand>();
registrations.AddSingleton<IImportImageCommand, ImportImageCommand>();
registrations.AddSingleton(typeof(Config), _ => ConfigFactory.GetOrCreateConfig());
registrations.AddSingleton(typeof(IDockerClient), provider =>
{
    var config = provider.GetService<Config>();
    if (config?.DockerEndpoint == null)
        throw new InvalidOperationException("Docker endpoint has not been configured");
    var endpoint = new Uri(config.DockerEndpoint);
    return new DockerClientConfiguration(endpoint).CreateClient();
});

var registrar = new TypeRegistrar(registrations);

var app = new CommandApp(registrar);

app.Configure(appConfig =>
{
    appConfig.AddCommand<PullCliCommand>("pull")
        .WithAlias("p");
    appConfig.AddCommand<RunCliCommand>("run")
        .WithAlias("r");
    appConfig.AddCommand<ResetCliCommand>("reset")
        .WithAlias("rs");
    appConfig.AddCommand<CommitCliCommand>("commit")
        .WithAlias("c");
    appConfig.AddCommand<ListCliCommand>("list")
        .WithAlias("ls");
    appConfig.AddCommand<RemoveCliCommand>("remove")
        .WithAlias("rm");
    appConfig.AddCommand<PruneCliCommand>("prune")
        .WithAlias("pr");
    appConfig.AddCommand<StopCliCommand>("stop")
        .WithAlias("s");
});

return app.Run(args);