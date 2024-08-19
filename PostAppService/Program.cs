using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using PostAppService.Data;
using PostAppService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<PostAppServiceContext>(o =>
    o.UseSqlite(@"Data Source=user.db"));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PostAppServiceContext>();
        dbContext.Database.EnsureCreated();
    }
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

ListenForIntegrationEvents(app);

app.Run();

static void ListenForIntegrationEvents(IHost host)
{
    var factory = new ConnectionFactory();
    var connection = factory.CreateConnection();
    var channel = connection.CreateModel();
    var consumer = new EventingBasicConsumer(channel);

    consumer.Received += (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine(" [x] Received {0}", message);
        var data = JObject.Parse(message);
        var type = ea.RoutingKey;

        using var localScope = host.Services.CreateScope();
        var localDbContext = localScope.ServiceProvider.GetRequiredService<PostAppServiceContext>();

        if (type == "user.add")
        {
            localDbContext.Users.Add(new User()
            {
                ID = data["id"].Value<int>(),
                Name = data["name"].Value<string>()
            });
            localDbContext.SaveChanges();
        }
        else if (type == "user.update")
        {
            var user = localDbContext.Users.First(a => a.ID == data["id"].Value<int>());
            user.Name = data["newname"].Value<string>();
            localDbContext.SaveChanges();
        }
    };

    channel.BasicConsume(queue: "user.postservice",
                         autoAck: true,
                         consumer: consumer);
}