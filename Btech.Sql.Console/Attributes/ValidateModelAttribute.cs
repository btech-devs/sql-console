using Btech.Sql.Console.Models.Responses.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Btech.Sql.Console.Attributes;

public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new OkObjectResult(
                new Response<List<string>>
                {
                    ValidationErrorMessages = context.ModelState
                        .Where(state =>
                            state.Value?.ValidationState == ModelValidationState.Invalid &&
                            state.Key.Any())
                        .Select(state =>
                            new KeyValuePair<string, string>(
                                key: state.Key.ToLower(),
                                value: string.Join(',', state.Value.Errors.Select(field => field.ErrorMessage))))
                        .ToDictionary(pair => pair.Key, x => x.Value)
                });
        }
    }
}