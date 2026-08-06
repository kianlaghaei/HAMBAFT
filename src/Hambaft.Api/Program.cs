using Hambaft.Api.Middleware;
using Hambaft.Api.Realtime;
using Hambaft.Api.Security;
using Hambaft.Application;
using Hambaft.Contracts;
using Hambaft.Domain;
using Hambaft.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();builder.Services.AddOpenApi();builder.Services.AddSignalR();
builder.Services.AddHambaftInfrastructure(builder.Configuration,builder.Environment.ContentRootPath);
builder.Services.AddCors(options=>options.AddPolicy("DevelopmentSpa",policy=>policy.WithOrigins("http://localhost:5173","http://127.0.0.1:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

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
if(app.Environment.IsDevelopment())app.UseCors("DevelopmentSpa");
app.UseDefaultFiles();app.UseStaticFiles();
app.UseAuthentication();app.UseAuthorization();
if(app.Environment.IsDevelopment()){app.MapOpenApi();app.UseSwaggerUI(options=>options.SwaggerEndpoint("/openapi/v1.json","HAMBAFT API v1"));}

static CommandContext Context(HttpContext http,Guid? teamId=null,Guid? commandId=null)
{
    var correlation=Guid.TryParse(http.TraceIdentifier,out var parsed)?parsed:Guid.NewGuid();var id=commandId is { } supplied&&supplied!=Guid.Empty?supplied:Guid.NewGuid();return new(correlation,id,id,teamId,DateTimeOffset.UtcNow);
}
static Guid ClaimGuid(HttpContext http,string name)=>Guid.TryParse(http.User.FindFirst(name)?.Value,out var id)?id:throw new ArgumentException($"Required claim '{name}' is missing.");
static async Task Notify(IHubContext<SessionHub,ISessionHubClient> hub,CommandResult result,string scope,CancellationToken ct)=>await hub.Clients.Group(HubGroups.Session(result.SessionId)).StateChanged(new(result.SessionId,result.StateVersion,result.EventType,scope));
static ProposalValidityDefinition? Validity(string? type,int? count,string? checkpoint)
{
    if(string.IsNullOrWhiteSpace(type))return null;if(!Enum.TryParse<ProposalValidityType>(type,true,out var parsed))throw new ArgumentException("Invalid proposal validity type.");return new(parsed,checkpoint,count);
}
static async Task NotifyInteraction(IHubContext<SessionHub,ISessionHubClient> hub,string eventType,Guid sessionId,Guid proposalId,Guid? agreementId,long version,IEnumerable<Guid> teams,bool isPublic=false)
{
    var notification=new InteractionNotification(sessionId,proposalId,agreementId,version,eventType);
    foreach(var team in teams.Distinct())
    {
        var client=hub.Clients.Group(HubGroups.Team(team));
        switch(eventType){case nameof(ProposalSent):await client.ProposalReceived(notification with { EventType="ProposalReceived" });break;case nameof(ProposalCountered):await client.ProposalCountered(notification);break;case nameof(ProposalAccepted):await client.ProposalAccepted(notification);break;case nameof(ProposalRejected):await client.ProposalRejected(notification);break;case nameof(ProposalExpired):await client.ProposalExpired(notification);break;case nameof(ProposalCancelled):await client.ProposalCancelled(notification);break;case nameof(AgreementActivated):await client.AgreementActivated(notification);break;case nameof(AgreementExecuted):await client.AgreementExecuted(notification);break;case nameof(AgreementFailed):await client.AgreementFailed(notification);break;}
    }
    if(isPublic&&eventType is nameof(AgreementActivated) or nameof(AgreementExecuted) or nameof(AgreementFailed))
    {
        foreach(var client in new[]{hub.Clients.Group(HubGroups.Session(sessionId)),hub.Clients.Group(HubGroups.Display(sessionId))})
        {
            if(eventType==nameof(AgreementActivated))await client.AgreementActivated(notification);else if(eventType==nameof(AgreementExecuted))await client.AgreementExecuted(notification);else await client.AgreementFailed(notification);
        }
    }
}

app.MapPost("/api/sessions",async(CreateSessionRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var id=Guid.NewGuid();var result=await runtime.ExecuteAsync(new CreateSession(id,request.StoryPackageId,request.StoryVersion,request.ContentHash,request.Seed,request.ExpectedVersion,Context(http),request.DifficultyId),ct);await Notify(hub,result,"Session",ct);return Results.Created($"/api/sessions/{id:D}",new CommandResponse(id,result.StateVersion,result.EventType,id));
});
app.MapGet("/api/story-packages",async(IStoryPackageCatalog catalog,CancellationToken ct)=>Results.Ok(await catalog.ListAsync(ct)));
app.MapGet("/api/story-packages/{packageId}/{version}",async(string packageId,string version,IStoryPackageCatalog catalog,IStoryPackageLoader loader,CancellationToken ct)=>
{
    var metadata=(await catalog.ListAsync(ct)).SingleOrDefault(x=>x.Id==packageId&&x.Version==version);
    if(metadata is null)return Results.NotFound();
    var package=await loader.LoadAsync(packageId,version,ct);return Results.Ok(ToClientPackage(package));
});
app.MapGet("/api/sessions/{sessionId:guid}",async(Guid sessionId,SessionRuntime runtime,CancellationToken ct)=>await runtime.GetSessionAsync(sessionId,ct) is { } view?Results.Ok(ToResponse(view)):Results.NotFound());
app.MapGet("/api/sessions/{sessionId:guid}/admin",async(Guid sessionId,SessionRuntime runtime,HttpContext http,CancellationToken ct)=>
{
    if(ClaimGuid(http,"session_id")!=sessionId)return Results.Forbid();return await runtime.GetAdminAsync(sessionId,ct) is { } view?Results.Ok(view):Results.NotFound();
}).RequireAuthorization("Admin");
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
app.MapGet("/api/public/sessions/{sessionId:guid}",async(Guid sessionId,SessionRuntime runtime,CancellationToken ct)=>await runtime.GetPublicAsync(sessionId,ct) is { } view?Results.Ok(view):Results.NotFound());
app.MapGet("/api/admin/sessions/{sessionId:guid}",async(Guid sessionId,SessionRuntime runtime,HttpContext http,CancellationToken ct)=>ClaimGuid(http,"session_id")!=sessionId?Results.Forbid():await runtime.GetAdminAsync(sessionId,ct) is { } view?Results.Ok(view):Results.NotFound()).RequireAuthorization("Admin");
app.MapPost("/api/sessions/{sessionId:guid}/pair",async(Guid sessionId,PairingRequest request,SessionRuntime runtime,IPairingCodeGenerator pairing,JwtTokenIssuer tokens,CancellationToken ct)=>
{
    var session=await runtime.GetSessionAsync(sessionId,ct);
    var team=session?.Teams.SingleOrDefault(x=>x.Id==request.TeamId);
    if(team is null||string.IsNullOrWhiteSpace(request.PairingCode)||!pairing.Verify(request.PairingCode.Trim().ToUpperInvariant(),team.PairingCodeHash))return Results.Unauthorized();
    return Results.Ok(tokens.IssueTeam(sessionId,team.Id));
});

app.MapPost("/api/pair",async(PairByCodeRequest request,ISessionStore store,IPairingCodeGenerator pairing,JwtTokenIssuer tokens,CancellationToken ct)=>
{
    if(string.IsNullOrWhiteSpace(request.PairingCode))return Results.Unauthorized();
    var code=request.PairingCode.Trim().ToUpperInvariant();var matches=new List<PairingCandidate>();
    foreach(var candidate in await store.ListPairingCandidatesAsync(ct))if(pairing.Verify(code,candidate.PairingCodeHash))matches.Add(candidate);
    return matches.Count==1?Results.Ok(tokens.IssueTeam(matches[0].SessionId,matches[0].TeamId)):Results.Unauthorized();
});

app.MapGet("/api/story/experience",async(SessionRuntime runtime,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");var view=await runtime.GetTeamAsync(teamId,ct);
    return view is null?Results.NotFound():view.SessionId!=sessionId?Results.Forbid():Results.Ok(view);
}).RequireAuthorization("Team");

app.MapPost("/api/sessions/{sessionId:guid}/narrative/initialize",async(Guid sessionId,InitializeNarrativeRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    if(ClaimGuid(http,"session_id")!=sessionId)return Results.Forbid();
    var result=await runtime.ExecuteAsync(new InitializeNarrative(sessionId,request.ExpectedStateVersion,Context(http,commandId:request.CommandId)),ct);
    var state=(await runtime.GetSessionAsync(sessionId,ct))!;var checkpoint=state.CurrentCheckpointId!;
    var initialized=new NarrativeNotification(sessionId,null,result.StateVersion,checkpoint,nameof(NarrativeInitialized));
    await hub.Clients.Group(HubGroups.Session(sessionId)).NarrativeInitialized(initialized);
    foreach(var assignment in state.StoryletAssignments.Where(x=>x.CheckpointId==checkpoint&&x.TargetTeamId is not null&&x.Status==StoryletAssignmentStatus.Assigned))
    {
        var notification=new NarrativeNotification(sessionId,assignment.TargetTeamId,result.StateVersion,checkpoint,nameof(StoryletAssigned));
        await hub.Clients.Group(HubGroups.Team(assignment.TargetTeamId!.Value)).PrivateStoryAssigned(notification);
        if(assignment.RequiredResponse)await hub.Clients.Group(HubGroups.Team(assignment.TargetTeamId.Value)).DecisionRequested(notification with { EventType="DecisionRequested" });
    }
    await hub.Clients.Group(HubGroups.Display(sessionId)).WorldNarrativePublished(initialized with { EventType=nameof(WorldNarrativePublished) });
    return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType));
}).RequireAuthorization("Admin");

app.MapPost("/api/story/choices",async(SubmitStoryChoiceRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");
    var result=await runtime.ExecuteAsync(new SubmitStoryChoice(sessionId,request.AssignmentId,request.ChoiceId,request.ExpectedStateVersion,Context(http,teamId,request.CommandId)),ct);
    var state=(await runtime.GetSessionAsync(sessionId,ct))!;var notification=new NarrativeNotification(sessionId,teamId,result.StateVersion,state.CurrentCheckpointId!,nameof(StoryChoiceSubmitted));
    await hub.Clients.Group(HubGroups.Team(teamId)).ChoiceRecorded(notification);
    await hub.Clients.Group(HubGroups.Admins(sessionId)).ChoiceRecorded(notification);
    await hub.Clients.Group(HubGroups.Session(sessionId)).StateChanged(new(sessionId,result.StateVersion,"ChoiceRecorded","Session"));
    return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType));
}).RequireAuthorization("Team");

