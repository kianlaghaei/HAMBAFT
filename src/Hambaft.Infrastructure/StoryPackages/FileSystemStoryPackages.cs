using System.Security.Cryptography;
using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hambaft.Application;
using Hambaft.Domain;

namespace Hambaft.Infrastructure.StoryPackages;

public sealed class StoryPackageOptions
{
    public const string SectionName="StoryPackages";
    public string Root { get; set; }="stories";
}

public sealed class DeterministicStoryPackageHasher : IStoryPackageHasher
{
    public async Task<string> ComputeAsync(string packageDirectory,CancellationToken ct)
    {
        var root=Path.GetFullPath(packageDirectory);
        if(!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        var files=Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories)
            .Select(path=>new { Path=path, Relative=Path.GetRelativePath(root,path).Replace('\\','/') })
            .OrderBy(x=>x.Relative,StringComparer.Ordinal).ToList();
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach(var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var pathBytes=Encoding.UTF8.GetBytes(file.Relative);
            AppendLength(hash,pathBytes.Length); hash.AppendData(pathBytes);
            var bytes=Path.GetExtension(file.Path).Equals(".json",StringComparison.OrdinalIgnoreCase)
                ? CanonicalizeJson(await File.ReadAllTextAsync(file.Path,ct))
                : Encoding.UTF8.GetBytes((await File.ReadAllTextAsync(file.Path,ct)).Replace("\r\n","\n").Replace('\r','\n'));
            AppendLength(hash,bytes.Length); hash.AppendData(bytes);
        }
        return "sha256:"+Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    private static void AppendLength(IncrementalHash hash,int length) { Span<byte> bytes=stackalloc byte[4]; BinaryPrimitives.WriteInt32LittleEndian(bytes,length); hash.AppendData(bytes); }

    private static byte[] CanonicalizeJson(string json)
    {
        using var document=JsonDocument.Parse(json);
        using var stream=new MemoryStream();
        using(var writer=new Utf8JsonWriter(stream,new JsonWriterOptions { Indented=false })) Write(writer,document.RootElement);
        return stream.ToArray();
    }

    private static void Write(Utf8JsonWriter writer,JsonElement element)
    {
        switch(element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach(var property in element.EnumerateObject().OrderBy(x=>x.Name,StringComparer.Ordinal)) { writer.WritePropertyName(property.Name); Write(writer,property.Value); }
                writer.WriteEndObject(); break;
            case JsonValueKind.Array:
                writer.WriteStartArray(); foreach(var item in element.EnumerateArray()) Write(writer,item); writer.WriteEndArray(); break;
            default: element.WriteTo(writer); break;
        }
    }
}

public sealed class FileSystemStoryPackageLoader : IStoryPackageLoader
{
    private static readonly string[] RequiredFiles=["manifest.json","metrics.json","entities.json","storylets.json","effects.json","narrative.json"];
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive=true,
        UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,
        Converters={new JsonStringEnumConverter()}
    };
    private readonly string root;
    private readonly IStoryPackageHasher hasher;
    private readonly IStoryPackageValidator validator;

    public FileSystemStoryPackageLoader(StoryPackageOptions options,IStoryPackageHasher hasher,IStoryPackageValidator validator)
    {
        root=Path.GetFullPath(options.Root); this.hasher=hasher; this.validator=validator;
    }

