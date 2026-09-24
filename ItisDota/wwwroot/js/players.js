export async function copyText(text) {
    if (navigator.clipboard && typeof navigator.clipboard.writeText === "function") {
        await navigator.clipboard.writeText(text);
        return;
    }

    const textarea = document.createElement("textarea");
    textarea.value = text;
    textarea.setAttribute("readonly", "");
    textarea.style.position = "fixed";
    textarea.style.top = "0";
    textarea.style.left = "0";
    textarea.style.width = "1px";
    textarea.style.height = "1px";
    textarea.style.padding = "0";
    textarea.style.border = "none";
    textarea.style.outline = "none";
    textarea.style.boxShadow = "none";
    textarea.style.background = "transparent";
    document.body.appendChild(textarea);

    let copied = false;
    try {
        textarea.focus();
        textarea.select();
        textarea.setSelectionRange(0, textarea.value.length);
        copied = document.execCommand("copy");
    } finally {
        document.body.removeChild(textarea);
    }

    if (!copied) {
        throw new Error("Буфер обмена недоступен в этом браузере.");
    }
}

export function confirmAction(message) {
    return confirm(message);
}

let leaveHandler = null;

export function watchRoomLeave(groupId) {
    clearRoomLeave();
    leaveHandler = () => {
        const url = new URL(`groups/${groupId}/not-ready`, document.baseURI).toString();
        const sent = navigator.sendBeacon(url);
        if (!sent) {
            fetch(url, { method: "POST", keepalive: true, credentials: "same-origin" });
        }
    };
    window.addEventListener("pagehide", leaveHandler);
}

export function clearRoomLeave() {
    if (leaveHandler) {
        window.removeEventListener("pagehide", leaveHandler);
        leaveHandler = null;
    }
}
