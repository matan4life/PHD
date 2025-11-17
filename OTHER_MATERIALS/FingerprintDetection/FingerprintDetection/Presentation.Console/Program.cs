// See https://aka.ms/new-console-template for more information

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Console.EventSourcing;
using Presentation.Console.Extensions;
using Presentation.Console.Features.GetClusterDescriptor;
using Presentation.Console.Features.GetMinutiae;
using Presentation.Console.Features.SeparateByClusters;
using Presentation.Console.Services;

var serviceProvider = new ServiceCollection()
    .RegisterServices()
    .BuildServiceProvider();

var sender = serviceProvider.GetRequiredService<ISender>();
var publisher = serviceProvider.GetRequiredService<IPublisher>();
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
string path = args.First();
string anotherPath = args.Last();
var response = await sender.Send(new GetMinutiaeQuery(path));
var otherResponse = await sender.Send(new GetMinutiaeQuery(anotherPath));
var clusters = await sender.Send(new SeparateByClustersCommand(response.Value!.Minutiae));
var otherClusters = await sender.Send(new SeparateByClustersCommand(otherResponse.Value!.Minutiae));
var descriptors = clusters.Clusters.Select(async cluster => await sender.Send(new GetClusterDescriptorQuery(cluster)));
var clusterDescriptors = await Task.WhenAll(descriptors);
var otherDescriptors = otherClusters.Clusters.Select(async cluster => await sender.Send(new GetClusterDescriptorQuery(cluster)));
var otherClusterDescriptors = await Task.WhenAll(otherDescriptors);
var matrix = new int[clusterDescriptors.Length, otherClusterDescriptors.Length];
var session = $"{Path.GetFileName(path)}_{Path.GetFileName(anotherPath)}";
if (Directory.Exists($@"D:\PHD\FingerprintDetection\Telemetry\{session}"))
{
    Directory.Delete($@"D:\PHD\FingerprintDetection\Telemetry\{session}", true);
}
for (var i = 0; i < clusterDescriptors.Length; i++)
{
    for (var j = 0; j < otherClusterDescriptors.Length; j++)
    {
        matrix[i, j] = new DescriptorComparator().Compare(session, clusterDescriptors[i].ClusterDescriptor, otherClusterDescriptors[j].ClusterDescriptor, i, j);
    }
}