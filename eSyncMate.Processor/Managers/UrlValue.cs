namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Escapes a data value (SKU, product id, order number, ...) before it is put into a URL path or query string.
    /// A raw [ ] / # ? &amp; + % or space in the value breaks the request (Walmart: "protocol.http.InvalidPath").
    /// Letters, digits, '-', '_', '.', '~' are left as they are, so ordinary ids give the same URL as before.
    /// </summary>
    public static class UrlValue
    {
        public static string Escape(object? value)
        {
            return Uri.EscapeDataString(Convert.ToString(value) ?? string.Empty);
        }
    }
}
