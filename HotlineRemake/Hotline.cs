using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace HotlineRemake;

public class Hotline : Control
{
    private double _palmOffset;
    private double _gradientShift;
    private const double GradientSpeed = 0.4;
    private const double BuildingScrollSpeed = 4;
    private const double BuildingMinWidth = 70;
    private const double BuildingMaxWidth = 130;
    private const double BuildingMinHeight = 150;
    private const double BuildingMaxHeight = 400;
    private const double BuildingMinSpacing = 200;
    private const double BuildingMaxSpacing = 500;
    private const double PalmScrollSpeed = 17.0;
    private Bitmap? _palmsImage;
    private readonly List<Rect> _buildings = new();
    private readonly Random _random = new();

    public Hotline()
    {
        this.AttachedToVisualTree += (_, _) =>
        {
            InitializeBuildings();
            var uri = new Uri("avares://HotlineRemake/Assets/palms.png");
            _palmsImage = new Bitmap(AssetLoader.Open(uri));
        };

        DispatcherTimer.Run(UpdateFrame, TimeSpan.FromMilliseconds(16));
    }

    private void InitializeBuildings()
    {
        _buildings.Clear();
        double x = -this.Bounds.Width;
        double controlWidth = this.Bounds.Width > 1280 ? this.Bounds.Width : 1280;
        double controlHeight = this.Bounds.Height > 0 ? this.Bounds.Height : 720;
            
        while (x > -(controlWidth + 1280))
        {
            double width = _random.NextDouble() * (BuildingMaxWidth - BuildingMinWidth) + BuildingMinWidth;
            double height = _random.NextDouble() * (BuildingMaxHeight - BuildingMinHeight) + BuildingMinHeight;
            double spacing = _random.NextDouble() * (BuildingMaxSpacing - BuildingMinSpacing) + BuildingMinSpacing;
            _buildings.Add(new Rect(x, controlHeight - height, width, height));
            x -= width + spacing;
        }
    }

    private bool UpdateFrame()
    {
        _gradientShift += GradientSpeed;
        if (_gradientShift >= 360) _gradientShift -= 360;

        _palmOffset += PalmScrollSpeed;
        if (_palmOffset >= 910) _palmOffset -= 910;
            
        for (int i = 0; i < _buildings.Count; i++)
        {
            var building = _buildings[i];
            building = new Rect(building.X + BuildingScrollSpeed, building.Y, building.Width, building.Height);
            if (building.X > this.Bounds.Width)
            {
                building = new Rect(-building.Width, building.Y, building.Width, building.Height);
            }
            _buildings[i] = building;
        }


        InvalidateVisual();
        return true;
    }

    public override void Render(DrawingContext context)
    {
        var bounds = this.Bounds;
        double letterboxHeight = bounds.Height * 0.20;

        // Cinematic letterboxes
        context.DrawRectangle(Brushes.Black, null, new Rect(0, 0, bounds.Width, letterboxHeight)); // Top
        context.DrawRectangle(Brushes.Black, null,
            new Rect(0, bounds.Height - letterboxHeight, bounds.Width, letterboxHeight)); // Bottom

        // lgtv+ Background
        var stops = new GradientStops
        {
            new GradientStop(
                Color.FromArgb(255, (byte)(Math.Sin(_gradientShift * Math.PI / 180) * 127 + 128), 0, 255),
                0.0), 
            new GradientStop(
                Color.FromArgb(255, 255, 0, (byte)(Math.Cos(_gradientShift * Math.PI / 180) * 127 + 128)),
                1.0), 
        };

        var dynamicGradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            GradientStops = stops
        };

        context.DrawRectangle(dynamicGradient, null,
            new Rect(0, letterboxHeight, bounds.Width, bounds.Height - 2 * letterboxHeight));

        // Buildings
        foreach (var building in _buildings)
        {
            var offsetRect = new Rect(building.X, building.Y, building.Width, building.Height);
            context.DrawRectangle(Brushes.Black, null, offsetRect);
        }

        // Draw palms
        double imageWidth = _palmsImage!.PixelSize.Width;
        double imageHeight = _palmsImage!.PixelSize.Height;

        double firstPalmX = (_palmOffset % imageWidth) - imageWidth;
        if (firstPalmX < -imageWidth) firstPalmX += imageWidth;

        for (double x = firstPalmX; x < bounds.Width + imageWidth; x += imageWidth)
        {
            var destRect = new Rect(x, bounds.Height - imageHeight - letterboxHeight, imageWidth, imageHeight);
            context.DrawImage(_palmsImage, new Rect(0, 0, imageWidth, imageHeight), destRect);
        }

        // Vignette
        var topVignette = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 0.2, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.FromArgb(255, 0, 0, 0), 0.0),
                new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0)
            }
        };
        context.DrawRectangle(topVignette, null, new Rect(0, letterboxHeight, bounds.Width, bounds.Height * 0.8));

        var bottomVignette = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.75, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.FromArgb(255, 0, 0, 0), 0.0),
                new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0)
            }
        };
        context.DrawRectangle(bottomVignette, null,
            new Rect(0, bounds.Height * 0.2, bounds.Width, bounds.Height * 0.8));

        // CRT scan lines
        double lineHeight = 2;
        for (double y = letterboxHeight; y < bounds.Height - letterboxHeight; y += lineHeight * 2)
        {
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), null,
                new Rect(0, y, bounds.Width, lineHeight));
        }

            
           
            
        // Debug Text 
#if DEBUG
        var debugText = "Buildings:\n";
        foreach (var building in _buildings)
        {
            debugText += $"Position: ({building.X:F2}, {building.Y:F2}), Width: {building.Width:F2}, Height: {building.Height:F2}\n";
        }
        debugText += $"Gradient Shift: {_gradientShift:F2}\n";
        debugText += $"Palm Offset: {_palmOffset:F2}";

        var formattedText = new FormattedText(
            debugText, // Text to display
            CultureInfo.CurrentCulture, 
            FlowDirection.LeftToRight, 
            Typeface.Default, 
            24, // Font size
            Brushes.White // Text color
        );
            
        context.DrawText(formattedText, new Point(20, 20));
#endif
    }
}