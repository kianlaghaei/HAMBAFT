using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hambaft.Api.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Hambaft.IntegrationTests;

[Collection("postgres")]
public sealed class NarrativeApiFlowTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Authenticated_two_team_rest_story_flow_preserves_privacy()
    {
        await using var factory=new HambaftApiFactory(fixture.ConnectionString);using var client=factory.CreateClient();
        var packages=await Body(await client.GetAsync("/api/story-packages"));var sample=packages.EnumerateArray().Single(x=>x.GetProperty("id").GetString()=="sample-cargo-delay"&&x.GetProperty("version").GetString()=="1.0.0");sample.GetProperty("isValid").GetBoolean().Should().BeTrue();var hash=sample.GetProperty("contentHash").GetString()!;
        var created=await Body(await client.PostAsJsonAsync("/api/sessions",new{storyPackageId="sample-cargo-delay",storyVersion="1.0.0",contentHash=hash,seed=42,expectedVersion=0}));var sessionId=created.GetProperty("sessionId").GetGuid();
        var supplierAdded=await Body(await client.PostAsJsonAsync($"/api/sessions/{sessionId}/teams",new{displayName="تامین‌کننده",expectedVersion=1}));var supplierTeam=supplierAdded.GetProperty("resourceId").GetGuid();var supplierCode=supplierAdded.GetProperty("pairingCode").GetString()!;
        var carrierAdded=await Body(await client.PostAsJsonAsync($"/api/sessions/{sessionId}/teams",new{displayName="حمل‌کننده",expectedVersion=2}));var carrierTeam=carrierAdded.GetProperty("resourceId").GetGuid();var carrierCode=carrierAdded.GetProperty("pairingCode").GetString()!;
        var supplierEntity=(await Body(await client.PostAsJsonAsync($"/api/sessions/{sessionId}/entities",new{definitionId="supplier",displayName="تامین‌کننده",controllerType="HumanTeam",behaviorProfileId=(string?)null,expectedVersion=3}))).GetProperty("resourceId").GetGuid();
        var carrierEntity=(await Body(await client.PostAsJsonAsync($"/api/sessions/{sessionId}/entities",new{definitionId="carrier",displayName="حمل‌کننده",controllerType="HumanTeam",behaviorProfileId=(string?)null,expectedVersion=4}))).GetProperty("resourceId").GetGuid();
        (await client.PostAsJsonAsync($"/api/sessions/{sessionId}/assignments",new{entityId=supplierEntity,teamId=supplierTeam,expectedVersion=5})).EnsureSuccessStatusCode();(await client.PostAsJsonAsync($"/api/sessions/{sessionId}/assignments",new{entityId=carrierEntity,teamId=carrierTeam,expectedVersion=6})).EnsureSuccessStatusCode();(await client.PostAsJsonAsync($"/api/sessions/{sessionId}/start",new{expectedVersion=7})).EnsureSuccessStatusCode();
        var supplierToken=await Pair(client,sessionId,supplierTeam,supplierCode);var carrierToken=await Pair(client,sessionId,carrierTeam,carrierCode);var adminToken=factory.Services.GetRequiredService<JwtTokenIssuer>().IssueAdmin(sessionId).AccessToken;
        var initialize=await Send(client,HttpMethod.Post,$"/api/sessions/{sessionId}/narrative/initialize",adminToken,new{expectedStateVersion=8,commandId=Guid.NewGuid()});initialize.EnsureSuccessStatusCode();var version=(await Body(initialize)).GetProperty("stateVersion").GetInt64();
        var supplierExperience=await AuthGet(client,"/api/story/experience",supplierToken);var carrierExperience=await AuthGet(client,"/api/story/experience",carrierToken);var supplierJson=await supplierExperience.Content.ReadAsStringAsync();var carrierJson=await carrierExperience.Content.ReadAsStringAsync();supplierJson.Should().Contain("supplier-delay-response").And.NotContain("carrier-delay-response");carrierJson.Should().Contain("carrier-delay-response").And.NotContain("supplier-delay-response");
        var supplierAssignment=(await Body(supplierExperience)).GetProperty("privateStorylets")[0].GetProperty("assignmentId").GetGuid();var carrierAssignment=(await Body(carrierExperience)).GetProperty("privateStorylets")[0].GetProperty("assignmentId").GetGuid();
        var supplierChoice=await Send(client,HttpMethod.Post,"/api/story/choices",supplierToken,new{assignmentId=supplierAssignment,choiceId="disclose-full-delay",expectedStateVersion=version,commandId=Guid.NewGuid()});supplierChoice.EnsureSuccessStatusCode();version=(await Body(supplierChoice)).GetProperty("stateVersion").GetInt64();
        var carrierChoice=await Send(client,HttpMethod.Post,"/api/story/choices",carrierToken,new{assignmentId=carrierAssignment,choiceId="reserve-emergency-capacity",expectedStateVersion=version,commandId=Guid.NewGuid()});carrierChoice.EnsureSuccessStatusCode();version=(await Body(carrierChoice)).GetProperty("stateVersion").GetInt64();
        var resolve=await Send(client,HttpMethod.Post,$"/api/sessions/{sessionId}/narrative/resolve",adminToken,new{expectedStateVersion=version,commandId=Guid.NewGuid()});resolve.EnsureSuccessStatusCode();
        var publicResponse=await client.GetAsync($"/api/sessions/{sessionId}/public");publicResponse.EnsureSuccessStatusCode();var publicJson=await publicResponse.Content.ReadAsStringAsync();publicJson.Should().Contain("cargo-delay-coordinated-outcome").And.Contain("هماهنگی موثر").And.NotContain("supplier-delay-response").And.NotContain("carrier-delay-response");
        (await client.GetAsync("/health")).EnsureSuccessStatusCode();(await client.GetAsync("/alive")).EnsureSuccessStatusCode();(await client.GetAsync("/ready")).EnsureSuccessStatusCode();
    }
    private static async Task<string> Pair(HttpClient client,Guid session,Guid team,string code)=>(await Body(await client.PostAsJsonAsync($"/api/sessions/{session}/pair",new{teamId=team,pairingCode=code}))).GetProperty("accessToken").GetString()!;
    private static async Task<HttpResponseMessage> AuthGet(HttpClient client,string uri,string token){var request=new HttpRequestMessage(HttpMethod.Get,uri);request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);var response=await client.SendAsync(request);response.EnsureSuccessStatusCode();return response;}
    private static async Task<HttpResponseMessage> Send(HttpClient client,HttpMethod method,string uri,string token,object body){var request=new HttpRequestMessage(method,uri){Content=JsonContent.Create(body)};request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);return await client.SendAsync(request);}
    private static async Task<JsonElement> Body(HttpResponseMessage response){response.EnsureSuccessStatusCode();using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());return document.RootElement.Clone();}
}
