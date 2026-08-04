using Hambaft.Domain;

namespace Hambaft.Application;

public static class CommandFactory
{
    public static CreateWorldEntity CreateEntity(Guid sessionId,Guid entityId,string definitionId,string displayName,string controllerType,string? behaviorProfileId,long expectedVersion,CommandContext context)=>new(sessionId,entityId,definitionId,displayName,Parse<ControllerType>(controllerType),behaviorProfileId,expectedVersion,context);
    public static SetInitialMetric SetMetric(Guid sessionId,string scope,Guid scopeId,string key,decimal value,long expectedVersion,CommandContext context)=>new(sessionId,Parse<MetricScope>(scope),scopeId,key,value,expectedVersion,context);
    public static AddInitialMemory AddMemory(Guid sessionId,string scope,Guid scopeId,string key,string? json,string visibility,long expectedVersion,CommandContext context)=>new(sessionId,Parse<MemoryScope>(scope),scopeId,key,json,Parse<MemoryVisibility>(visibility),expectedVersion,context);
    private static T Parse<T>(string value) where T:struct,Enum=>Enum.TryParse<T>(value,true,out var parsed)?parsed:throw new ArgumentException($"Invalid {typeof(T).Name} value '{value}'.");
}
