using UnityEngine;

/// <summary>
/// Делает круглый спрайт кодом, чтобы не тянуть ассеты:
/// враги и игрок — просто круги разного размера и цвета.
/// </summary>
public static class CircleSpriteFactory
{
    private const int Size = 128;
    private static Sprite cached;

    public static Sprite Get()
    {
        if (cached != null)
            return cached;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.name = "WF_Circle";
        texture.filterMode = FilterMode.Bilinear;

        float radius = Size * 0.5f - 1f;
        Vector2 center = new Vector2(Size * 0.5f, Size * 0.5f);

        var pixels = new Color32[Size * Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = x + 0.5f - center.x;
                float dy = y + 0.5f - center.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // Мягкий край: сглаживаем по 1.5 пикселя
                float alpha = Mathf.Clamp01(radius - distance + 0.75f);

                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        cached = Sprite.Create(texture, new Rect(0f, 0f, Size, Size),
            new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
        cached.name = "WF_Circle";
        cached.hideFlags = HideFlags.HideAndDontSave;
        texture.hideFlags = HideFlags.HideAndDontSave;

        return cached;
    }
}
