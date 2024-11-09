using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;

[JsonConverter(typeof(ReviewItemRatingJsonConverter))]
public record ReviewItemRating(string Value)
{
    public static implicit operator ReviewItemRating(string value)
    {
        return new ReviewItemRating(value);
    } 

    public static implicit operator string(ReviewItemRating rating)
    {
        return rating.Value;
    }
}

internal class ReviewItemRatingJsonConverter : JsonConverter<ReviewItemRating>
{
    public override ReviewItemRating? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return new ReviewItemRating(stringValue ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, ReviewItemRating value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}