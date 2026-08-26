# Overlay templates

Overlay templates are plain HTML/CSS files served by the local application. The application injects its own browser runtime when the template is requested through `/overlay/{name}`.

Use `data-field` to bind an element's text to a value from `/api/state`:

```html
<span data-field="character.name"></span>
```

Use `data-visible-field` to hide an element while the referenced value is null:

```html
<div data-visible-field="mythicPlus">
    <span data-field="mythicPlus.dungeonName"></span>
</div>
```

Templates do not need to include application JavaScript.
