using EMSuite.DataAccess;
using EMSuite.PhoneNotification.BackgroundServices;
using EMSuite.PhoneNotification.Services;
using Microsoft.Extensions.Localization;
using Serilog;
using Serilog.Events;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var connection = configuration["DatabaseConnection:DefaultConnection"];

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.MSSqlServer(
        connectionString: connection,
        tableName: "Logs",
        autoCreateSqlTable: true)
    .CreateLogger();

builder.Host.UseSerilog();


// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddHttpClient();
builder.Services.AddSingleton<IDataAccess>(sp => new SqlDataAccess(connection));
builder.Services.AddSingleton<IAzureSpeechService, AzureSpeechService>();
builder.Services.AddSingleton<IAzureBlobStorageService, AzureBlobStorageService>();
builder.Services.AddSingleton<ITwillioService, TwillioService>();
builder.Services.AddSingleton<ISignalRClient, SignalRClient>();
builder.Services.AddSingleton<IPhoneProcessor, PhoneProcessor>();
builder.Services.AddSingleton<ITranslatorService, TranslatorService>();
builder.Services.AddSingleton<IDelayProvider, DelayProvider>();
builder.Services.AddSingleton<ISpeechSynthesizer, SpeechSynthesizerHandler>();
builder.Services.AddSingleton<IMemoryStreamHandler, MemoryStreamHandler>();
builder.Services.AddSingleton<ITwillioCallHandler, TwillioCallHandler>();
builder.Services.AddSingleton<INotificationLogService, NotificationLogService>();
builder.Services.AddHostedService<FailedBatchProcessingService>();

builder.Services.AddSingleton<IBlobServiceClient>(x => 
new BlobServiceClientWrapper(configuration["BlobStorage:AzureBlobConnectionString"]));



builder.Services.AddSingleton<IAzureCognitiveVoiceProvider, AzureCogntiveVoiceProvider>();
builder.Services.AddHostedService<SignalRBackroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