app.MapPost("/api/story/investigations",async(RecordTeamInvestigationRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");
    var result=await runtime.ExecuteAsync(new RecordTeamInvestigation(sessionId,request.LocationId,request.EvidenceIds,request.ExpectedStateVersion,Context(http,teamId,request.CommandId)),ct);
    await hub.Clients.Group(HubGroups.Team(teamId)).StateChanged(new(sessionId,result.StateVersion,result.EventType,"Team"));
    await hub.Clients.Group(HubGroups.Admins(sessionId)).StateChanged(new(sessionId,result.StateVersion,result.EventType,"Team"));
    return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType));
}).RequireAuthorization("Team");

app.MapGet("/api/proposals/inbox",async(SessionRuntime runtime,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");var view=await runtime.GetTeamAsync(teamId,ct);return view is null?Results.NotFound():view.SessionId!=sessionId?Results.Forbid():Results.Ok(new ProposalInboxView(teamId,sessionId,view.Inbox??[],[],view.StateVersion));
}).RequireAuthorization("Team");
app.MapGet("/api/proposals/outbox",async(SessionRuntime runtime,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");var view=await runtime.GetTeamAsync(teamId,ct);return view is null?Results.NotFound():view.SessionId!=sessionId?Results.Forbid():Results.Ok(new ProposalInboxView(teamId,sessionId,[],view.Outbox??[],view.StateVersion));
}).RequireAuthorization("Team");
app.MapGet("/api/agreements",async(SessionRuntime runtime,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");var view=await runtime.GetTeamAsync(teamId,ct);return view is null?Results.NotFound():view.SessionId!=sessionId?Results.Forbid():Results.Ok(view.Agreements??[]);
}).RequireAuthorization("Team");
app.MapPost("/api/proposals",async(SendProposalRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");var proposalId=Guid.NewGuid();var result=await runtime.ExecuteAsync(new SendProposal(sessionId,proposalId,request.InteractionTypeId,request.ReceiverTeamId,request.TermsPayload,Validity(request.ValidityType,request.ValidForCheckpointCount,request.ValidUntilCheckpointId),request.ExpectedStateVersion,Context(http,teamId,request.CommandId)),ct);await NotifyInteraction(hub,nameof(ProposalSent),sessionId,proposalId,null,result.StateVersion,[request.ReceiverTeamId]);return Results.Created($"/api/proposals/{proposalId:D}",new CommandResponse(sessionId,result.StateVersion,result.EventType,proposalId));
}).RequireAuthorization("Team");
app.MapPost("/api/proposals/{proposalId:guid}/counter",async(Guid proposalId,CounterProposalRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");var result=await runtime.ExecuteAsync(new CounterProposal(sessionId,proposalId,request.ExpectedRevisionNumber,request.TermsPayload,request.ExpectedStateVersion,Context(http,teamId,request.CommandId)),ct);var state=(await runtime.GetSessionAsync(sessionId,ct))!;var proposal=state.Proposals!.Single(x=>x.ProposalId==proposalId);var revision=state.ProposalRevisions!.Single(x=>x.ProposalId==proposalId&&x.RevisionNumber==proposal.CurrentRevisionNumber);var responder=revision.CreatedByTeamId==proposal.SenderTeamId?proposal.ReceiverTeamId:proposal.SenderTeamId;await NotifyInteraction(hub,nameof(ProposalCountered),sessionId,proposalId,null,result.StateVersion,[responder]);return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType,proposalId));
}).RequireAuthorization("Team");
app.MapPost("/api/proposals/{proposalId:guid}/accept",async(Guid proposalId,ProposalDecisionRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");var result=await runtime.ExecuteAsync(new AcceptProposal(sessionId,proposalId,request.ExpectedRevisionNumber,request.ExpectedStateVersion,Context(http,teamId,request.CommandId)),ct);await NotifyInteraction(hub,nameof(ProposalAccepted),sessionId,proposalId,result.ResourceId,result.StateVersion,result.AffectedTeamIds??[]);await NotifyInteraction(hub,nameof(AgreementActivated),sessionId,proposalId,result.ResourceId,result.StateVersion,result.AffectedTeamIds??[],result.IsPublic);if(result.EmittedEventTypes?.Contains(nameof(AgreementExecuted))==true)await NotifyInteraction(hub,nameof(AgreementExecuted),sessionId,proposalId,result.ResourceId,result.StateVersion,result.AffectedTeamIds??[],result.IsPublic);return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType,result.ResourceId));
}).RequireAuthorization("Team");
app.MapPost("/api/proposals/{proposalId:guid}/reject",async(Guid proposalId,ProposalDecisionRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");var result=await runtime.ExecuteAsync(new RejectProposal(sessionId,proposalId,request.ExpectedRevisionNumber,request.ExpectedStateVersion,Context(http,teamId,request.CommandId)),ct);await NotifyInteraction(hub,nameof(ProposalRejected),sessionId,proposalId,null,result.StateVersion,result.AffectedTeamIds??[]);return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType,proposalId));
}).RequireAuthorization("Team");
app.MapPost("/api/proposals/{proposalId:guid}/cancel",async(Guid proposalId,ProposalDecisionRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");var result=await runtime.ExecuteAsync(new CancelProposal(sessionId,proposalId,request.ExpectedRevisionNumber,request.ExpectedStateVersion,Context(http,teamId,request.CommandId)),ct);await NotifyInteraction(hub,nameof(ProposalCancelled),sessionId,proposalId,null,result.StateVersion,result.AffectedTeamIds??[]);return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType,proposalId));
}).RequireAuthorization("Team");

