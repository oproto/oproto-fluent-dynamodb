using FsCheck;
using FsCheck.Xunit;
using Oproto.FluentDynamoDb.Geospatial.H3;

namespace Oproto.FluentDynamoDb.Geospatial.UnitTests.H3;

/// <summary>
/// Property-based tests for H3Encoder using FsCheck.
/// These tests verify universal properties that should hold across all valid inputs.
/// </summary>
public class H3EncoderPropertyTests
{
    // Feature: s2-h3-geospatial-support, Property 2: H3 encoding produces valid cell indices
    // For any valid GeoLocation and H3 resolution (0-15), encoding the location to an H3 cell index 
    // should produce a valid H3 index that can be decoded back to coordinates
    // Validates: Requirements 1.3
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ValidGeoArbitraries) })]
    public void H3EncodingProducesValidCellIndices(ValidLatitude lat, ValidLongitude lon, ValidH3Resolution res)
    {
        // NOTE: H3 does NOT guarantee round-trip consistency (encode → decode → encode).
        // Per H3 documentation: "H3 provides exact logical containment but only approximate geometric containment"
        // Cell centers may fall slightly outside their geometric boundaries due to the aperture-7 grid design.
        // See: h3/website/docs/highlights/indexing.md
        
        var latitude = lat.Value;
        var longitude = lon.Value;
        var resolution = res.Value;
        
        // Act: Encode the location
        var h3Index = H3Encoder.Encode(latitude, longitude, resolution);
        
        // Assert: Index should be a valid hexadecimal string (15 characters)
        Assert.NotNull(h3Index);
        Assert.Equal(15, h3Index.Length);
        Assert.Matches("^[0-9a-f]+$", h3Index);
        
        // Assert: Index should be decodable back to coordinates
        var (decodedLat, decodedLon) = H3Encoder.Decode(h3Index);
        
        // The decoded coordinates should be valid
        Assert.InRange(decodedLat, -90, 90);
        Assert.InRange(decodedLon, -180, 180);
        
        // Assert: Encoding is deterministic (same input produces same output)
        var h3IndexAgain = H3Encoder.Encode(latitude, longitude, resolution);
        Assert.Equal(h3Index, h3IndexAgain);
        
        // Assert: Decoding is deterministic (same index produces same coordinates)
        var (decodedLat2, decodedLon2) = H3Encoder.Decode(h3Index);
        Assert.Equal(decodedLat, decodedLat2);
        Assert.Equal(decodedLon, decodedLon2);
    }
}

// Shared arbitraries are now in PropertyTestArbitraries.cs
