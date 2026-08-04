using Hambaft.Api.Middleware;
using Hambaft.Api.Realtime;
using Hambaft.Api.Security;
using Hambaft.Application;
using Hambaft.Contracts;
using Hambaft.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();builder.Services.AddOpenApi();builder.Services.AddSignalR();
builder.Services.AddHambaftInfrastructure(builder.Configuration);

var jwt=JwtConfiguration.From(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<JwtTokenIssuer>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options=>
{
    options.MapInboundClaims=false;
    options.TokenValidationParameters=new TokenValidationParameters{ValidateIssuerSigningKey=true,IssuerSigningKey=jwt.SigningKey,ValidateIssuer=true,ValidIssuer=jwt.Issuer,ValidateAudience=true,ValidAudience=jwt.Audience,ValidateLifetime=true,ClockSkew=TimeSpan.FromSeconds(30),RoleClaimType="client_role"};
    options.Events=new JwtBearerEvents{OnMessageReceived=context=>{if(context.HttpContext.Request.Path.StartsWithSegments("/hubs/session")&&context.Request.Query.TryGetValue("access_token",out var token))context.Token=token;return Task.CompletedTask;}};
});
builder.Services.AddAuthorization(options=>
{
    options.AddPolicy("Team",policy=>policy.RequireClaim("client_role","Team").RequireClaim("session_id").RequireClaim("team_id"));
    options.AddPolicy("Admin",policy=>policy.RequireClaim("client_role","Admin").RequireClaim("session_id"));
    options.AddPolicy("PublicDisplay",policy=>policy.RequireClaim("client_role","PublicDisplay").RequireClaim("session_id"));
    options.AddPolicy("SessionClient",policy=>policy.RequireAuthenticatedUser().RequireAssertion(context=>
    {
        var role=context.User.FindFirst("client_role")?.Value;
        var hasSession=Guid.TryParse(context.User.FindFirst("session_id")?.Value,out _);
        return hasSession&&(role is "Admin" or "PublicDisplay" || role=="Team"&&Guid.TryParse(context.User.FindFirst("team_id")?.Value,out _));
    }));
});

var app=builder.Build();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.Use(async(context,next)=>{var supplied=context.Request.Headers["X-Correlation-ID"].FirstOrDefault();context.TraceIdentifier=Guid.TryParse(supplied,out var id)?id.ToString("D"):Guid.NewGuid().ToString("D");context.Response.Headers["X-Correlation-ID"]=context.TraceIdentifier;await next();});
app.UseAuthentication();app.UseAuthorization();
if(app.Environment.IsDevelopment()){app.MapOpenApi();app.UseSwaggerUI(options=>options.SwaggerEndpoint("/openapi/v1.json","HAMBAFT API v1"));}

static CommandContext Context(HttpContext http,Guid? teamId=null)
{
    var correlation=Guid.TryParse(http.TraceIdentifier,out var parsed)?parsed:Guid.NewGuid();var commandId=Guid.NewGuid();return new(correlation,commandId,commandId,teamId,DateTimeOffset.UtcNow);
}
static async Task Notify(IHubContext<SessionHub,ISessionHubClient> hub,CommandResult result,string scope,CancellationToken ct)=>await hub.Clients.Group(HubGroups.Session(result.SessionId)).StateChanged(new(result.SessionId,result.StateVersion,result.EventType,scope));