app.MapPost("/api/sessions/{sessionId:guid}/narrative/resolve",async(Guid sessionId,ResolveNarrativeRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    if(ClaimGuid(http,"session_id")!=sessionId)return Results.Forbid();
    var before=(await runtime.GetSessionAsync(sessionId,ct))!;
    var result=await runtime.ExecuteAsync(new ResolveNarrativeCheckpoint(sessionId,request.ExpectedStateVersion,Context(http,commandId:request.CommandId)),ct);
    var state=(await runtime.GetSessionAsync(sessionId,ct))!;var notification=new NarrativeNotification(sessionId,null,result.StateVersion,state.CurrentCheckpointId!,nameof(NarrativeCheckpointResolved));
    foreach(var proposal in state.Proposals??[])
    {
        var old=(before.Proposals??[]).SingleOrDefault(x=>x.ProposalId==proposal.ProposalId);if(old?.Status is ProposalStatus.Pending or ProposalStatus.Countered&&proposal.Status==ProposalStatus.Expired)await NotifyInteraction(hub,nameof(ProposalExpired),sessionId,proposal.ProposalId,null,result.StateVersion,[proposal.SenderTeamId,proposal.ReceiverTeamId]);
    }
    foreach(var agreement in state.Agreements??[])
    {
        var old=(before.Agreements??[]).SingleOrDefault(x=>x.AgreementId==agreement.AgreementId);if(old?.Status==AgreementStatus.Active&&agreement.Status==AgreementStatus.Executed)await NotifyInteraction(hub,nameof(AgreementExecuted),sessionId,agreement.ProposalId,agreement.AgreementId,result.StateVersion,agreement.PartyTeamIds,agreement.Visibility==AgreementVisibility.Public);else if(old?.Status==AgreementStatus.Active&&agreement.Status==AgreementStatus.Failed)await NotifyInteraction(hub,nameof(AgreementFailed),sessionId,agreement.ProposalId,agreement.AgreementId,result.StateVersion,agreement.PartyTeamIds,agreement.Visibility==AgreementVisibility.Public);
    }
    foreach(var consequence in state.ScheduledConsequences??[])
    {
        var old=(before.ScheduledConsequences??[]).SingleOrDefault(x=>x.ScheduledConsequenceId==consequence.ScheduledConsequenceId);if(old is null||old.Status!=consequence.Status)
        {
            var change=new NarrativeNotification(sessionId,consequence.SourceTeamId,result.StateVersion,state.CurrentCheckpointId!,consequence.Status==ScheduledConsequenceStatus.Triggered?nameof(ConsequenceTriggered):nameof(ConsequenceScheduled));if(consequence.Visibility==ConsequenceVisibility.Public){await hub.Clients.Group(HubGroups.Session(sessionId)).ConsequenceChanged(change);await hub.Clients.Group(HubGroups.Display(sessionId)).ConsequenceChanged(change);}else if(consequence.Visibility==ConsequenceVisibility.PrivateToSourceTeam&&consequence.SourceTeamId is { } team)await hub.Clients.Group(HubGroups.Team(team)).ConsequenceChanged(change);
        }
    }
    if((state.AuthoredBehaviorSelections?.Count??0)>(before.AuthoredBehaviorSelections?.Count??0)){var behavior=new NarrativeNotification(sessionId,null,result.StateVersion,state.CurrentCheckpointId!,nameof(AuthoredBehaviorActionSelected));await hub.Clients.Group(HubGroups.Session(sessionId)).AuthoredBehaviorResolved(behavior);await hub.Clients.Group(HubGroups.Display(sessionId)).AuthoredBehaviorResolved(behavior);}
    await hub.Clients.Group(HubGroups.Session(sessionId)).NarrativeResolved(notification);
    await hub.Clients.Group(HubGroups.Display(sessionId)).WorldNarrativePublished(notification with { EventType=nameof(WorldNarrativePublished) });
    return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType));
}).RequireAuthorization("Admin");

