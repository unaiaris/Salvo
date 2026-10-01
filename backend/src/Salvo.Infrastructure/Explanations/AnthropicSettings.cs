namespace Salvo.Infrastructure.Explanations;

/// <summary>
/// What the Anthropic adapter needs from configuration: the key and the model.
/// </summary>
/// <remarks>
/// <para>
/// A class and not a record, on purpose. A record writes every property into its
/// <see cref="object.ToString"/>, and an object that reaches a log message by accident would carry
/// the key with it. This one says which model it names and nothing else.
/// </para>
/// <para>
/// Read once, by <c>AddExplanationProvider</c>, which is the only reader of
/// <c>ANTHROPIC_API_KEY</c> in the codebase (decision 77).
/// </para>
/// </remarks>
public sealed class AnthropicSettings
{
    public AnthropicSettings(string apiKey, string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        ApiKey = apiKey;
        Model = model;
    }

    /// <summary>The model asked for. What answered is read from the response, not from here.</summary>
    public string Model { get; }

    /// <summary>The secret. Internal so that nothing outside the adapter can even name it.</summary>
    internal string ApiKey { get; }

    public override string ToString()
    {
        return $"{nameof(AnthropicSettings)} {{ {nameof(Model)} = {Model} }}";
    }
}
