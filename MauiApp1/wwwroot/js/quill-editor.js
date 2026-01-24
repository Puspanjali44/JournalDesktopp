var quill;

window.initQuill = (editorId, htmlContent) => {

    const container = document.getElementById(editorId);
    if (!container) {
        console.error("Editor container not found:", editorId);
        return;
    }

    quill = new Quill(container, {
        theme: 'snow',
        modules: {
            toolbar: [
                ['bold', 'italic', 'underline'],
                [{ 'header': [1, 2, 3, false] }],
                [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                ['link'],
                ['clean']
            ]
        }
    });

    if (htmlContent) {
        quill.root.innerHTML = htmlContent;
    }
};

window.getQuillHtml = () => {
    if (!quill) return "";
    return quill.root.innerHTML;
};