    public async Task<StoryPackage> LoadAsync(string packageId,string version,CancellationToken ct)
    {
        var safeId=DomainKeys.Normalize(packageId,nameof(packageId));
        var safeVersion=DomainKeys.Normalize(version,nameof(version));
        var directory=Path.GetFullPath(Path.Combine(root,safeId,safeVersion));
        if(!directory.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!Directory.Exists(directory)) throw new StoryPackageNotFoundException(packageId,version);
        var missing=RequiredFiles.Where(file=>!File.Exists(Path.Combine(directory,file))).Select(file=>new StoryPackageValidationError(file,null,"missing-file",$"Required package file '{file}' is missing.")).ToList();
        if(missing.Count>0) throw new InvalidStoryPackageException(missing);
        try
        {
            var manifest=await Read<StoryPackageManifest>(directory,"manifest.json",ct);
            var metrics=await Read<List<MetricDefinition>>(directory,"metrics.json",ct);
            var entities=await Read<List<EntityDefinition>>(directory,"entities.json",ct);
            var storylets=await Read<List<StoryletDefinition>>(directory,"storylets.json",ct);
            var effects=await Read<List<EffectDefinition>>(directory,"effects.json",ct);
            var narrative=await Read<Dictionary<string,IReadOnlyDictionary<string,NarrativeDefinition>>>(directory,"narrative.json",ct);
            var interactions=await ReadOptional(directory,"interactions.json",new List<InteractionTypeDefinition>(),ct);
            var behaviors=await ReadOptional(directory,"behaviors.json",new BehaviorCatalogDefinition([],[]),ct);
            var consequences=await ReadOptional(directory,"consequences.json",new List<ConsequenceDefinition>(),ct);
            var difficulties=await ReadOptional(directory,"difficulties.json",new List<DifficultyDefinition>(),ct);
            var package=new StoryPackage(manifest,metrics,entities,storylets,effects,narrative,await hasher.ComputeAsync(directory,ct),interactions,behaviors,consequences,difficulties);
            var errors=validator.Validate(package).Errors.ToList();
            if(!string.Equals(manifest.Id,safeId,StringComparison.Ordinal)||!string.Equals(manifest.Version,safeVersion,StringComparison.Ordinal)) errors.Add(new("manifest.json",manifest.Id,"manifest-path-mismatch","Manifest ID and version must match their package directory names."));
            if(errors.Count>0) throw new InvalidStoryPackageException(errors);
            return package;
        }
        catch(JsonException ex)
        {
            throw new InvalidStoryPackageException([new("package json",null,"invalid-json",ex.Message)]);
        }
    }

    private static async Task<T> Read<T>(string directory,string file,CancellationToken ct)
        =>JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(directory,file),ct),JsonOptions)
            ??throw new JsonException($"{file} contains null content.");
    private static async Task<T> ReadOptional<T>(string directory,string file,T fallback,CancellationToken ct)
        =>File.Exists(Path.Combine(directory,file))?await Read<T>(directory,file,ct):fallback;
}

