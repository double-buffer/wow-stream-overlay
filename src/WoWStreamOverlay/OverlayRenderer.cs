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

        document.querySelectorAll('[data-visible-field]').forEach(element => {
            const value = getValue(state, element.dataset.visibleField);
            element.hidden = value == null;
        });
    };

    fetch('/api/state')
        .then(response => {
            if (!response.ok) {
                throw new Error(`Unable to load game state: ${response.status}`);
            }

            return response.json();
        })
        .then(applyState)
        .catch(error => console.error(error));
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