app.MapPost("/api/sessions",async(CreateSessionRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var id=Guid.NewGuid();var result=await runtime.ExecuteAsync(new CreateSession(id,request.StoryPackageId,request.StoryVersion,request.ContentHash,request.Seed,request.ExpectedVersion,Context(http)),ct);await Notify(hub,result,"Session",ct);return Results.Created($"/api/sessions/{id:D}",new CommandResponse(id,result.StateVersion,result.EventType,id));
});
app.MapGet("/api/sessions/{sessionId:guid}",async(Guid sessionId,SessionRuntime runtime,CancellationToken ct)=>await runtime.GetSessionAsync(sessionId,ct) is { } view?Results.Ok(ToResponse(view)):Results.NotFound());
app.MapPost("/api/sessions/{sessionId:guid}/teams",async(Guid sessionId,AddTeamRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var id=Guid.NewGuid();var result=await runtime.ExecuteAsync(new AddTeam(sessionId,id,request.DisplayName,request.ExpectedVersion,Context(http,id)),ct);await Notify(hub,result,"Team",ct);return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType,id,result.PairingCode));
});
app.MapPost("/api/sessions/{sessionId:guid}/entities",async(Guid sessionId,CreateEntityRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var id=Guid.NewGuid();var result=await runtime.CreateEntityAsync(sessionId,id,request.DefinitionId,request.DisplayName,request.ControllerType,request.BehaviorProfileId,request.ExpectedVersion,Context(http),ct);await Notify(hub,result,"Entity",ct);return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType,id));
});
app.MapPost("/api/sessions/{sessionId:guid}/assignments",async(Guid sessionId,AssignEntityRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var result=await runtime.ExecuteAsync(new AssignEntityToTeam(sessionId,request.EntityId,request.TeamId,request.ExpectedVersion,Context(http,request.TeamId)),ct);await Notify(hub,result,"Entity",ct);return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType));
});
app.MapPost("/api/sessions/{sessionId:guid}/start",(Guid sessionId,TransitionRequest r,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>Transition(new StartSession(sessionId,r.ExpectedVersion,Context(http)),runtime,hub,ct));
app.MapPost("/api/sessions/{sessionId:guid}/pause",(Guid sessionId,TransitionRequest r,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>Transition(new PauseSession(sessionId,r.ExpectedVersion,Context(http)),runtime,hub,ct));
app.MapPost("/api/sessions/{sessionId:guid}/resume",(Guid sessionId,TransitionRequest r,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>Transition(new ResumeSession(sessionId,r.ExpectedVersion,Context(http)),runtime,hub,ct));
app.MapGet("/api/sessions/{sessionId:guid}/public",async(Guid sessionId,SessionRuntime runtime,CancellationToken ct)=>await runtime.GetPublicAsync(sessionId,ct) is { } view?Results.Ok(view):Results.NotFound());
app.MapPost("/api/sessions/{sessionId:guid}/pair",async(Guid sessionId,PairingRequest request,SessionRuntime runtime,IPairingCodeGenerator pairing,JwtTokenIssuer tokens,CancellationToken ct)=>
{
    var session=await runtime.GetSessionAsync(sessionId,ct);
    var team=session?.Teams.SingleOrDefault(x=>x.Id==request.TeamId);
    if(team is null||string.IsNullOrWhiteSpace(request.PairingCode)||!pairing.Verify(request.PairingCode.Trim().ToUpperInvariant(),team.PairingCodeHash))return Results.Unauthorized();
    return Results.Ok(tokens.IssueTeam(sessionId,team.Id));
});

if(app.Environment.IsDevelopment())app.MapPost("/internal/sessions/{sessionId:guid}/bootstrap",async(Guid sessionId,BootstrapRequest request,SessionRuntime runtime,HttpContext http,CancellationToken ct)=>
{
    var version=request.ExpectedVersion;CommandResult? result=null;
    foreach(var x in request.Metrics){result=await runtime.SetMetricAsync(sessionId,x.Scope,x.ScopeId,x.MetricKey,x.NumericValue,version++,Context(http),ct);}
    foreach(var x in request.Memories){result=await runtime.AddMemoryAsync(sessionId,x.Scope,x.ScopeId,x.Key,x.OptionalJsonValue,x.Visibility,version++,Context(http),ct);}
    foreach(var x in request.Relationships){result=await runtime.ExecuteAsync(new SetInitialRelationship(sessionId,x.SourceEntityId,x.TargetEntityId,x.RelationshipKey,x.NumericValue,version++,Context(http)),ct);}
    return Results.Ok(new CommandResponse(sessionId,result?.StateVersion??request.ExpectedVersion,result?.EventType??"NoChanges"));
}).ExcludeFromDescription();

app.MapHub<SessionHub>("/hubs/session");
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive",new HealthCheckOptions{Predicate=_=>false});
app.MapHealthChecks("/ready",new HealthCheckOptions{Predicate=check=>check.Tags.Contains("ready")});
app.Run();

static async Task<IResult> Transition(object command,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,CancellationToken ct)
{
    var result=command switch{StartSession c=>await runtime.ExecuteAsync(c,ct),PauseSession c=>await runtime.ExecuteAsync(c,ct),ResumeSession c=>await runtime.ExecuteAsync(c,ct),_=>throw new ArgumentException("Unknown transition.")};await Notify(hub,result,"Session",ct);return Results.Ok(new CommandResponse(result.SessionId,result.StateVersion,result.EventType));
}

static SessionResponse ToResponse(SessionStateView view)=>new(
    view.Id,view.StoryPackageId,view.StoryVersion,view.ContentHash,view.Seed,view.Status.ToString(),view.CreatedAtUtc,view.StartedAtUtc,view.CompletedAtUtc,
    view.Teams.Select(x=>new TeamResponse(x.Id,x.SessionId,x.DisplayName,x.ControlledEntityId,x.JoinedAtUtc)).ToList(),
    view.Entities.Select(x=>new EntityResponse(x.Id,x.SessionId,x.DefinitionId,x.DisplayName,x.ControllerType.ToString(),x.ControlledByTeamId,x.BehaviorProfileId,x.Status.ToString())).ToList(),
    view.StateVersion);

public partial class Program;
