# Assets

Design and source files for WheelOverlay graphics and icons.

## Contents

- Vector or hirh-rez source files for icons and graphics
- SVG exports used in the application (e.g., dial knob graphic)
- Light and dark mode icon variants

## Usage

Source files live here for version control. Exported assets used by the application should be copied to `WheelOverlay/Resources/` after finalization.

## SVG in WPF

WPF doesn't natively support SVG. Currently, SVG path data and gradients are manually translated into XAML shapes (`Path`, `Ellipse`, `LinearGradientBrush`) in the layout files (e.g., `DialLayout.xaml`). The SVG files here are the source-of-truth design files — they aren't part of the build.

If more SVG graphics are added in the future, investigate [SharpVectors](https://github.com/nicholasgasior/SharpVectors) as a NuGet package that can load and render SVGs at runtime in WPF, avoiding manual translation.
