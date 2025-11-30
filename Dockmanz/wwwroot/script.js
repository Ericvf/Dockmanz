window.appendLogLine = function (preElement, line) {
    var scrollToBottom = preElement.scrollHeight - preElement.clientHeight <= preElement.scrollTop + 3;
    if (line) {
        preElement.textContent += '\n' + line;
    }
    if (scrollToBottom) {
        preElement.scrollTop = preElement.scrollHeight;
    }
};

window.scrollToBottom = (el) => {
    if (!el) return;
    el.scrollTop = el.scrollHeight;
};