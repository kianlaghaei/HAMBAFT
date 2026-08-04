using Hambaft.Application;
using Hambaft.Domain;
using JasperFx;
using Microsoft.AspNetCore.Mvc;

namespace Hambaft.Api.Middleware;

public sealed class ProblemDetailsMiddleware(RequestDelegate next,ILogger<ProblemDetailsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch(DomainException ex){await Write(context,422,"Domain rule rejected the command",ex.Message);}
        catch(SessionNotFoundException ex){await Write(context,404,"Session not found",ex.Message);}
        catch(StoryPackageNotFoundException ex){await Write(context,404,"Story Package not found",ex.Message);}
        catch(InvalidStoryPackageException ex){await Write(context,422,"Story Package validation failed",ex.Message,new Dictionary<string,object?>{{"errors",ex.Errors}});}
        catch(StoryPackageHashMismatchException ex){await Write(context,409,"Story Package hash mismatch",ex.Message,new Dictionary<string,object?>{{"expectedHash",ex.Expected},{"actualHash",ex.Actual}});}
        catch(ConcurrencyConflictException ex){await Write(context,409,"State version conflict",ex.Message,new Dictionary<string,object?>{{"expectedVersion",ex.ExpectedVersion},{"actualVersion",ex.ActualVersion}});}
        catch(EndingResolutionConflictException ex){await Write(context,409,"Ending resolution conflict",ex.Message);}
        catch(ConcurrencyException ex){logger.LogWarning(ex,"Marten stream concurrency conflict");await Write(context,409,"State version conflict","The Session changed concurrently. Reload its current state version and retry.");}
        catch(ArgumentException ex){await Write(context,400,"Invalid request",ex.Message);}
        catch(Exception ex){logger.LogError(ex,"Unhandled request failure for correlation {CorrelationId}",context.TraceIdentifier);await Write(context,500,"Internal server error","The request could not be processed.");}
    }
    private static async Task Write(HttpContext context,int status,string title,string detail,IReadOnlyDictionary<string,object?>? extensions=null)
    {
        if(context.Response.HasStarted)return;context.Response.StatusCode=status;context.Response.ContentType="application/problem+json";
        var problem=new ProblemDetails{Status=status,Title=title,Detail=detail,Instance=context.Request.Path};problem.Extensions["correlationId"]=context.TraceIdentifier;
        if(extensions is not null)foreach(var pair in extensions)problem.Extensions[pair.Key]=pair.Value;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
