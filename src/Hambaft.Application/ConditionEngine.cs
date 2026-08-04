using Hambaft.Domain;

namespace Hambaft.Application;

public sealed record ConditionEvaluationContext(StorySession State,StoryPackage Package,Guid? CurrentTeamId=null,Guid? CurrentEntityId=null,Guid? AssignmentId=null);
public sealed record ConditionTrace(ConditionType ConditionType,string ResolvedTarget,string? ActualValue,string? ExpectedValue,bool Result);
public sealed record ConditionEvaluationResult(bool Result,IReadOnlyList<ConditionTrace> Trace);

public interface IConditionEngine
{
    ConditionEvaluationResult Evaluate(ConditionDefinition condition,ConditionEvaluationContext context,bool includeTrace=false);
    ConditionEvaluationResult EvaluateAll(IEnumerable<ConditionDefinition> conditions,ConditionEvaluationContext context,bool includeTrace=false);
}

public sealed class ConditionEngine : IConditionEngine
{
    public ConditionEvaluationResult EvaluateAll(IEnumerable<ConditionDefinition> conditions,ConditionEvaluationContext context,bool includeTrace=false)
    {
        var traces=new List<ConditionTrace>(); var result=true;
        foreach(var condition in conditions) result=EvaluateCore(condition,context,traces)&&result;
        return new(result,includeTrace?traces:[]);
    }

    public ConditionEvaluationResult Evaluate(ConditionDefinition condition,ConditionEvaluationContext context,bool includeTrace=false)
    {
        var traces=new List<ConditionTrace>(); var result=EvaluateCore(condition,context,traces);
        return new(result,includeTrace?traces:[]);
    }

