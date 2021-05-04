/*********************************************************************************
* The comments in this file are used to generate the API documentation. Please see
* Assets/RetroBlit/Docs for much easier reading!
*********************************************************************************/

/// <summary>
/// Defines a packed sprite used with sprite packs
/// </summary>
/// <remarks>
/// Defines a packed sprite used with sprite packs. Instances of this struct are not usually created manually, but instead they returned
/// by <see cref="RB.PackedSpriteGet"/>.
/// <seedoc>Features:Sprite Packs</seedoc>
/// </remarks>
/// <seealso cref="SpriteSheetAsset"/>
/// <seealso cref="RB.PackedSpriteID"/>
/// <seealso cref="RB.PackedSpriteGet"/>
public struct PackedSprite
{
    private PackedSpriteID mID;
    private Vector2i mSize;
    private Rect2i mSourceRect;
    private Vector2i mTrimOffset;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <remarks>
    /// Constructor. Most often <see cref="RB.PackedSpriteGet"/> should be used instead of created PackedSprite manually.
    /// <seedoc>Features:Sprite Packs</seedoc>
    /// </remarks>
    /// <param name="id">SpriteID of the sprite</param>
    /// <param name="size">Size of the sprite, untrimmed</param>
    /// <param name="sourceRect">Source rectangle from within packed texture, may be trimmed</param>
    /// <param name="trimOffset">Trim offset</param>
    /// <seealso cref="SpriteSheetAsset"/>
    /// <seealso cref="RB.PackedSpriteGet"/>
    public PackedSprite(PackedSpriteID id, Vector2i size, Rect2i sourceRect, Vector2i trimOffset)
    {
        mID = id;
        mSize = size;
        mSourceRect = sourceRect;
        mTrimOffset = trimOffset;
    }

    /// <summary>
    /// Sprite ID
    /// </summary>
    /// <remarks>
    /// ID of the sprite. IDs are unique to the filename of the sprite, and therefore the same ID would be used with multiple spritesheets if they
    /// are made of sprites with the same filenames.
    /// <seedoc>Features:Sprite Packs</seedoc>
    /// </remarks>
    /// <seealso cref="RB.PackedSpriteID"/>
    public PackedSpriteID id
    {
        get
        {
            return mID;
        }
    }

    /// <summary>
    /// Size of original sprite, before any trimming
    /// </summary>
    /// <remarks>
    /// The original size of the packed sprite as it was defined in the source image of the sprite.
    /// <seedoc>Features:Sprite Packs</seedoc>
    /// </remarks>
    public Vector2i Size
    {
        get
        {
            return mSize;
        }
    }

    /// <summary>
    /// Source rect of the sprite in the packed sprite sheet.
    /// </summary>
    /// <remarks>
    /// The rectangle representing the location of the sprite in the sprite sheet. This rectangle may be smaller than the original sprite size if the packed sprite
    /// had empty space trimmed.
    /// <seedoc>Features:Sprite Packs</seedoc>
    /// </remarks>
    public Rect2i SourceRect
    {
        get
        {
            return mSourceRect;
        }
    }

    /// <summary>
    /// Top-left corner offset of the sprite after it's been trimmed
    /// </summary>
    /// <remarks>
    /// Top-left corner offset of the sprite after it had any empty space trimmed.
    /// <seedoc>Features:Sprite Packs</seedoc>
    /// </remarks>
    public Vector2i TrimOffset
    {
        get
        {
            return mTrimOffset;
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
    public static bool operator ==(PackedSprite a, object b)
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
    public static bool operator !=(PackedSprite a, object b)
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

        if (other is PackedSprite)
        {
            PackedSprite sprite = (PackedSprite)other;
            if (id == sprite.id &&
                Size == sprite.Size &&
                SourceRect == sprite.SourceRect &&
                TrimOffset == sprite.TrimOffset)
            {
                return true;
            }

            return false;
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
        return id.GetHashCode() ^ Size.GetHashCode() ^ SourceRect.GetHashCode() ^ TrimOffset.GetHashCode();
    }
}
