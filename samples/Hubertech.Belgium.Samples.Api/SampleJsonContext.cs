using System.Text.Json.Serialization;

namespace Hubertech.Belgium.Samples.Api;

/// <summary>
/// Serialization metadata generated at compile time, as Native AOT requires.
/// </summary>
[JsonSerializable(typeof(InvoiceRequest))]
[JsonSerializable(typeof(Invoice))]
[JsonSerializable(typeof(List<FieldError>))]
[JsonSerializable(typeof(EnterpriseNumberForms))]
[JsonSerializable(typeof(BusinessDayResult))]
internal sealed partial class SampleJsonContext : JsonSerializerContext;
