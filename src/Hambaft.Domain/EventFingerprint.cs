using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Hambaft.Domain;

public static class EventFingerprint
{
    public static string Compute(IEnumerable<IDomainEvent> events)
    {
        var canonical = string.Join("\n", events.Select(Canonical));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static string Canonical(IDomainEvent value)
    {
        var body = value switch
        {
            SessionCreated e => Join(e.Id,e.StoryPackageId,e.StoryVersion,e.ContentHash,e.Seed),
            TeamAdded e => Join(e.TeamId,e.DisplayName,e.PairingCodeHash),
            WorldEntityCreated e => Join(e.EntityId,e.DefinitionId,e.DisplayName,e.ControllerType,e.BehaviorProfileId,e.Status),
            EntityAssignedToTeam e => Join(e.EntityId,e.TeamId),
            InitialMetricSet e => Join(e.Scope,e.ScopeId,e.MetricKey,e.NumericValue),
            InitialMemoryAdded e => Join(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility),
            InitialRelationshipSet e => Join(e.SourceEntityId,e.TargetEntityId,e.RelationshipKey,e.NumericValue),
            SessionStarted => string.Empty, SessionPaused => string.Empty, SessionResumed => string.Empty, SessionCancelled => string.Empty,
            _ => throw new DomainException($"Unsupported fingerprint event {value.GetType().Name}.")
        };
        var m=value.Metadata;
        return $"{value.GetType().Name}|{body}|meta:{m.CorrelationId:D}|{m.CausationId:D}|{m.CommandId:D}|{m.SessionId:D}|{m.TeamId?.ToString("D")}|{m.OccurredAtUtc.UtcDateTime:O}";
    }
    private static string Join(params object?[] values) => string.Join("|", values.Select(x => x switch { null=>"", decimal d=>d.ToString(CultureInfo.InvariantCulture), IFormattable f=>f.ToString(null,CultureInfo.InvariantCulture), _=>x.ToString() }));
}
