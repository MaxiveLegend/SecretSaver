using Microsoft.AspNetCore.Mvc;
using SecretSaver.Application;
using SecretSaver.Web.Models;

namespace SecretSaver.Web.Controllers;

public class SecretController(SecretService secretService) : Controller
{
    [HttpGet("/")]
    public IActionResult Create() => View(new CreateSecretViewModel());

    [HttpPost("/")]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Create(CreateSecretViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var id = await secretService.CreateAsync(model.Secret!, TimeSpan.FromHours(model.ExpiresInHours), model.MaxViews);

        return View("Created", new SecretCreatedViewModel
        {
            ShareUrl = Url.Action(nameof(Show), "Secret", new { id }, Request.Scheme)!,
            ExpiresInHours = model.ExpiresInHours,
            MaxViews = model.MaxViews
        });
    }

    [HttpGet("/s/{id:guid}")]
    public IActionResult Show(Guid id) => View(id);

    [HttpPost("/s/{id:guid}")]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Reveal(Guid id)
    {
        var secret = await secretService.RevealAsync(id);

        return secret is null ? View("Unavailable") : View("Reveal", secret);
    }
}
