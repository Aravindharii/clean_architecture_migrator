using LibraryManagement.Api.Common.ETag;
using LibraryManagement.Api.Common.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryManagement.Api.Filters;

public class ETagResultFilter : IAsyncResultFilter
{
    private readonly IETagService _etagService;

    public ETagResultFilter(IETagService etagService)
    {
        _etagService = etagService;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method))
        {
            var etag = ComputeETag(context.Result);

            if (etag is not null)
            {
                context.HttpContext.Response.Headers.ETag = etag;

                var ifNoneMatch = context.HttpContext.Request.Headers.IfNoneMatch.ToString();
                if (_etagService.MatchesAny(ifNoneMatch, etag, weak: true))
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
                }
            }
        }

        await next();
    }

    private string? ComputeETag(IActionResult result)
    {
        if (result is not ObjectResult { StatusCode: null or StatusCodes.Status200OK } objectResult)
            return null;

        if (objectResult.Value is IApiResponse response)
            return _etagService.ComputeForPayload(response.Data);

        return _etagService.ComputeForPayload(objectResult.Value);
    }
}
