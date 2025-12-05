using DeuEposta.Models.Enums;
using HtmlAgilityPack;
using System.ComponentModel.DataAnnotations;

namespace DeuEposta.Attributes;

/// <summary>
/// HTML içeriğini XSS saldırılarına karşı koruyan validation attribute
/// Duyuru ve şablon içeriklerinde kullanılır
/// </summary>
public class SafeHtmlAttribute : ValidationAttribute
{
    private readonly string[] _allowedTags;
    private readonly string[] _allowedAttributes;

    public SafeHtmlAttribute(string[]? allowedTags = null, string[]? allowedAttributes = null)
    {
        _allowedTags = allowedTags ?? new[] { "p", "br", "strong", "em", "u", "h1", "h2", "h3", "h4", "ul", "ol", "li" };
        _allowedAttributes = allowedAttributes ?? new[] { "style" };
        ErrorMessage = "İçerik güvenli olmayan HTML elementi içeriyor.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
            return ValidationResult.Success;

        var html = value.ToString() ?? string.Empty;

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Tehlikeli tagları kontrol et
            var dangerousTags = new[] { "script", "iframe", "object", "embed", "link", "meta", "style" };
            foreach (var tag in dangerousTags)
            {
                if (doc.DocumentNode.Descendants(tag).Any())
                {
                    return new ValidationResult($"Güvenli olmayan HTML tag tespit edildi: <{tag}>");
                }
            }

            // Tehlikeli attributeleri kontrol et
            var dangerousAttributes = new[] { "onclick", "onload", "onerror", "onmouseover", "onfocus" };
            foreach (var node in doc.DocumentNode.Descendants())
            {
                foreach (var attr in node.Attributes)
                {
                    if (dangerousAttributes.Contains(attr.Name.ToLower()) ||
                        attr.Name.ToLower().StartsWith("on"))
                    {
                        return new ValidationResult($"Güvenli olmayan HTML attribute tespit edildi: {attr.Name}");
                    }

                    // JavaScript içeren href/src kontrolleri
                    if ((attr.Name.ToLower() == "href" || attr.Name.ToLower() == "src") &&
                        attr.Value.ToLower().Contains("javascript:"))
                    {
                        return new ValidationResult("JavaScript URL tespit edildi");
                    }
                }
            }

            return ValidationResult.Success;
        }
        catch
        {
            return new ValidationResult("HTML içeriği parse edilemedi");
        }
    }
}

/// <summary>
/// Duyuru durum değerlerinin geçerliliğini kontrol eden validation attribute
/// </summary>
public class DuyuruDurumValidationAttribute : ValidationAttribute
{
    public DuyuruDurumValidationAttribute()
    {
        ErrorMessage = "Geçersiz durum değeri.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
            return ValidationResult.Success;

        var durum = value.ToString() ?? string.Empty;

        if (!DuyuruDurum.IsValidDurum(durum))
        {
            var allowedValues = string.Join(", ", DuyuruDurum.TumDurumlar);
            return new ValidationResult($"Geçersiz durum değeri: '{durum}'. İzin verilen değerler: {allowedValues}");
        }

        return ValidationResult.Success;
    }
}