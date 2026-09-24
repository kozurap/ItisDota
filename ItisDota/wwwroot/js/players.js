export function clickFile() {
    const input = document.getElementById("json-file");
    if (input) {
        input.click();
    }
}

export function bindDrop(element, dotnet) {
    const onDragOver = (event) => {
        event.preventDefault();
        element.classList.add("is-dragover");
    };
    const onDragLeave = (event) => {
        event.preventDefault();
        element.classList.remove("is-dragover");
    };
    const onDrop = async (event) => {
        event.preventDefault();
        element.classList.remove("is-dragover");
        const file = event.dataTransfer && event.dataTransfer.files && event.dataTransfer.files[0];
        if (!file) {
            return;
        }
        const text = await file.text();
        await dotnet.invokeMethodAsync("OnFileDropped", file.name, text);
    };

    element.addEventListener("dragenter", onDragOver);
    element.addEventListener("dragover", onDragOver);
    element.addEventListener("dragleave", onDragLeave);
    element.addEventListener("drop", onDrop);

    return {
        dispose() {
            element.removeEventListener("dragenter", onDragOver);
            element.removeEventListener("dragover", onDragOver);
            element.removeEventListener("dragleave", onDragLeave);
            element.removeEventListener("drop", onDrop);
        }
    };
}

export async function copyText(text) {
    await navigator.clipboard.writeText(text);
}

export function confirmAction(message) {
    return confirm(message);
}
