using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using MyProject.Models;

namespace MyProject.Controllers;

public class ContactSurfaceController : SurfaceController
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ContactSurfaceController> _logger;

    public ContactSurfaceController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IEmailSender emailSender,
        ILogger<ContactSurfaceController> logger)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ContactFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return CurrentUmbracoPage();
        }

        var recipient = CurrentPage?.Value<string>("recipientEmail");
        if (string.IsNullOrWhiteSpace(recipient))
        {
            ModelState.AddModelError(string.Empty, "This form is not configured yet.");
            return CurrentUmbracoPage();
        }

        var body = $"From: {model.Name} <{model.Email}>\n\n{model.Message}";
        var message = new EmailMessage(null, recipient, "New message from the BrewHouse site", body, false);

        try
        {
            await _emailSender.SendAsync(message, "Contact");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send contact email");
            ModelState.AddModelError(string.Empty, "Sorry, we could not send your message. Please try again later.");
            return CurrentUmbracoPage();
        }

        TempData["ContactSent"] = true;
        return RedirectToCurrentUmbracoPage();
    }
}