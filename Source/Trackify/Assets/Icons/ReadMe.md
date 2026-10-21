# App icon sources

Uno.Resizetizer renders every platform's icon set from these SVGs at build time; nothing here ships
as an in-app image (`Trackify.csproj` excludes the folder from the default `UnoImage` glob). The
splash screen comes from `../Splash/splash_screen.svg`.

| File | Used by | What it is |
|---|---|---|
| `icon.svg` | Android, iOS | Full-bleed opaque square. The OS cuts the shape (adaptive-icon mask, iOS squircle). |
| `icon_rounded.svg` | Windows, desktop, web | Same colour with its own rounded corners on a transparent canvas, because nothing masks the icon there. |
| `icon_foreground.svg` | all heads | The glyph. Scaled 0.65 on Android, 0.80 on iOS, 0.80 on Windows/desktop/web. |

Rules when changing the icon:

- **Keep the glyph inside a circle of radius 440 around the centre of the 1024 canvas.** At
  Android's 0.65 scale that is the 66 dp safe zone of the 108 dp adaptive icon; anything outside it
  is cut off by some launcher mask.
- **The background fill is `TrackifyBrandColor` in `Trackify.csproj`.** It is also the icon `Color`
  on the phones (keeps the iOS 1024 marketing icon free of transparency, which App Store Connect
  rejects), the splash background, and the PWA `theme_color`/`background_color` in
  `Platforms/WebAssembly/manifest.webmanifest`. Change all of them together.
- **Do not rename `icon.svg`.** iOS finds the icon set through `XSAppIconAssets`
  (`Assets.xcassets/icon.appiconset`) in `Platforms/iOS/Info.plist`, Android through `@mipmap/icon`.
- **No third-party marks** in the icon (no LEGO logo, wordmark, minifigure or brick studs). Store
  review rejects apps that use other companies' trademarks in the icon.
- The splash glyph has to fit a circle of two thirds of the canvas: Android 12+ shows the splash
  image inside the launcher-icon slot and masks it to that circle.
