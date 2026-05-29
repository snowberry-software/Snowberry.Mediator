using Microsoft.Extensions.DependencyInjection;
using Sample.Handlers;
using Snowberry.Mediator.Abstractions;

var services = new ServiceCollection();

// Generated, reflection-free registration. No options, no assembly scanning.
services.AddSnowberryMediator();

using var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();

var sum = await mediator.SendAsync<AddNumbers, int>(new AddNumbers(40, 2));
Console.WriteLine($"AddNumbers(40, 2) => {sum}");

await mediator.PublishAsync(new ItemCreated("widget"));

Console.WriteLine("Snowberry.Mediator source-generated registration ran successfully.");
