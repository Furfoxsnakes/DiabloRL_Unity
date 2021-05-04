/*********************************************************************************
* The comments in this file are used to generate the API documentation. Please see
* Assets/RetroBlit/Docs for much easier reading!
*********************************************************************************/

/// <summary>
/// Defines a packed sprite ID.
/// </summary>
/// <remarks>
/// Defines a packed sprite ID. Sprite IDs are static and not tied to any sprite sheet,
/// they are tied to the sprite name, and can be used with multiple sprite sheets with
/// matching sprite names.
///
/// <see cref="PackedSpriteID"/> is returned by <see cref="RB.PackedSpriteID"/>.
/// <seedoc>Features:Sprite Packs</seedoc>
/// </remarks>
public struct PackedSpriteID
{
    /// <summary>
    /// Empty sprite
    /// </summary>
    /// <remarks>
    /// Empty sprite ID, drawing this sprite will have no effect.
    /// </remarks>
    public static PackedSpriteID empty = new PackedSpriteID(0);

    private int mID;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <remarks>
    /// Constructor. It's usually not useful to construct <see cref="PackedSpriteID"/> manually,
    /// <see cref="RB.PackedSpriteID"/> should be called instead.
    /// <seedoc>Features:Sprite Packs</seedoc>
    /// </remarks>
    /// <param name="id">ID</param>
    /// <seealso cref="RB.PackedSpriteID"/>
    public PackedSpriteID(int id)
    {
        mID = id;
    }

    /// <summary>
    /// The sprite ID that corresponds to the sprite name
    /// </summary>
    /// <remarks>
    /// The sprite ID that corresponds to the sprite name.
    /// <seedoc>Features:Sprite Packs</seedoc>
    /// </remarks>
    /// <seealso cref="RB.PackedSpriteID"/>
    public int id
    {
        get
        {
            return mID;
        }
    }

    /// <summary>
    /// Equality operator
    /// </summary>
    /// <remarks>
    /// Equality operator.
    /// </remarks>
    /// <param name="a">Left side</param>
    /// <param name="b">Right side</param>
    /// <returns>True if equal</returns>
    public static bool operator ==(PackedSpriteID a, object b)
    {
        if (object.ReferenceEquals(a, null) && object.ReferenceEquals(b, null))
        {
            return true;
        }

        if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null))
        {
            return false;
        }

        return a.Equals(b);
    }

    /// <summary>
    /// Inequality operator
    /// </summary>
    /// <remarks>
    /// Inequality operator.
    /// </remarks>
    /// <param name="a">Left side</param>
    /// <param name="b">Right side</param>
    /// <returns>True if not equal</returns>
    public static bool operator !=(PackedSpriteID a, object b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Equality
    /// </summary>
    /// <remarks>
    /// Equality check.
    /// </remarks>
    /// <param name="other">Other</param>
    /// <returns>True if equal</returns>
    public override bool Equals(object other)
    {
        if (other == null)
        {
            return false;
        }

        if (other is PackedSpriteID)
        {
            PackedSpriteID pid = (PackedSpriteID)other;
            if (id != pid.id)
            {
                return false;
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Get hashcode
    /// </summary>
    /// <remarks>
    /// Get hashcode.
    /// </remarks>
    /// <returns>Hash code</returns>
    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
}