    private static bool EvaluateCore(ConditionDefinition c,ConditionEvaluationContext x,List<ConditionTrace> trace)
    {
        if(c.Type is ConditionType.All or ConditionType.Any)
        {
            var children=(c.Conditions??[]).Select(child=>EvaluateCore(child,x,trace)).ToList();
            var compositeResult=c.Type==ConditionType.All?children.All(v=>v):children.Any(v=>v);
            trace.Add(new(c.Type,"composite",string.Join(",",children.Select(v=>v.ToString())),c.Type==ConditionType.All?"all":"any",compositeResult)); return compositeResult;
        }
        if(c.Type==ConditionType.Not)
        {
            var nestedActual=c.Condition is not null&&EvaluateCore(c.Condition,x,trace); var notResult=!nestedActual;
            trace.Add(new(c.Type,"composite",nestedActual.ToString(),"false",notResult)); return notResult;
        }

        string target="session"; object? actual=null; object? expected=null; bool result;
        switch(c.Type)
        {
            case ConditionType.MetricAbove:
            case ConditionType.MetricAtLeast:
            case ConditionType.MetricBelow:
            case ConditionType.MetricAtMost:
            case ConditionType.MetricEquals:
                var metricTarget=ResolveMetricTarget(c,x); target=metricTarget.Description;
                actual=x.State.Metrics.SingleOrDefault(m=>m.Scope==metricTarget.Scope&&m.ScopeId==metricTarget.Id&&m.MetricKey==c.MetricKey)?.NumericValue;
                expected=c.Expected;
                result=actual is decimal a&&expected is decimal e&&c.Type switch
                {
                    ConditionType.MetricAbove=>a>e,ConditionType.MetricAtLeast=>a>=e,ConditionType.MetricBelow=>a<e,ConditionType.MetricAtMost=>a<=e,_=>a==e
                }; break;
            case ConditionType.MemoryExists:
            case ConditionType.MemoryMissing:
                var memoryTarget=ResolveMemoryTarget(c,x); target=memoryTarget.Description;
                actual=x.State.Memories.Any(m=>m.Scope==memoryTarget.Scope&&m.ScopeId==memoryTarget.Id&&m.Key==c.MemoryKey);
                expected=c.Type==ConditionType.MemoryExists;
                result=(bool)actual==(bool)expected; break;
            case ConditionType.RelationshipAbove:
            case ConditionType.RelationshipBelow:
                var pair=ResolveRelationship(c,x); target=$"entity:{pair.Source:D}->entity:{pair.Target:D}";
                actual=x.State.Relationships.SingleOrDefault(r=>r.SourceEntityId==pair.Source&&r.TargetEntityId==pair.Target&&r.RelationshipKey==c.RelationshipKey)?.NumericValue;
                expected=c.Expected; result=actual is decimal ra&&expected is decimal re&&(c.Type==ConditionType.RelationshipAbove?ra>re:ra<re); break;
            case ConditionType.EntityControllerIs:
                var controllerEntity=ResolveEntity(c,x); target=$"entity:{controllerEntity.Id:D}"; actual=controllerEntity.ControllerType; expected=c.Controller; result=c.Controller is not null&&controllerEntity.ControllerType==c.Controller; break;
            case ConditionType.EntityDefinitionIs:
                var definitionEntity=ResolveEntity(c,x); target=$"entity:{definitionEntity.Id:D}"; actual=definitionEntity.DefinitionId; expected=c.EntityDefinitionId; result=definitionEntity.DefinitionId==c.EntityDefinitionId; break;
            case ConditionType.TeamControlsEntityDefinition:
                var team=ResolveTeam(x); target=$"team:{team.Id:D}"; actual=x.State.Entities.SingleOrDefault(e=>e.ControlledByTeamId==team.Id)?.DefinitionId; expected=c.EntityDefinitionId; result=actual as string==c.EntityDefinitionId; break;
            case ConditionType.SessionStatusIs:
                actual=x.State.Status; expected=c.SessionStatus; result=c.SessionStatus is not null&&x.State.Status==c.SessionStatus; break;
            case ConditionType.CheckpointIs:
                actual=x.State.CurrentCheckpointId; expected=c.CheckpointId; result=x.State.CurrentCheckpointId==c.CheckpointId; break;
            case ConditionType.ChoiceWasSubmitted:
                target=x.AssignmentId is null?"session":$"assignment:{x.AssignmentId:D}"; actual=x.State.SubmittedChoices.Any(s=>(x.AssignmentId is null||s.AssignmentId==x.AssignmentId)&&s.ChoiceId==c.ChoiceId); expected=true; result=(bool)actual; break;
            case ConditionType.MetricDistributionAbove:
            case ConditionType.MetricDistributionBelow:
            case ConditionType.EntityCountWithMetricAbove:
                var metricValues=x.State.Entities.OrderBy(e=>e.DefinitionId,StringComparer.Ordinal).ThenBy(e=>e.Id)
                    .Select(e=>x.State.Metrics.SingleOrDefault(m=>m.Scope==MetricScope.Entity&&m.ScopeId==e.Id&&m.MetricKey==c.MetricKey)?.NumericValue).Where(v=>v is not null).Select(v=>v!.Value).ToList();
                var matching=metricValues.Count(v=>c.Type==ConditionType.MetricDistributionBelow?v<c.Expected:v>c.Expected);target="entity-distribution";actual=matching;expected=c.Count??1;result=matching>=(c.Count??1);break;
            case ConditionType.AgreementCountAtLeast:
                var agreementCount=x.State.Agreements.Count(a=>(c.AgreementTypeId is null||a.InteractionTypeId==c.AgreementTypeId)&&a.Status is AgreementStatus.Active or AgreementStatus.Executed);target="agreements";actual=agreementCount;expected=c.Count??1;result=agreementCount>=(c.Count??1);break;
            case ConditionType.AgreementTypeExists:
                target="agreements";actual=x.State.Agreements.Any(a=>a.InteractionTypeId==c.AgreementTypeId&&a.Status is AgreementStatus.Active or AgreementStatus.Executed);expected=true;result=(bool)actual;break;
            case ConditionType.RelationshipNetworkAverageAbove:
            case ConditionType.RelationshipNetworkMinimumAbove:
                var network=x.State.Relationships.Where(r=>r.RelationshipKey==c.RelationshipKey).OrderBy(r=>r.SourceEntityId).ThenBy(r=>r.TargetEntityId).Select(r=>r.NumericValue).ToList();
                decimal? networkValue=network.Count==0?null:c.Type==ConditionType.RelationshipNetworkAverageAbove?network.Average():network.Min();target="relationship-network";actual=networkValue;expected=c.Expected;result=networkValue is { } nv&&c.Expected is { } ne&&nv>ne;break;
            case ConditionType.EntityCountWithMemoryAtLeast:
                var memoryCount=x.State.Entities.OrderBy(e=>e.DefinitionId,StringComparer.Ordinal).Count(e=>x.State.Memories.Any(m=>m.Scope==MemoryScope.Entity&&m.ScopeId==e.Id&&m.Key==c.MemoryKey));target="entities-with-memory";actual=memoryCount;expected=c.Count??1;result=memoryCount>=(c.Count??1);break;
            case ConditionType.ProposalExpiredCountAtLeast:
                var expired=x.State.Proposals.Count(p=>p.Status==ProposalStatus.Expired);target="expired-proposals";actual=expired;expected=c.Count??1;result=expired>=(c.Count??1);break;
            case ConditionType.ConsequenceTriggered:
                target="consequences";actual=x.State.ScheduledConsequences.Any(s=>s.DefinitionId==c.ConsequenceId&&s.Status==ScheduledConsequenceStatus.Triggered);expected=true;result=(bool)actual;break;
            case ConditionType.AuthoredBehaviorActionWas:
                target="authored-behavior";actual=x.State.AuthoredBehaviorSelections.Any(s=>s.ActionId==c.BehaviorActionId);expected=true;result=(bool)actual;break;
            default: throw new DomainException($"Unsupported condition type {c.Type}.");
        }
        trace.Add(new(c.Type,target,Format(actual),Format(expected),result)); return result;
    }