public sealed class StoryPackageValidator : IStoryPackageValidator
{
    public StoryPackageValidationResult Validate(StoryPackage p)
    {
        var errors=new List<StoryPackageValidationError>();
        void Error(string file,string? id,string code,string message)=>errors.Add(new(file,id,code,message));
        var m=p.Manifest;
        if(string.IsNullOrWhiteSpace(m.Id)||string.IsNullOrWhiteSpace(m.Version)||string.IsNullOrWhiteSpace(m.Title)||string.IsNullOrWhiteSpace(m.Description)||string.IsNullOrWhiteSpace(m.EntryCheckpointId)||string.IsNullOrWhiteSpace(m.DefaultLocale)||m.SupportedLocales.Count==0||string.IsNullOrWhiteSpace(m.RequiredRuntimeVersion))
            Error("manifest.json",m.Id,"missing-manifest-field","All required manifest fields must be populated.");
        if(m.RequiredRuntimeVersion is not ("2.0" or "3.0")) Error("manifest.json",m.Id,"unsupported-runtime-version","requiredRuntimeVersion must be '2.0' or '3.0'.");
        if(!Version.TryParse(m.Version,out var packageVersion)||packageVersion.Major!=1) Error("manifest.json",m.Id,"unsupported-package-version","Phase 2 supports Story Package format versions with major version 1.");
        if(m.MinimumTeams<1||m.MaximumTeams<m.MinimumTeams) Error("manifest.json",m.Id,"invalid-team-range","minimumTeams must be positive and no greater than maximumTeams.");
        if(!m.SupportedLocales.Contains(m.DefaultLocale,StringComparer.OrdinalIgnoreCase)) Error("manifest.json",m.Id,"invalid-default-locale","defaultLocale must be included in supportedLocales.");
        Duplicates(p.Metrics.Select(x=>x.Key),"metrics.json","duplicate-metric-id");
        Duplicates(p.Entities.Select(x=>x.Id),"entities.json","duplicate-entity-id");
        Duplicates(p.Storylets.Select(x=>x.Id),"storylets.json","duplicate-storylet-id");
        Duplicates(p.Effects.Select(x=>x.Id),"effects.json","duplicate-effect-id");
        Duplicates(p.InteractionDefinitions.Select(x=>x.Id),"interactions.json","duplicate-interaction-id");
        Duplicates(p.BehaviorDefinitions.Profiles.Select(x=>x.Id),"behaviors.json","duplicate-behavior-profile-id");
        Duplicates(p.BehaviorDefinitions.Actions.Select(x=>x.Id),"behaviors.json","duplicate-behavior-action-id");
        Duplicates(p.ConsequenceDefinitions.Select(x=>x.Id),"consequences.json","duplicate-consequence-id");
        Duplicates(p.DifficultyDefinitions.Select(x=>x.Id),"difficulties.json","duplicate-difficulty-id");
        foreach(var metric in p.Metrics)
        {
            if(metric.Minimum>metric.Maximum||metric.DefaultValue<metric.Minimum||metric.DefaultValue>metric.Maximum) Error("metrics.json",metric.Key,"invalid-metric-range","Metric minimum, maximum and defaultValue form an impossible range.");
        }
        var metrics=p.Metrics.GroupBy(x=>(x.Scope,x.Key)).ToDictionary(x=>x.Key,x=>x.First());
        var entities=p.Entities.GroupBy(x=>x.Id,StringComparer.Ordinal).ToDictionary(x=>x.Key,x=>x.First(),StringComparer.Ordinal);
        var storylets=p.Storylets.GroupBy(x=>x.Id,StringComparer.Ordinal).ToDictionary(x=>x.Key,x=>x.First(),StringComparer.Ordinal);
        var effects=p.Effects.GroupBy(x=>x.Id,StringComparer.Ordinal).ToDictionary(x=>x.Key,x=>x.First(),StringComparer.Ordinal);
        if(!p.Storylets.Any(x=>x.CheckpointId==m.EntryCheckpointId)) Error("manifest.json",m.EntryCheckpointId,"missing-entry-checkpoint","No Storylet uses the entry checkpoint.");
        foreach(var entity in p.Entities)
            foreach(var initial in entity.InitialMetrics)
                if(!metrics.ContainsKey((MetricScope.Entity,initial.Key))) Error("entities.json",entity.Id,"unknown-metric",$"Initial metric '{initial.Key}' is not an Entity metric.");
        foreach(var storylet in p.Storylets)
        {
            if(storylet.Weight<1) Error("storylets.json",storylet.Id,"invalid-weight","Storylet weight must be positive.");
            if(!ValidTarget(storylet.TargetSelector,entities)) Error("storylets.json",storylet.Id,"invalid-target-selector","Target selector is invalid or references a missing Entity definition.");
            if(!HasNarrative(p,storylet.NarrativeRef)) Error("storylets.json",storylet.Id,"missing-narrative-reference",$"Narrative reference '{storylet.NarrativeRef}' is missing.");
            Duplicates(storylet.Choices.Select(x=>x.Id),"storylets.json","duplicate-choice-id",storylet.Id);
            foreach(var condition in storylet.Conditions) ValidateCondition(condition,storylet.Id,metrics,entities,Error);
            foreach(var choice in storylet.Choices)
            {
                if(!HasNarrative(p,choice.LabelRef)) Error("storylets.json",storylet.Id,"invalid-choice-reference",$"Choice label reference '{choice.LabelRef}' is missing.");
                foreach(var effectId in choice.EffectIds) if(!effects.ContainsKey(effectId)) Error("storylets.json",storylet.Id,"unknown-effect",$"Choice references unknown Effect '{effectId}'.");
                if(choice.NextStoryletHint is not null&&!storylets.ContainsKey(choice.NextStoryletHint)) Error("storylets.json",storylet.Id,"missing-storylet-reference",$"Choice references unknown next Storylet '{choice.NextStoryletHint}'.");
            }
            if(storylet.RequiredResponse&&storylet.Scope is StoryletScope.TeamPrivate or StoryletScope.EntityPrivate&&!HasEligibleTarget(storylet,p.Entities)) Error("storylets.json",storylet.Id,"no-eligible-target","Required private Storylet has no eligible target definition.");
        }
        foreach(var effect in p.Effects) ValidateEffect(effect,p,metrics,entities,storylets,Error);
        ValidatePhase3(p,effects,entities,Error);
        var entryRequired=p.Storylets.Where(x=>x.CheckpointId==m.EntryCheckpointId&&x.RequiredResponse).ToList();
        if(entryRequired.Count>0&&(entryRequired.Any(x=>string.IsNullOrWhiteSpace(x.NextCheckpointId))||!entryRequired.Select(x=>x.NextCheckpointId).Distinct(StringComparer.Ordinal).Any(next=>p.Storylets.Any(s=>s.CheckpointId==next))))
            Error("storylets.json",m.EntryCheckpointId,"checkpoint-no-exit","Mandatory entry checkpoint progression has no possible next Storylet.");
        return new(errors);

        void Duplicates(IEnumerable<string> ids,string file,string code,string? owner=null)
        {
            foreach(var id in ids.GroupBy(x=>x,StringComparer.Ordinal).Where(x=>x.Count()>1).Select(x=>x.Key)) Error(file,owner??id,code,$"Duplicate ID '{id}'.");
        }
    }

