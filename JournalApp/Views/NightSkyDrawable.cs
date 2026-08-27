namespace JournalApp.Views;

/// <summary>
/// Paints the faint star field layered over the night-sky gradient. Positions come from a fixed
/// seed so the sky is identical on every redraw instead of shimmering as the page relayouts.
/// </summary>
public sealed class NightSkyDrawable : IDrawable
{
    private const int StarCount = 26;
    private static readonly Color Cream = Color.FromArgb("#F1E6DC");
    private static readonly Color Gold = Color.FromArgb("#E8C98A");

    public void Draw(ICanvas canvas, RectF rect)
    {
        var random = new Random(20260820);

        for (var i = 0; i < StarCount; i++)
        {
            var x = (float)random.NextDouble() * rect.Width;
            var y = (float)random.NextDouble() * rect.Height * 0.94f;
            var radius = 0.6f + (float)random.NextDouble() * 1.4f;

            canvas.FillColor = i % 4 == 0 ? Gold : Cream;
            canvas.Alpha = 0.3f + (float)random.NextDouble() * 0.45f;
            canvas.FillCircle(x, y, radius);
        }

        canvas.Alpha = 1f;
    }
}