app.MapPost("/api/sessions/{sessionId:guid}/endings/resolve",async(Guid sessionId,ResolveEndingsRequest request,SessionRuntime runtime,IHubContext<SessionHub,ISessionHubClient> hub,HttpContext http,CancellationToken ct)=>
{
    if(ClaimGuid(http,"session_id")!=sessionId)return Results.Forbid();
    var result=await runtime.ExecuteAsync(new ResolveEndings(sessionId,request.ExpectedStateVersion,Context(http,commandId:request.CommandId)),ct);
    var endings=(await runtime.GetEndingsAsync(sessionId,ct))!;var state=(await runtime.GetSessionAsync(sessionId,ct))!;
    foreach(var entityEnding in endings.EntityEndings)
    {
        var teamId=state.Entities.Single(x=>x.Id==entityEnding.ScopeId).ControlledByTeamId;
        if(teamId is { } team)await hub.Clients.Group(HubGroups.Team(team)).EntityEndingPublished(new(sessionId,entityEnding.ScopeId,entityEnding.EndingDefinitionId,result.StateVersion,nameof(EntityEndingResolved)));
    }
    var world=endings.WorldEnding!;var worldNotice=new EndingNotification(sessionId,sessionId,world.EndingDefinitionId,result.StateVersion,nameof(WorldEndingResolved));
    foreach(var client in new[]{hub.Clients.Group(HubGroups.Session(sessionId)),hub.Clients.Group(HubGroups.Display(sessionId)),hub.Clients.Group(HubGroups.Admins(sessionId))})await client.WorldEndingPublished(worldNotice);
    var completed=worldNotice with { EventType=nameof(SessionCompleted) };
    foreach(var client in new[]{hub.Clients.Group(HubGroups.Session(sessionId)),hub.Clients.Group(HubGroups.Display(sessionId)),hub.Clients.Group(HubGroups.Admins(sessionId))})await client.SessionCompleted(completed);
    return Results.Ok(new CommandResponse(sessionId,result.StateVersion,result.EventType));
}).RequireAuthorization("Admin");

