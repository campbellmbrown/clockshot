# App icon

These are the icons for the exe and the windows.
[app.ico](app.ico) is made from [app.svg](app.svg).
The SVG draws a Lucide icon in white on a blue square.
Lucide's licence is in [Assets/Icons/LICENSE](../Icons/LICENSE).

After changing the SVG, remake the icon with [ImageMagick](https://imagemagick.org):

```shell
magick -background none app.svg -define icon:auto-resize=256,48,32,24,16 app.ico
```

The `.ico` file holds the sizes Windows uses, from 16 to 256 pixels.