    private static bool HasNarrative(StoryPackage p,string key)=>p.Narrative.TryGetValue(p.Manifest.DefaultLocale,out var locale)&&locale.ContainsKey(key);
    private static bool ValidTarget(TargetSelectorDefinition selector,IReadOnlyDictionary<string,EntityDefinition> entities)=>selector.Type switch
    {
        TargetSelectorType.AllTeams or TargetSelectorType.World=>selector.EntityDefinitionId is null,
        TargetSelectorType.TeamControllingEntityDefinition or TargetSelectorType.EntityDefinition=>selector.EntityDefinitionId is not null&&entities.ContainsKey(selector.EntityDefinitionId),
        _=>false
    };
    private static bool HasEligibleTarget(StoryletDefinition s,IReadOnlyList<EntityDefinition> entities)=>s.TargetSelector.Type switch
    {
        TargetSelectorType.AllTeams=>true,
        TargetSelectorType.TeamControllingEntityDefinition=>entities.Any(x=>x.Id==s.TargetSelector.EntityDefinitionId&&x.ControllerRequirement is ControllerRequirement.HumanTeam or ControllerRequirement.Any),
        TargetSelectorType.EntityDefinition=>entities.Any(x=>x.Id==s.TargetSelector.EntityDefinitionId),
        _=>false
    };
    private static void ValidateCondition(ConditionDefinition c,string owner,IReadOnlyDictionary<(MetricScope,string),MetricDefinition> metrics,IReadOnlyDictionary<string,EntityDefinition> entities,Action<string,string?,string,string> error)
    {
        if(c.Type is ConditionType.All or ConditionType.Any)
        {
            if(c.Conditions is null||c.Conditions.Count==0) error("storylets.json",owner,"invalid-condition","All and Any require non-empty conditions.");
            else foreach(var child in c.Conditions) ValidateCondition(child,owner,metrics,entities,error);
            return;
        }
        if(c.Type==ConditionType.Not) { if(c.Condition is null) error("storylets.json",owner,"invalid-condition","Not requires condition."); else ValidateCondition(c.Condition,owner,metrics,entities,error); return; }
        if(c.Type is ConditionType.MetricAbove or ConditionType.MetricAtLeast or ConditionType.MetricBelow or ConditionType.MetricAtMost or ConditionType.MetricEquals)
        {
            var scope=MetricScopeFor(c.Scope);
            if(scope is null||c.MetricKey is null||!metrics.ContainsKey((scope.Value,c.MetricKey))||c.Expected is null) error("storylets.json",owner,"unknown-metric","Metric condition has an invalid scope, key or expected value.");
        }
        if(c.Type is ConditionType.MemoryExists or ConditionType.MemoryMissing) { if(c.Scope is null||string.IsNullOrWhiteSpace(c.MemoryKey)) error("storylets.json",owner,"invalid-condition","Memory condition requires an explicit scope and memoryKey."); }
        if(c.Scope==ConditionTargetScope.ExplicitEntityDefinition&&(c.EntityDefinitionId is null||!entities.ContainsKey(c.EntityDefinitionId))) error("storylets.json",owner,"missing-entity-reference","Condition references a missing Entity definition.");
        if(c.Type is ConditionType.RelationshipAbove or ConditionType.RelationshipBelow&&(c.TargetEntityDefinitionId is null||!entities.ContainsKey(c.TargetEntityDefinitionId))) error("storylets.json",owner,"missing-entity-reference","Relationship condition requires a valid target Entity definition.");
    }
    private static void ValidateEffect(EffectDefinition e,StoryPackage p,IReadOnlyDictionary<(MetricScope,string),MetricDefinition> metrics,IReadOnlyDictionary<string,EntityDefinition> entities,IReadOnlyDictionary<string,StoryletDefinition> storylets,Action<string,string?,string,string> error)
    {
        if(e.Type is EffectType.ChangeMetric or EffectType.SetMetric)
        {
            var scope=MetricScopeFor(e.Scope);
            if(scope is null||e.MetricKey is null||!metrics.ContainsKey((scope.Value,e.MetricKey))||e.Value is null) error("effects.json",e.Id,"unknown-metric","Metric effect has an invalid scope, metric key or value.");
        }
        if(e.Type is EffectType.AddMemory or EffectType.RemoveMemory)
        {
            try { DomainKeys.Normalize(e.MemoryKey??string.Empty,nameof(e.MemoryKey)); } catch(DomainException) { error("effects.json",e.Id,"invalid-memory-key","Memory effect requires a valid memory key."); }
            if(e.Scope is null) error("effects.json",e.Id,"invalid-effect-scope","Memory effect requires an explicit target scope.");
        }
        if(e.Type==EffectType.ChangeRelationship&&(e.TargetEntityDefinitionId is null||!entities.ContainsKey(e.TargetEntityDefinitionId)||string.IsNullOrWhiteSpace(e.RelationshipKey)||e.Value is null)) error("effects.json",e.Id,"invalid-relationship-target","Relationship effect requires resolvable source, target, key and value.");
        if(e.Type==EffectType.AssignStorylet&&(e.StoryletId is null||!storylets.ContainsKey(e.StoryletId))) error("effects.json",e.Id,"missing-storylet-reference","AssignStorylet references a missing Storylet.");
        if(e.Type==EffectType.PublishWorldNarrative)
        {
            if(e.StoryletId is null||!storylets.TryGetValue(e.StoryletId,out var target)) error("effects.json",e.Id,"missing-storylet-reference","PublishWorldNarrative references a missing Storylet.");
            else if(target.Scope!=StoryletScope.WorldPublic) error("effects.json",e.Id,"private-public-leak","A private Storylet cannot be published as World narrative.");
        }
        if(e.Type==EffectType.ScheduleConsequence&&(e.ConsequenceDefinitionId is null||!p.ConsequenceDefinitions.Any(x=>x.Id==e.ConsequenceDefinitionId))) error("effects.json",e.Id,"missing-consequence-reference","ScheduleConsequence references a missing Consequence definition.");
        if(e.Type==EffectType.CancelConsequence&&e.ConsequenceDefinitionId is not null&&!p.ConsequenceDefinitions.Any(x=>x.Id==e.ConsequenceDefinitionId)) error("effects.json",e.Id,"missing-consequence-reference","CancelConsequence references a missing Consequence definition.");
    }
    private static void ValidatePhase3(StoryPackage p,IReadOnlyDictionary<string,EffectDefinition> effects,IReadOnlyDictionary<string,EntityDefinition> entities,Action<string,string?,string,string> error)
    {
        foreach(var interaction in p.InteractionDefinitions)
        {
            if(string.IsNullOrWhiteSpace(interaction.DisplayNameRef)||string.IsNullOrWhiteSpace(interaction.DescriptionRef))error("interactions.json",interaction.Id,"missing-interaction-field","Interaction display and description references are required.");
            if(!HasNarrative(p,interaction.DisplayNameRef)||!HasNarrative(p,interaction.DescriptionRef))error("interactions.json",interaction.Id,"missing-narrative-reference","Interaction narrative references are missing.");
            if(interaction.AllowedSenderSelectors.Count==0||interaction.AllowedReceiverSelectors.Count==0||interaction.AllowedSenderSelectors.Any(x=>!ValidTarget(x,entities))||interaction.AllowedReceiverSelectors.Any(x=>!ValidTarget(x,entities)))error("interactions.json",interaction.Id,"invalid-target-selector","Interaction sender and receiver selectors must resolve to Teams.");
            foreach(var effectId in interaction.OfferedEffectIds.Concat(interaction.RequestedEffectIds))if(!effects.ContainsKey(effectId))error("interactions.json",interaction.Id,"unknown-effect",$"Interaction references unknown Effect '{effectId}'.");
            if(interaction.DefaultValidity.Type==ProposalValidityType.ValidForCheckpointCount&&interaction.DefaultValidity.CheckpointCount is not >0)error("interactions.json",interaction.Id,"invalid-validity","Checkpoint-count validity must be positive.");
        }
        var actions=p.BehaviorDefinitions.Actions.ToDictionary(x=>x.Id,StringComparer.Ordinal);
        foreach(var action in actions.Values)
        {
            foreach(var effectId in action.EffectIds)if(!effects.ContainsKey(effectId))error("behaviors.json",action.Id,"unknown-effect",$"Behavior Action references unknown Effect '{effectId}'.");
            if(action.NarrativeRef is not null&&!HasNarrative(p,action.NarrativeRef))error("behaviors.json",action.Id,"missing-narrative-reference","Behavior Action narrative is missing.");
        }
        foreach(var profile in p.BehaviorDefinitions.Profiles)
        {
            if(profile.EligibleEntityDefinitions.Any(x=>!entities.ContainsKey(x)))error("behaviors.json",profile.Id,"missing-entity-reference","Behavior Profile references a missing Entity definition.");
            if(!actions.ContainsKey(profile.FallbackActionId))error("behaviors.json",profile.Id,"missing-action-reference","Behavior fallback Action is missing.");
            foreach(var rule in profile.Rules){if(rule.Weight<1)error("behaviors.json",rule.Id,"invalid-weight","Behavior rule weight must be positive.");if(!actions.ContainsKey(rule.ActionId))error("behaviors.json",rule.Id,"missing-action-reference","Behavior rule Action is missing.");foreach(var condition in rule.Conditions)ValidateCondition(condition,rule.Id,p.Metrics.GroupBy(x=>(x.Scope,x.Key)).ToDictionary(x=>x.Key,x=>x.First()),entities,error);}
        }
        foreach(var consequence in p.ConsequenceDefinitions)
        {
            foreach(var effectId in consequence.EffectIds)if(!effects.ContainsKey(effectId))error("consequences.json",consequence.Id,"unknown-effect",$"Consequence references unknown Effect '{effectId}'.");
            if(consequence.NarrativeRef is not null&&!HasNarrative(p,consequence.NarrativeRef))error("consequences.json",consequence.Id,"missing-narrative-reference","Consequence narrative is missing.");
            if(consequence.Trigger.Type==ConsequenceTriggerType.AfterCheckpointCount&&consequence.Trigger.CheckpointCount is not >0)error("consequences.json",consequence.Id,"invalid-trigger","AfterCheckpointCount requires a positive checkpointCount.");
            if(consequence.Trigger.Type==ConsequenceTriggerType.AtCheckpoint&&(string.IsNullOrWhiteSpace(consequence.Trigger.CheckpointId)||!p.Storylets.Any(x=>x.CheckpointId==consequence.Trigger.CheckpointId)))error("consequences.json",consequence.Id,"invalid-trigger","AtCheckpoint requires a known checkpoint.");
        }
        foreach(var entity in p.Entities.Where(x=>x.ControllerRequirement==ControllerRequirement.AuthoredBehavior))if(!p.BehaviorDefinitions.Profiles.Any(x=>x.EligibleEntityDefinitions.Contains(entity.Id,StringComparer.Ordinal)))error("behaviors.json",entity.Id,"missing-behavior-profile","AuthoredBehavior Entity has no eligible Behavior Profile.");
    }
    private static MetricScope? MetricScopeFor(ConditionTargetScope? scope)=>scope switch
    {
        ConditionTargetScope.World=>MetricScope.World,
        ConditionTargetScope.CurrentTeam=>MetricScope.Team,
        ConditionTargetScope.CurrentEntity or ConditionTargetScope.ExplicitEntityDefinition=>MetricScope.Entity,
        ConditionTargetScope.RelationshipBetweenCurrentAndTarget=>MetricScope.Relationship,
        _=>null
    };
}