app.MapGet("/api/endings",async(SessionRuntime runtime,HttpContext http,CancellationToken ct)=>
{
    var sessionId=ClaimGuid(http,"session_id");var teamId=ClaimGuid(http,"team_id");var view=await runtime.GetTeamAsync(teamId,ct);
    return view is null?Results.NotFound():view.SessionId!=sessionId?Results.Forbid():Results.Ok(new { view.EntityEnding,view.WorldEnding,view.StateVersion });
}).RequireAuthorization("Team");

app.MapGet("/api/sessions/{sessionId:guid}/endings",async(Guid sessionId,SessionRuntime runtime,HttpContext http,CancellationToken ct)=>
{
    if(ClaimGuid(http,"session_id")!=sessionId)return Results.Forbid();var role=http.User.FindFirst("client_role")?.Value;
    if(role=="Admin")return await runtime.GetEndingsAsync(sessionId,ct) is { } evidence?Results.Ok(evidence):Results.NotFound();
    if(role=="Team"){var teamId=ClaimGuid(http,"team_id");var view=await runtime.GetTeamAsync(teamId,ct);return view is null?Results.NotFound():Results.Ok(new { view.EntityEnding,view.WorldEnding,view.StateVersion });}
    var publicView=await runtime.GetPublicAsync(sessionId,ct);return publicView is null?Results.NotFound():Results.Ok(new { publicView.WorldEnding,publicView.PublicEntityEndingSummaries,publicView.StateVersion });
}).RequireAuthorization("SessionClient");

