using System.Net;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using KABILIN.Models;

namespace KABILIN.Helpers
{
    public static class UiHelpers
    {
        /// <summary>Renders a Lucide icon from the local sprite.</summary>
        public static IHtmlContent Icon(this IHtmlHelper html, string name, string? cssClass = null)
        {
            var basePath = html.ViewContext.HttpContext.Request.PathBase.Value ?? string.Empty;
            var cls = string.IsNullOrWhiteSpace(cssClass) ? "icon" : $"icon {cssClass}";
            return new HtmlString(
                $"<svg class=\"{cls}\" aria-hidden=\"true\" focusable=\"false\"><use href=\"{basePath}/lib/lucide/sprite.svg#{name}\"></use></svg>");
        }

        public static string StatusSlug(string? statusName) =>
            (statusName ?? "unknown").ToLowerInvariant().Replace(' ', '-');

        public static IHtmlContent StatusBadge(this IHtmlHelper html, string? statusName) =>
            new HtmlString($"<span class=\"badge badge-{StatusSlug(statusName)}\">{WebUtility.HtmlEncode(statusName ?? "Unknown")}</span>");

        public static IHtmlContent VerificationBadge(this IHtmlHelper html, string state)
        {
            var (cls, label) = state switch
            {
                VerificationStates.Verified => ("badge-completed", "Verified"),
                VerificationStates.Incomplete => ("badge-incomplete-requirements", "Needs correction"),
                _ => ("badge-submitted", "Awaiting review")
            };
            return new HtmlString($"<span class=\"badge {cls}\">{label}</span>");
        }

        public static string TypeIcon(int typeId) => typeId switch
        {
            RequestTypeIds.DeathCertificate => "file-text",
            RequestTypeIds.BurialPermit => "landmark",
            RequestTypeIds.BenefitsAssistance => "hand-heart",
            RequestTypeIds.InsuranceClaims => "shield-check",
            _ => "file"
        };

        public static string Initials(string? fullName)
        {
            var parts = (fullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][..1].ToUpperInvariant();
            return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
        }
    }
}
