# App icon

These are the icons for the exe, the windows and the tray.
[app.ico](app.ico) is made from [app.svg](app.svg), and [app-paused.ico](app-paused.ico) from [app-paused.svg](app-paused.svg).
Each SVG draws a Lucide icon in white on a blue square.
Lucide's licence is in [Assets/Icons/LICENSE](../Icons/LICENSE).

After changing an SVG, remake its icon with [ImageMagick](https://imagemagick.org):

```shell
magick -background none app.svg -define icon:auto-resize=256,48,32,24,16 app.ico
magick -background none app-paused.svg -define icon:auto-resize=256,48,32,24,16 app-paused.ico
```

Each `.ico` file holds the sizes Windows uses, from 16 to 256 pixels.
