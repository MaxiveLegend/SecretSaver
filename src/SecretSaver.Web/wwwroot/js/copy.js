document.addEventListener("click", async (event) => {
    const button = event.target.closest("[data-copy-target]");
    if (!button) return;

    const target = document.getElementById(button.dataset.copyTarget);
    const label = button.textContent;

    try {
        await navigator.clipboard.writeText(target.value ?? target.textContent);
        button.textContent = "Copied";
    } catch {
        target.select?.();
        button.textContent = "Press Ctrl+C to copy";
    }

    setTimeout(() => (button.textContent = label), 2000);
});
