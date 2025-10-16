using HtmlAgilityPack;
using System.Web;

namespace AcsConversationGateway.Function.Helpers;

public static class EmailBodyHelper
{
    public static string ConvertHtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Load HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove <style>, <script>, <head>, etc.
        var nodesToRemove = doc.DocumentNode.SelectNodes("//style|//script|//head");
        if (nodesToRemove != null)
        {
            foreach (var node in nodesToRemove)
                node.Remove();
        }

        // Replace <br> and <p> with newlines to preserve structure
        var brNodes = doc.DocumentNode.SelectNodes("//br");
        if (brNodes != null)
        {
            foreach (var br in brNodes)
                br.ParentNode.ReplaceChild(HtmlNode.CreateNode("\n"), br);
        }

        var pNodes = doc.DocumentNode.SelectNodes("//p");
        if (pNodes != null)
        {
            foreach (var p in pNodes)
            {
                p.InnerHtml += "\n";
            }
        }

        // Extract text
        var text = doc.DocumentNode.InnerText;

        // Decode HTML entities (&nbsp;, &#128522;, etc.)
        text = HttpUtility.HtmlDecode(text);

        // Normalize line endings
        text = text.Replace("\r", "").Replace("\n\n", "\n").Trim();

        return text;
    }
}