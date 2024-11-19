window.adjustTextAreaHeight = function (elementId) {
    var textarea = document.getElementById(elementId);
    if (textarea) {
        // Reset the height to auto so the height will adjust based on content
        textarea.style.height = 'auto';

        // Set the height to match the scroll height of the content
        var newHeight = textarea.scrollHeight;

        // Get the maximum height based on the window height, minus some offset for padding/margins
        var maxHeight = window.innerHeight - 345; 

        // Use the smaller of the new height and the maximum height
        textarea.style.height = (newHeight > maxHeight ? maxHeight : newHeight) + 'px';
    }
};


function updateUrlWithoutQueryParam(url) {
    window.history.replaceState({}, document.title, url);
}