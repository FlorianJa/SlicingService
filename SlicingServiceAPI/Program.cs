using LettuceEncrypt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.IdentityModel.Logging;
using SlicerCLIWrapper;
using SlicingServiceAPI;

const string _roleClaimType = "role";

IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("JwtBearer").Get<SlicingServiceAPI.JwtBearerOptions>();


var basePath = "data";
var slicerPath = builder.Configuration.GetValue<string>("Slicer:Path");
var modelDownloadPath = Path.Combine(basePath, "models");
var gCodePath = Path.Combine(basePath, "gCode");
var slicingConfigPath = Path.Combine(basePath, "profiles");

if (!Directory.Exists(gCodePath))
{
    Directory.CreateDirectory(gCodePath);
}

if (!Directory.Exists(modelDownloadPath))
{
    Directory.CreateDirectory(modelDownloadPath);
}

if (!Directory.Exists(slicingConfigPath))
{
    Directory.CreateDirectory(slicingConfigPath);
}


if (builder.Environment.IsProduction())
{
    var certPassword = builder.Configuration.GetValue<string>("CertPassword");

    builder.Services.AddLettuceEncrypt()
        .PersistDataToDirectory(new DirectoryInfo(Path.Combine(basePath, "certs")), certPassword);
}

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<SlicingService>(s => new SlicingService(modelDownloadPath, gCodePath, slicingConfigPath, new Slicer(slicerPath)));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.Authority = jwtOptions.Authority;
    options.Audience = jwtOptions.Audience;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters.NameClaimType = "preferred_username";
    options.TokenValidationParameters.ValidateAudience = true;
    options.Events = new JwtBearerEvents()
    {
        OnAuthenticationFailed = c =>
        {
            c.NoResult();
            c.Response.StatusCode = 500;
            c.Response.ContentType = "text/plain";
            c.Response.WriteAsync(c.Exception.ToString()).Wait();
            return Task.CompletedTask;
        }//,
        //OnChallenge = c =>
        //{
        //    c.HandleResponse();
        //    return Task.CompletedTask;
        //}
    };
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

app.UseWebSockets(webSocketOptions);


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
