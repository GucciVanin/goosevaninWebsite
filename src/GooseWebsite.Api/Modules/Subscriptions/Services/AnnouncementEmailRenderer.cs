using System.Net;
using GooseWebsite.Api.Shared.Email;
using GooseWebsite.Api.Shared.Announcements;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

/// <summary>What one announcement email says. All values are plain text (see <see cref="PublicText"/>).</summary>
internal sealed record AnnouncementContent(AnnouncedContentKind Kind, string Title, string Summary, string Url);

// Renders the "Gustavo published something new" email: the title, a short plain-text summary, the public link, and
// the personal unsubscribe link, in the shared brand frame. Everything a person wrote is HTML-encoded in the HTML
// view and appears only as text; the summary never becomes a link or markup.
internal static class AnnouncementEmailRenderer
{
    private const string Mono = BrandedEmailLayout.Mono;
    private const string Sans = BrandedEmailLayout.Sans;

    public static string Subject(AnnouncementContent content) => $"{KindWord(content.Kind, capital: true)}: {content.Title}";

    public static string RenderPlainText(string displayName, AnnouncementContent content, string unsubscribeUrl) =>
        $"Hi {displayName},\n\nGustavo published a new {KindWord(content.Kind, capital: false)}:\n\n{content.Title}\n\n{content.Summary}\n\n{ReadLabel(content.Kind)}: {content.Url}\n\n{BrandedEmailLayout.UnsubscribeSentence}\n{unsubscribeUrl}\n\nGustavo Couto Vanin";

    public static string RenderHtml(string displayName, AnnouncementContent content, string unsubscribeUrl, string logoUrl, string siteUrl)
    {
        var name = WebUtility.HtmlEncode(displayName);
        var title = WebUtility.HtmlEncode(content.Title);
        var summary = WebUtility.HtmlEncode(content.Summary);
        var link = WebUtility.HtmlEncode(content.Url);
        var eyebrow = WebUtility.HtmlEncode(KindWord(content.Kind, capital: true));
        var kind = KindWord(content.Kind, capital: false);
        var button = WebUtility.HtmlEncode(ReadLabel(content.Kind));

        var card = $$"""
      <div style="font-family:{{Mono}}; font-size:12px; letter-spacing:3px; text-transform:uppercase; color:#ff2bd6; padding-bottom:12px;">// New {{kind}}</div>
      <h1 style="font-family:{{Sans}}; font-weight:800; font-size:28px; line-height:34px; letter-spacing:1px; text-transform:uppercase; color:#ffffff; margin:0 0 16px 0; word-break:break-word;">{{title}}</h1>
      <p style="font-family:{{Sans}}; font-size:15px; line-height:24px; color:#c9cdf0; margin:0 0 16px 0;">Hi {{name}}, Gustavo published a new {{kind}}.</p>
      <p style="font-family:{{Sans}}; font-size:15px; line-height:24px; color:#c9cdf0; margin:0 0 20px 0;">{{summary}}</p>
      <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:6px 0 4px 0;"><tr>
        <td bgcolor="#0b0d1f" style="border:1px solid #19e6ff; border-left:3px solid #ff2bd6; border-bottom:3px solid #ff2bd6; background-color:#0b0d1f;">
          <a href="{{link}}" target="_blank" style="display:block; padding:15px 30px; font-family:{{Mono}}; font-size:13px; font-weight:bold; letter-spacing:3px; text-transform:uppercase; color:#ffffff; text-decoration:none;">{{button}} &rarr;</a>
        </td>
      </tr></table>
      <p style="font-family:{{Sans}}; font-size:13px; line-height:20px; color:#8b90b8; margin:22px 0 6px 0;">If the button does not work, copy this address into your browser:</p>
      <p style="font-family:{{Mono}}; font-size:12px; line-height:18px; color:#19e6ff; word-break:break-all; margin:0;">{{link}}</p>
""";

        return BrandedEmailLayout.Render(
            $"{eyebrow}: {content.Title}",
            $"{content.Title} - a new {kind} from Gustavo.",
            card,
            unsubscribeUrl,
            logoUrl,
            siteUrl);
    }

    private static string KindWord(AnnouncedContentKind kind, bool capital) => (kind, capital) switch
    {
        (AnnouncedContentKind.Story, true) => "New story",
        (AnnouncedContentKind.Story, false) => "story",
        (AnnouncedContentKind.Project, true) => "New project",
        _ => "project"
    };

    private static string ReadLabel(AnnouncedContentKind kind) => kind == AnnouncedContentKind.Story ? "Read the story" : "See the project";
}