    private static (MetricScope Scope,Guid Id,string Description) ResolveMetricTarget(ConditionDefinition c,ConditionEvaluationContext x)=>c.Scope switch
    {
        ConditionTargetScope.World=>(MetricScope.World,x.State.Id,$"world:{x.State.Id:D}"),
        ConditionTargetScope.CurrentTeam=>(MetricScope.Team,ResolveTeam(x).Id,$"team:{ResolveTeam(x).Id:D}"),
        ConditionTargetScope.CurrentEntity=>(MetricScope.Entity,ResolveEntity(c,x).Id,$"entity:{ResolveEntity(c,x).Id:D}"),
        ConditionTargetScope.ExplicitEntityDefinition=>(MetricScope.Entity,ResolveEntity(c,x).Id,$"entity:{ResolveEntity(c,x).Id:D}"),
        _=>throw new DomainException("Metric condition target scope cannot be resolved.")
    };
    private static (MemoryScope Scope,Guid Id,string Description) ResolveMemoryTarget(ConditionDefinition c,ConditionEvaluationContext x)=>c.Scope switch
    {
        ConditionTargetScope.World=>(MemoryScope.World,x.State.Id,$"world:{x.State.Id:D}"),
        ConditionTargetScope.CurrentTeam=>(MemoryScope.Team,ResolveTeam(x).Id,$"team:{ResolveTeam(x).Id:D}"),
        ConditionTargetScope.CurrentEntity=>(MemoryScope.Entity,ResolveEntity(c,x).Id,$"entity:{ResolveEntity(c,x).Id:D}"),
        ConditionTargetScope.ExplicitEntityDefinition=>(MemoryScope.Entity,ResolveEntity(c,x).Id,$"entity:{ResolveEntity(c,x).Id:D}"),
        _=>throw new DomainException("Memory condition target scope cannot be resolved.")
    };
    private static Team ResolveTeam(ConditionEvaluationContext x)=>x.CurrentTeamId is { } id?x.State.Teams.SingleOrDefault(t=>t.Id==id)??throw new DomainException("Current Team cannot be resolved."):throw new DomainException("Current Team is required.");
    private static WorldEntity ResolveEntity(ConditionDefinition c,ConditionEvaluationContext x)
    {
        if(c.Scope==ConditionTargetScope.ExplicitEntityDefinition||x.CurrentEntityId is null&&c.EntityDefinitionId is not null)
            return x.State.Entities.SingleOrDefault(e=>e.DefinitionId==c.EntityDefinitionId)??throw new DomainException("Explicit Entity definition cannot be resolved.");
        return x.CurrentEntityId is { } id?x.State.Entities.SingleOrDefault(e=>e.Id==id)??throw new DomainException("Current Entity cannot be resolved."):throw new DomainException("Current Entity is required.");
    }
    private static (Guid Source,Guid Target) ResolveRelationship(ConditionDefinition c,ConditionEvaluationContext x)
    {
        var source=ResolveEntity(c with { Scope=ConditionTargetScope.CurrentEntity },x);
        var target=x.State.Entities.SingleOrDefault(e=>e.DefinitionId==c.TargetEntityDefinitionId)??throw new DomainException("Relationship target Entity cannot be resolved.");
        if(source.Id==target.Id) throw new DomainException("Relationship source and target must differ."); return(source.Id,target.Id);
    }
    private static string? Format(object? value)=>value switch { null=>null,decimal d=>d.ToString(System.Globalization.CultureInfo.InvariantCulture),_=>value.ToString() };
}
