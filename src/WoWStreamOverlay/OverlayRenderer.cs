namespace WowStreamOverlay;

/// <summary>
/// Injects the common browser runtime into user-provided overlay templates.
/// </summary>
public static class OverlayRenderer
{
    private const string RuntimeScript = """
<script>
(() => {
    const getValue = (source, path) => path.split('.').reduce((value, key) => value == null ? null : value[key], source);

    const applyState = state => {
        document.querySelectorAll('[data-field]').forEach(element => {
            const value = getValue(state, element.dataset.field);
            element.textContent = value == null ? '' : value;
        });

        document.querySelectorAll('[data-color-field]').forEach(element => {
            const value = getValue(state, element.dataset.colorField);
            element.style.color = value == null ? '' : value;
        });

        document.querySelectorAll('[data-visible-field], [data-hidden-field]').forEach(element => {
            const visibleField = element.dataset.visibleField;
            const hiddenField = element.dataset.hiddenField;
            const visible = visibleField == null || getValue(state, visibleField) != null;
            const hidden = hiddenField != null && getValue(state, hiddenField) != null;
            element.hidden = !visible || hidden;
        });
    };

    const events = new EventSource('/events');
    events.onmessage = event => applyState(JSON.parse(event.data));
})();
</script>
""";

    public static string Render(string template)
    {
        var bodyEnd = template.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

        if (bodyEnd < 0)
        {
            return template + RuntimeScript;
        }

        return template.Insert(bodyEnd, RuntimeScript);
    }
}
