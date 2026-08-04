using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Hambaft.IntegrationTests;

[Collection("postgres")]
public sealed class ApiFlowTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Rest_pairing_and_authorized_signalr_smoke_flow_completes()
    {
        await using var factory=new HambaftApiFactory(fixture.ConnectionString);
        using var client=factory.CreateClient();

        var create=await client.PostAsJsonAsync("/api/sessions",new
        {
            storyPackageId="demo.story",storyVersion="1.0.0",contentHash="sha256:demo",seed=42,expectedVersion=0
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created=await Body(create);
        var sessionId=created.GetProperty("sessionId").GetGuid();

        var addTeam=await client.PostAsJsonAsync($"/api/sessions/{sessionId:D}/teams",new{displayName="گروه آبی",expectedVersion=1});
        addTeam.EnsureSuccessStatusCode();
        var teamBody=await Body(addTeam);
        var teamId=teamBody.GetProperty("resourceId").GetGuid();
        var pairingCode=teamBody.GetProperty("pairingCode").GetString()!;

        var sessionRead=await client.GetStringAsync($"/api/sessions/{sessionId:D}");
        sessionRead.Should().NotContain("pairingCodeHash").And.NotContain(pairingCode);

        var badPair=await client.PostAsJsonAsync($"/api/sessions/{sessionId:D}/pair",new{teamId,pairingCode="WRONG234"});
        badPair.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var pair=await client.PostAsJsonAsync($"/api/sessions/{sessionId:D}/pair",new{teamId,pairingCode});
        pair.EnsureSuccessStatusCode();
        var token=(await Body(pair)).GetProperty("accessToken").GetString()!;

        var anonymousNegotiate=await client.PostAsync("/hubs/session/negotiate?negotiateVersion=1",null);
        anonymousNegotiate.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var negotiateRequest=new HttpRequestMessage(HttpMethod.Post,"/hubs/session/negotiate?negotiateVersion=1");
        negotiateRequest.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        var authorizedNegotiate=await client.SendAsync(negotiateRequest);
        authorizedNegotiate.EnsureSuccessStatusCode();

        var createEntity=await client.PostAsJsonAsync($"/api/sessions/{sessionId:D}/entities",new
        {
            definitionId="hero.blue",displayName="قهرمان آبی",controllerType="HumanTeam",behaviorProfileId=(string?)null,expectedVersion=2
        });
        createEntity.EnsureSuccessStatusCode();
        var entityId=(await Body(createEntity)).GetProperty("resourceId").GetGuid();
        (await client.PostAsJsonAsync($"/api/sessions/{sessionId:D}/assignments",new{entityId,teamId,expectedVersion=3})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/internal/sessions/{sessionId:D}/bootstrap",new
        {
            expectedVersion=4,
            metrics=new[]{new{scope="World",scopeId=sessionId,metricKey="chapter.progress",numericValue=0m}},
            memories=Array.Empty<object>(),relationships=Array.Empty<object>()
        })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/sessions/{sessionId:D}/start",new{expectedVersion=5})).EnsureSuccessStatusCode();
        (await client.GetAsync($"/api/sessions/{sessionId:D}")).EnsureSuccessStatusCode();
        (await client.GetAsync($"/api/sessions/{sessionId:D}/public")).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/sessions/{sessionId:D}/pause",new{expectedVersion=6})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/sessions/{sessionId:D}/resume",new{expectedVersion=7})).EnsureSuccessStatusCode();

        var stale=await client.PostAsJsonAsync($"/api/sessions/{sessionId:D}/pause",new{expectedVersion=6});
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        stale.Headers.Should().Contain(x=>x.Key=="X-Correlation-ID");
        (await client.GetAsync("/health")).EnsureSuccessStatusCode();
        (await client.GetAsync("/alive")).EnsureSuccessStatusCode();
        (await client.GetAsync("/ready")).EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response)
    {
        using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}

public sealed class HambaftApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_,configuration)=>configuration.AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["ConnectionStrings:Hambaft"]=connectionString,
            ["Jwt:SigningKey"]="integration-test-signing-key-32-bytes-minimum",
            ["Jwt:Issuer"]="Hambaft.IntegrationTests",
            ["Jwt:Audience"]="Hambaft.TestClients"
        }));
    }
}
