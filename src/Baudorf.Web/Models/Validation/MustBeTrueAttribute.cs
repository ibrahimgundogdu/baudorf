using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Baudorf.Web.Models.Validation;

/// <summary>
/// Pflicht-Checkbox (z. B. DSGVO-/AGB-Zustimmung): der Wert muss <c>true</c> sein.
/// Clientseitig wird die "required"-Regel emittiert — die funktioniert für Checkboxen
/// zuverlässig, anders als <c>[Range(typeof(bool),"true","true")]</c>, das mit
/// jQuery-Unobtrusive bricht (min/max werden als Zahl geparst → NaN → immer ungültig).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool IsValid(object? value) => value is bool b && b;

    public void AddValidation(ClientModelValidationContext context)
    {
        var message = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-required", message);
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key)) attributes.Add(key, value);
    }
}
