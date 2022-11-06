using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NameParser;
using Newtonsoft.Json;

namespace Omnics.NameParserFunctionApp;

public static class NameParserFunctions
{
    [FunctionName(nameof(ParseFullName))]
    [OpenApiOperation(nameof(ParseFullName), tags:"name parser", Summary = "Parses a full name",
        Description =
            "This parses a full name, e.g. \"Mr Fred Blogs\" or \"Blogs, Fred\", into a various parts, e.g. `title`, `firstName`, `lastName`.",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody(
        MediaTypeNames.Application.Json,
        typeof(ParseFullNameBody), Required = true, Description = "Structure containing the name to be parsed.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(Dictionary<string, string>),
        Description = "Broken out parts of the name")]
    public static async Task<IActionResult> ParseFullName(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequest req,
        ILogger log)
    {
        string name = req.Query["name"];

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        name ??= data?.name;
        if (string.IsNullOrWhiteSpace(name))
            return new BadRequestObjectResult(
                "Expected a non-empty value for a 'name' query parameter or 'name' property in a JSON object in the request body.");

        log.LogDebug("Received name: {name}", name);

        var humanName = new HumanName(name);

        return new OkObjectResult(humanName.AsDictionary());
        //.Select(dictEntry => $"{dictEntry.Key}: {dictEntry.Value}")
        //.Aggregate((soFar, next) => $"{soFar}\n{next}"));
    }
}

public record ParseFullNameBody(string Name);