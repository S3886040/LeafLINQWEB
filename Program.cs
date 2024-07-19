using Azure.Storage.Blobs;
using LeafLINQWeb.Handler;
using LeafLINQWeb.Middleware;
using LeafLINQWeb.Models;
using LeafLINQWeb.Scheduler;
using LeafLINQWeb.Utilities;
using System.Net.Http.Headers;
using System.Net.Mime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new BlobServiceClient(builder.Configuration.GetConnectionString("AzureStorageConnectionString")));
builder.Services.AddSingleton(new PseudoSession());

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<WebAPICustomHandler>();
builder.Services.AddScoped<PlantAPICustomHandler>();

// Job scheduler.
builder.Services.AddHostedService<PlantAlertsScheduler>();

var containerName = builder.Configuration["BlobStorage:ContainerName"];
builder.Services.AddScoped<BlobStorageService>(sp =>
    new BlobStorageService(sp.GetRequiredService<BlobServiceClient>(), containerName));

// Api client for api calls requiring no bearer token. e.g. login
builder.Services.AddHttpClient("auth", client =>
{
    client.BaseAddress = new Uri("https://leaflinqwebappapi.azurewebsites.net");

    //**********************************************************************
    // Used when testing the api locally in conjunction with "api"
    //client.BaseAddress = new Uri("https://localhost:7063/api");
    //**********************************************************************

    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
});

// Api client for api calls requiring a bearer token
builder.Services.AddHttpClient("api", client =>
{

    client.BaseAddress = new Uri("https://leaflinqwebappapi.azurewebsites.net");

    //**********************************************************************
    // Used when testing the api locally in conjunction with "auth"
    //client.BaseAddress = new Uri("https://localhost:7063/api");
    //**********************************************************************

    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
}).AddHttpMessageHandler<WebAPICustomHandler>();

// Api client for api plant data from leaflinqapi
builder.Services.AddHttpClient("api-plant-data", client =>
{
    client.BaseAddress = new Uri("https://leaflinqapi.azurewebsites.net");
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
}).AddHttpMessageHandler<PlantAPICustomHandler>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Added with Razer.RuntimeCompilation 
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseTokenRefreshMiddleware();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();