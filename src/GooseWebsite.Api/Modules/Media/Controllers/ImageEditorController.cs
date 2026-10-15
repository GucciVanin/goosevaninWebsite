using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GooseWebsite.Api.Modules.Accounts.Authorization;
using GooseWebsite.Api.Modules.Media.Contracts;
using GooseWebsite.Api.Modules.Media.Services;

namespace GooseWebsite.Api.Modules.Media.Controllers;

// Gustavo's image uploads: administrators only, every write needs the CSRF token. Only JPEG, PNG and WebP files up
// to 5 MB are stored, under a generated name; anything else is refused before anything is written.
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/editor/images")]
public sealed class ImageEditorController(IImageService images) : ControllerBase
{
    // A little above the 5 MB image limit, to leave room for the multipart envelope.
    private const long RequestLimit = MediaOptions.MaxImageBytes + 64 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImageResponse>>> List() =>
        Ok((await images.ListAsync()).Select(ImageResponse.From).ToList());

    [ValidateAntiForgeryToken]
    [RequestSizeLimit(RequestLimit)]
    [HttpPost]
    public async Task<ActionResult<ImageResponse>> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Problem("Choose an image file to upload.", statusCode: StatusCodes.Status400BadRequest);
        }

        await using var content = file.OpenReadStream();
        var result = await images.UploadAsync(content, cancellationToken);
        return result.Outcome switch
        {
            ImageUploadOutcome.Stored => Created(string.Empty, ImageResponse.From(result.Image!)),
            ImageUploadOutcome.TooLarge => Problem("The image is larger than 5 MB.", statusCode: StatusCodes.Status413PayloadTooLarge),
            _ => Problem("Only JPEG, PNG and WebP images can be uploaded.", statusCode: StatusCodes.Status400BadRequest)
        };
    }

    [ValidateAntiForgeryToken]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await images.DeleteAsync(id) ? NoContent() : NotFound();
}
