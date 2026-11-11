using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SecretSaver.Web.Models;

namespace SecretSaver.Web.Controllers;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorController : Controller
{
    [Route("/error")]
    public IActionResult Error() =>
        View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [Route("/error/{statusCode:int}")]
    public IActionResult StatusCodePage(int statusCode) =>
        statusCode == StatusCodes.Status404NotFound ? View("NotFound") : Error();
}
