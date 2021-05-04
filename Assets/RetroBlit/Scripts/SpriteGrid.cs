/*********************************************************************************
* The comments in this file are used to generate the API documentation. Please see
* Assets/RetroBlit/Docs for much easier reading!
*********************************************************************************/

/// <summary>
/// Defines a sprite grid within a sprite sheet
/// </summary>
/// <remarks>
/// Defines a sprite grid within a sprite sheet. You can define as many sprite grids as needed to use with your sprite sheets.
/// The sprite grid is not tied to any particular sprite sheet, and can be reused with multiple sprite sheets.
///
/// If *region* is not specified then the grid covers the entire size of the sprite sheet, and each sprite is of *cellSize* dimensions.
///
/// If *region* is specified then only that area of the sprite sheet is used by the grid, and each sprite in this area is of *cellSize* dimensions.
/// This allows for defining of multiple grids of varying sprite sizes in the same sprite sheet.
/// </remarks>
public struct SpriteGrid
{
    /// <summary>
    /// Special sprite grid that covers the entire sprite sheet in a single grid cell, regardless of how large the sprite sheet is.
    /// </summary>
    /// <remarks>
    /// Special sprite grid that covers the entire sprite sheet in a single grid cell, regardless of how large the sprite sheet is.
    /// This is the default sprite grid for new sprite sheets.
    /// </remarks>
    public static readonly SpriteGrid fullSheet = new SpriteGrid(new Vector2i(-1, -1));

    /// <summary>
    /// Sprite sheet region covered by the sprite grid
    /// </summary>
    /// <remarks>
    /// Sprite sheet region covered by the sprite grid. The region could be a subset of the full sprite sheet.
    /// This allows for defining of multiple grids of varying sprite sizes in the same sprite sheet.
    /// </remarks>
    public Rect2i region;

    /// <summary>
    /// Size of a single cell in the sprite grid
    /// </summary>
    /// <remarks>
    /// The size of a single sprite or cell in the sprite grid. All sprites in the sprite grid are of the same size.
    /// </remarks>
    public Vector2i cellSize;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="region">Region covered by the sprite grid</param>
    /// <param name="cellSize">Size of a single grid cell</param>
    public SpriteGrid(Rect2i region, Vector2i cellSize)
    {
        this.region = region;
        this.cellSize = cellSize;
    }

    /// <summary>
    /// Constructor, assumes sprite grid covers entire sprite sheet
    /// </summary>
    /// <param name="cellSize">Size of a single grid cell</param>
    public SpriteGrid(Vector2i cellSize)
    {
        region = new Rect2i(0, 0, -1, -1);
        this.cellSize = cellSize;
    }
}