public sealed class FileSystemStoryPackageCatalog(StoryPackageOptions options,IStoryPackageLoader loader) : IStoryPackageCatalog
{
    public async Task<IReadOnlyList<StoryPackageMetadata>> ListAsync(CancellationToken ct)
    {
        var root=Path.GetFullPath(options.Root); if(!Directory.Exists(root)) return [];
        var result=new List<StoryPackageMetadata>();
        foreach(var versionDirectory in Directory.EnumerateDirectories(root).SelectMany(Directory.EnumerateDirectories).OrderBy(x=>x,StringComparer.Ordinal))
        {
            var version=Path.GetFileName(versionDirectory); var id=Path.GetFileName(Path.GetDirectoryName(versionDirectory))!;
            try
            {
                var package=await loader.LoadAsync(id,version,ct); var m=package.Manifest;
                result.Add(new(m.Id,m.Version,m.Title,m.Description,m.MinimumTeams,m.MaximumTeams,m.EstimatedDurationMinutes,true,package.ContentHash,[]));
            }
            catch(InvalidStoryPackageException ex) { result.Add(new(id,version,id,string.Empty,0,0,0,false,string.Empty,ex.Errors)); }
        }
        return result.OrderBy(x=>x.Id,StringComparer.Ordinal).ThenBy(x=>x.Version,StringComparer.Ordinal).ToList();
    }
}
