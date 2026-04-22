using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace MySolution.MyComponentsLib.Components;

public abstract class MyFormPageBase<TModel> : ComponentBase where TModel : class
{
    [Inject]
    public NavigationManager NavManager { get; set; } = default!;

    [Parameter]
    public TModel? Model { get; set; }

    protected string? SuccessMessage { get; set; }
    protected TModel? ActiveModel;
    protected EditContext EditContext = default!;
    protected readonly List<FieldMeta> Fields = new();

    protected string GetBaseFormTitle() => $"{typeof(TModel).Name} Form";
    protected string GetBaseSubmitButtonText() => "Submit";

    protected override void OnInitialized() => InitializeForm();
    protected override void OnParametersSet() => InitializeForm();

    private void InitializeForm()
    {
        var nextModel = Model ?? CreateDefaultModel();
        if (ActiveModel != null && ReferenceEquals(ActiveModel, nextModel)) return;

        ActiveModel = nextModel;
        EditContext = new EditContext(ActiveModel);
        BuildFieldMetadata();
    }

    protected void GoToHome() => NavManager.NavigateTo("/");
    protected virtual TModel CreateDefaultModel() => Activator.CreateInstance<TModel>();
    protected virtual Task OnValidModelSubmitAsync(TModel model) => Task.CompletedTask;

    protected async Task HandleValidSubmit()
    {
        if (ActiveModel is not null) await OnValidModelSubmitAsync(ActiveModel);
    }

    private void BuildFieldMetadata()
    {
        Fields.Clear();
        if (ActiveModel is null) return;

        foreach (var property in typeof(TModel).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite) continue;

            var label = property.GetCustomAttribute<DisplayAttribute>()?.Name ?? property.Name;
            var dataType = property.GetCustomAttribute<DataTypeAttribute>()?.DataType;
            var isEmail = property.GetCustomAttribute<EmailAddressAttribute>() is not null;
            var isPhone = property.GetCustomAttribute<PhoneAttribute>() is not null;

            var kind = ResolveInputKind(property.PropertyType, dataType, isEmail, isPhone);
            Fields.Add(new FieldMeta
            {
                Property = property,
                Label = label,
                Kind = kind,
                HtmlType = ResolveHtmlType(kind),
                FieldIdentifier = new FieldIdentifier(ActiveModel, property.Name)
            });
        }
    }

    private static InputKind ResolveInputKind(Type type, DataType? dt, bool email, bool phone)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (t == typeof(bool)) return InputKind.Checkbox;
        if (dt == DataType.MultilineText) return InputKind.TextArea;
        if (t == typeof(DateTime)) return InputKind.Date;
        if (IsNumber(t)) return InputKind.Number;
        if (email) return InputKind.Email;
        if (phone) return InputKind.Phone;
        return InputKind.Text;
    }

    private static bool IsNumber(Type t) => t == typeof(int) || t == typeof(long) || t == typeof(decimal) || t == typeof(double) || t == typeof(float);
    private static string ResolveHtmlType(InputKind k) => k switch { InputKind.Email => "email", InputKind.Phone => "tel", InputKind.Number => "number", InputKind.Date => "date", _ => "text" };

    protected string GetDisplayValue(FieldMeta f)
    {
        if (ActiveModel is null) return "";
        var v = f.Property.GetValue(ActiveModel);
        if (v is null) return "";
        return f.Kind == InputKind.Date && v is DateTime d ? d.ToString("yyyy-MM-dd") : Convert.ToString(v, CultureInfo.InvariantCulture)!;
    }

    protected bool GetBoolValue(FieldMeta f) => ActiveModel is not null && f.Property.GetValue(ActiveModel) is bool b && b;

    protected void SetValue(FieldMeta f, object? v)
    {
        if (ActiveModel is null) return;
        var val = ConvertValue(f.Property.PropertyType, v);
        f.Property.SetValue(ActiveModel, val);
        EditContext.NotifyFieldChanged(new FieldIdentifier(ActiveModel, f.Property.Name));
    }

    private static object? ConvertValue(Type target, object? raw)
    {
        var t = Nullable.GetUnderlyingType(target) ?? target;
        var s = raw?.ToString();
        if (t == typeof(string)) return s ?? "";
        if (t == typeof(bool)) return raw is bool b ? b : (bool.TryParse(s, out var p) && p);
        if (string.IsNullOrWhiteSpace(s)) return Nullable.GetUnderlyingType(target) is null ? Activator.CreateInstance(t) : null;
        if (t == typeof(DateTime)) return DateTime.TryParse(s, out var d) ? d : (Nullable.GetUnderlyingType(target) is null ? default(DateTime) : null);
        try { return Convert.ChangeType(s, t, CultureInfo.InvariantCulture); } catch { return Nullable.GetUnderlyingType(target) is null ? Activator.CreateInstance(t) : null; }
    }

    protected IEnumerable<string> GetValidationMessages(FieldMeta f) => ActiveModel is null ? Array.Empty<string>() : EditContext.GetValidationMessages(f.FieldIdentifier);

    public sealed class FieldMeta { public required PropertyInfo Property { get; init; } public required string Label { get; init; } public required InputKind Kind { get; init; } public required string HtmlType { get; init; } public required FieldIdentifier FieldIdentifier { get; init; } }
    public enum InputKind { Text, TextArea, Email, Phone, Number, Date, Checkbox }
}