if(app.Environment.IsDevelopment())app.MapPost("/internal/sessions/{sessionId:guid}/bootstrap",async(Guid sessionId,BootstrapRequest request,SessionRuntime runtime,HttpContext http,CancellationToken ct)=>
{
    var version=request.ExpectedVersion;CommandResult? result=null;
    foreach(var x in request.Metrics){result=await runtime.SetMetricAsync(sessionId,x.Scope,x.ScopeId,x.MetricKey,x.NumericValue,version++,Context(http),ct);}
    foreach(var x in request.Memories){result=await runtime.AddMemoryAsync(sessionId,x.Scope,x.ScopeId,x.Key,x.OptionalJsonValue,x.Visibility,version++,Context(http),ct);}
    foreach(var x in request.Relationships){result=await runtime.ExecuteAsync(new SetInitialRelationship(sessionId,x.SourceEntityId,x.TargetEntityId,x.RelationshipKey,x.NumericValue,version++,Context(http)),ct);}
    return Results.Ok(new CommandResponse(sessionId,result?.StateVersion??request.ExpectedVersion,result?.EventType??"NoChanges"));
}).ExcludeFromDescription();

if(app.Environment.IsDevelopment())
{
    app.MapPost("/internal/auth/admin",async(DevelopmentTokenRequest request,SessionRuntime runtime,JwtTokenIssuer tokens,CancellationToken ct)=>
        await runtime.GetSessionAsync(request.SessionId,ct) is null?Results.NotFound():Results.Ok(tokens.IssueAdmin(request.SessionId)));
    app.MapPost("/internal/auth/public-display",async(DevelopmentTokenRequest request,SessionRuntime runtime,JwtTokenIssuer tokens,CancellationToken ct)=>
        await runtime.GetSessionAsync(request.SessionId,ct) is null?Results.NotFound():Results.Ok(tokens.IssuePublicDisplay(request.SessionId)));
}

