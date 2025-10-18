using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;
using Microsoft.Teams.AI.Models.OpenAI.Extensions;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Apps.Extensions;

using My.Doom.Agent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<MainController>();
builder.AddTeams().AddTeamsDevTools().AddOpenAI<DoomPrompt>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseTeams();
app.AddTab("dialog-form", "Web/dialog-form");

app.Run();
