export async function copyText(text) {
    await navigator.clipboard.writeText(text);
}

export function confirmAction(message) {
    return confirm(message);
}