app.MapHub<SessionHub>("/hubs/session");
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive",new HealthCheckOptions{Predicate=_=>false});
app.MapHealthChecks("/ready",new HealthCheckOptions{Predicate=check=>check.Tags.Contains("ready")});
app.MapFallbackToFile("index.html");
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

static StoryPackageClientResponse ToClientPackage(StoryPackage package)
{
    NarrativeDefinition? Text(string reference)
    {
        var locale=package.Manifest.DefaultLocale;
        return package.Narrative.TryGetValue(locale,out var localized)&&localized.TryGetValue(reference,out var value)?value:null;
    }
    StoryPackageTermSchemaResponse Terms(InteractionTermSchemaDefinition schema)=>new(schema.Type.ToString(),schema.Name,schema.Required,schema.Minimum,schema.Maximum,schema.MaximumLength,(schema.Fields??[]).Select(Terms).ToList());
    string Label(string reference,string fallback)=>Text(reference) is { } value?(value.ChoiceLabel??value.Title):fallback;
    return new(package.Manifest.Id,package.Manifest.Version,package.Manifest.Title,package.Manifest.Description,package.Manifest.MinimumTeams,package.Manifest.MaximumTeams,package.Manifest.EstimatedDurationMinutes,package.Manifest.DefaultLocale,package.ContentHash,
        package.Entities.Select(x=>new StoryPackageEntityResponse(x.Id,x.DisplayName,x.ControllerRequirement.ToString(),x.PublicTags)).ToList(),
        package.Metrics.Select(x=>new StoryPackageMetricResponse(x.Key,x.Scope.ToString(),x.Minimum,x.Maximum,x.DefaultValue)).ToList(),
        package.InteractionDefinitions.Select(x=>new StoryPackageInteractionResponse(x.Id,Label(x.DisplayNameRef,x.Id),Text(x.DescriptionRef)?.Title??x.Id,Terms(x.TermsSchema),x.DefaultValidity.Type.ToString(),x.DefaultValidity.CheckpointId,x.DefaultValidity.CheckpointCount,x.ExecutionMode.ToString(),x.AgreementVisibility.ToString())).ToList(),
        package.DifficultyDefinitions.Select(x=>new StoryPackageDifficultyResponse(x.Id,Label(x.DisplayNameRef,x.Id))).ToList(),
        package.BehaviorDefinitions.Profiles.Select(x=>new StoryPackageBehaviorProfileResponse(x.Id,Label(x.DisplayNameRef,x.Id),x.EligibleEntityDefinitions)).ToList());
}

public partial class Program;
